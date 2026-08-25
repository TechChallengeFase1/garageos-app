#!/usr/bin/env bash
# Valida a solucao inteira do GarageOS, de ponta a ponta.
#
# Nao altera nada: so faz leitura e requisicoes HTTP. Pode rodar quantas vezes
# quiser, inclusive durante a gravacao do video.
#
#   Uso:  ./scripts/validar-solucao.sh
#
# Requisitos: aws cli autenticado, kubectl com acesso ao cluster, curl, python.

set -uo pipefail

# O Git Bash no Windows converte argumentos que parecem caminho ("/garageos/...")
# em caminhos do Windows. Sem isto, toda chamada ao SSM falha.
export MSYS_NO_PATHCONV=1

PROJETO="${PROJETO:-garageos}"
AMBIENTE="${AMBIENTE:-producao}"
REGIAO="${REGIAO:-us-east-1}"
CPF_DEMO="${CPF_DEMO:-52998224725}"

ok=0; falhou=0

verde()   { printf '\033[32m%s\033[0m' "$1"; }
vermelho(){ printf '\033[31m%s\033[0m' "$1"; }
cinza()   { printf '\033[90m%s\033[0m' "$1"; }

titulo() { printf '\n\033[1m%s\033[0m\n' "$1"; }

# checar <descricao> <esperado> <obtido>
checar() {
  local descricao="$1" esperado="$2" obtido="$3"
  if [ "$esperado" = "$obtido" ]; then
    printf '  %s %-52s %s\n' "$(verde OK)" "$descricao" "$(cinza "$obtido")"
    ok=$((ok+1))
  else
    printf '  %s %-52s esperado=%s obtido=%s\n' "$(vermelho XX)" "$descricao" "$esperado" "${obtido:-vazio}"
    falhou=$((falhou+1))
  fi
}

# checar_nao_vazio <descricao> <valor>
checar_nao_vazio() {
  local descricao="$1" valor="$2"
  if [ -n "$valor" ]; then
    printf '  %s %-52s %s\n' "$(verde OK)" "$descricao" "$(cinza "$valor")"
    ok=$((ok+1))
  else
    printf '  %s %-52s %s\n' "$(vermelho XX)" "$descricao" "nao encontrado"
    falhou=$((falhou+1))
  fi
}

ssm() { aws ssm get-parameter --name "$1" --query Parameter.Value --output text 2>/dev/null; }

printf '\033[1mValidacao da solucao GarageOS\033[0m  (%s / %s)\n' "$PROJETO" "$AMBIENTE"

# ─────────────────────────────────────────────────────────────────────────────
titulo "1. Fundacao (bootstrap)"

checar_nao_vazio "Bucket de state do Terraform" "$(ssm "/$PROJETO/terraform/state-bucket")"
checar_nao_vazio "Role assumida pelo CI via OIDC" "$(ssm "/$PROJETO/iam/github-actions-role-arn")"
VPC_ID=$(ssm "/$PROJETO/vpc/id")
checar_nao_vazio "VPC" "$VPC_ID"

AZS=$(aws ec2 describe-subnets --filters "Name=vpc-id,Values=$VPC_ID" \
  --query 'Subnets[].AvailabilityZone' --output text 2>/dev/null | tr '\t' '\n' | grep -v '^$' | sort -u | wc -l | tr -d ' ')
checar "Subnets distribuidas em 2 AZs" "2" "$AZS"

# ─────────────────────────────────────────────────────────────────────────────
titulo "2. Banco de dados gerenciado"

RDS_ID="$PROJETO-$AMBIENTE-postgres"
checar "RDS disponivel" "available" \
  "$(aws rds describe-db-instances --db-instance-identifier "$RDS_ID" \
     --query 'DBInstances[0].DBInstanceStatus' --output text 2>/dev/null)"

checar "RDS sem endereco publico" "False" \
  "$(aws rds describe-db-instances --db-instance-identifier "$RDS_ID" \
     --query 'DBInstances[0].PubliclyAccessible' --output text 2>/dev/null)"

checar "Armazenamento criptografado" "True" \
  "$(aws rds describe-db-instances --db-instance-identifier "$RDS_ID" \
     --query 'DBInstances[0].StorageEncrypted' --output text 2>/dev/null)"

SG_RDS=$(aws ec2 describe-security-groups --filters "Name=group-name,Values=$PROJETO-$AMBIENTE-rds" \
  --query 'SecurityGroups[0].GroupId' --output text 2>/dev/null)
REGRAS=$(aws ec2 describe-security-group-rules --filters "Name=group-id,Values=$SG_RDS" \
  --query 'length(SecurityGroupRules[?IsEgress==`false`])' --output text 2>/dev/null)
checar "Banco aceita UMA unica origem (o cracha)" "1" "$REGRAS"

CIDR=$(aws ec2 describe-security-group-rules --filters "Name=group-id,Values=$SG_RDS" \
  --query 'SecurityGroupRules[?IsEgress==`false`].CidrIpv4' --output text 2>/dev/null)
checar "E essa origem nao e uma faixa de IP" "None" "${CIDR:-None}"

checar_nao_vazio "Credenciais no Secrets Manager" \
  "$(aws secretsmanager describe-secret --secret-id "$PROJETO-$AMBIENTE/rds/master" \
     --query Name --output text 2>/dev/null)"

# ─────────────────────────────────────────────────────────────────────────────
titulo "3. Cluster Kubernetes"

CLUSTER=$(ssm "/$PROJETO/$AMBIENTE/eks/cluster-name")
checar "Cluster ativo" "ACTIVE" \
  "$(aws eks describe-cluster --name "$CLUSTER" --query 'cluster.status' --output text 2>/dev/null)"

checar "Autenticacao por API (aws-auth desabilitado)" "API" \
  "$(aws eks describe-cluster --name "$CLUSTER" --query 'cluster.accessConfig.authenticationMode' --output text 2>/dev/null)"

NOS=$(kubectl get nodes --no-headers 2>/dev/null | grep -c " Ready ")
checar_nao_vazio "Nos prontos" "${NOS:-0}"

CRACHA=$(ssm "/$PROJETO/$AMBIENTE/rds/client-security-group-id")
COM_CRACHA=$(aws ec2 describe-instances \
  --filters "Name=tag:eks:cluster-name,Values=$CLUSTER" "Name=instance-state-name,Values=running" \
  --query "length(Reservations[].Instances[?SecurityGroups[?GroupId=='$CRACHA']])" --output text 2>/dev/null)
checar "Nos carregam o cracha de acesso ao RDS" "$NOS" "$COM_CRACHA"

checar "metrics-server rodando (necessario ao HPA)" "1/1" \
  "$(kubectl get deploy metrics-server -n kube-system --no-headers 2>/dev/null | awk '{print $2}')"

# ─────────────────────────────────────────────────────────────────────────────
titulo "4. Aplicacao"

NS=$(ssm "/$PROJETO/$AMBIENTE/eks/namespace")
checar_nao_vazio "Namespace" "$NS"

checar "Deployment com todas as replicas prontas" \
  "$(kubectl get deploy garageos-api -n "$NS" -o jsonpath='{.spec.replicas}' 2>/dev/null)" \
  "$(kubectl get deploy garageos-api -n "$NS" -o jsonpath='{.status.readyReplicas}' 2>/dev/null)"

IMAGEM=$(kubectl get deploy garageos-api -n "$NS" -o jsonpath='{.spec.template.spec.containers[0].image}' 2>/dev/null)
if [[ "$IMAGEM" == *":latest" ]]; then
  printf '  %s %-52s %s\n' "$(vermelho XX)" "Imagem fixada por commit (nao :latest)" "$IMAGEM"; falhou=$((falhou+1))
else
  printf '  %s %-52s %s\n' "$(verde OK)" "Imagem fixada por commit (nao :latest)" "$(cinza "${IMAGEM##*:}")"; ok=$((ok+1))
fi

METRICA=$(kubectl get hpa garageos-api-hpa -n "$NS" --no-headers 2>/dev/null | awk '{print $3" "$4}')
if [[ "$METRICA" == *"unknown"* || -z "$METRICA" ]]; then
  printf '  %s %-52s %s\n' "$(vermelho XX)" "HPA lendo metricas reais" "${METRICA:-vazio}"; falhou=$((falhou+1))
else
  printf '  %s %-52s %s\n' "$(verde OK)" "HPA lendo metricas reais" "$(cinza "$METRICA")"; ok=$((ok+1))
fi

checar_nao_vazio "Secret criado pela pipeline (fora do Git)" \
  "$(kubectl get secret garageos-secret -n "$NS" -o jsonpath='{.metadata.name}' 2>/dev/null)"

NLB=$(kubectl get svc garageos-api -n "$NS" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null)
checar_nao_vazio "Load Balancer com hostname" "$NLB"

# ─────────────────────────────────────────────────────────────────────────────
titulo "5. Health checks"

checar "/health/live responde 200" "200" \
  "$(curl -s -m 20 -o /dev/null -w '%{http_code}' "http://$NLB/health/live" 2>/dev/null)"

PRONTIDAO=$(curl -s -m 20 "http://$NLB/health/ready" 2>/dev/null)
checar "/health/ready responde 200" "200" \
  "$(curl -s -m 20 -o /dev/null -w '%{http_code}' "http://$NLB/health/ready" 2>/dev/null)"

checar "E o readiness realmente consulta o Postgres" "Healthy" \
  "$(printf '%s' "$PRONTIDAO" | python -c "
import sys,json
try:
    d=json.load(sys.stdin)
    print(next((c['status'] for c in d.get('checks',[]) if c['nome']=='postgres'), 'ausente'))
except Exception: print('erro')" 2>/dev/null)"

checar "E o liveness NAO consulta (0 checks)" "0" \
  "$(curl -s -m 20 "http://$NLB/health/live" 2>/dev/null | python -c "
import sys,json
try: print(len(json.load(sys.stdin).get('checks',[])))
except Exception: print('erro')" 2>/dev/null)"

# ─────────────────────────────────────────────────────────────────────────────
titulo "6. Logs estruturados e correlacao"

MARCA="validacao-$(date +%s)"
curl -s -m 20 -o /dev/null -H "X-Correlation-Id: $MARCA" "http://$NLB/health/ready" 2>/dev/null

checar "Correlation ID devolvido no header da resposta" "$MARCA" \
  "$(curl -s -m 20 -D- -o /dev/null -H "X-Correlation-Id: $MARCA" "http://$NLB/health/ready" 2>/dev/null \
    | grep -i '^x-correlation-id:' | tr -d '\r' | awk '{print $2}')"

sleep 3
LINHAS=$(kubectl logs -n "$NS" -l app=garageos-api --tail=200 --since=2m 2>/dev/null | grep -c "$MARCA")
if [ "${LINHAS:-0}" -gt 0 ]; then
  printf '  %s %-52s %s\n' "$(verde OK)" "Logs pesquisaveis por correlation ID" "$(cinza "$LINHAS linha(s)")"; ok=$((ok+1))
else
  printf '  %s %-52s %s\n' "$(vermelho XX)" "Logs pesquisaveis por correlation ID" "nenhuma linha"; falhou=$((falhou+1))
fi

FORMATO=$(kubectl logs -n "$NS" -l app=garageos-api --tail=1 2>/dev/null | head -1 | cut -c1-1)
checar "Logs em JSON" "{" "$FORMATO"

# ─────────────────────────────────────────────────────────────────────────────
titulo "7. Autenticacao por CPF e API Gateway"

GATEWAY=$(aws apigatewayv2 get-apis --query "Items[?Name=='$PROJETO-$AMBIENTE-api'].ApiEndpoint" --output text 2>/dev/null)
checar_nao_vazio "API Gateway" "$GATEWAY"

checar "Rota protegida SEM token e barrada" "401" \
  "$(curl -s -m 25 -o /dev/null -w '%{http_code}' "$GATEWAY/api/Clientes" 2>/dev/null)"

checar "Token forjado e barrado" "403" \
  "$(curl -s -m 25 -o /dev/null -w '%{http_code}' -H 'Authorization: Bearer aaa.bbb.ccc' "$GATEWAY/api/Clientes" 2>/dev/null)"

checar "CPF invalido recusado" "400" \
  "$(curl -s -m 25 -o /dev/null -w '%{http_code}' -X POST "$GATEWAY/auth" \
     -H 'content-type: application/json' -d '{"cpf":"11111111111"}' 2>/dev/null)"

checar "CPF valido de cliente inexistente" "404" \
  "$(curl -s -m 25 -o /dev/null -w '%{http_code}' -X POST "$GATEWAY/auth" \
     -H 'content-type: application/json' -d '{"cpf":"11144477735"}' 2>/dev/null)"

TOKEN=$(curl -s -m 25 -X POST "$GATEWAY/auth" -H 'content-type: application/json' \
  -d "{\"cpf\":\"$CPF_DEMO\"}" 2>/dev/null \
  | python -c "import sys,json
try: print(json.load(sys.stdin).get('accessToken',''))
except Exception: print('')" 2>/dev/null)
checar_nao_vazio "Token emitido para o CPF de demonstracao" "${TOKEN:0:24}${TOKEN:+...}"

checar "Fluxo completo: token da Lambda aceito pela API .NET" "200" \
  "$(curl -s -m 25 -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $TOKEN" \
     "$GATEWAY/api/Clientes" 2>/dev/null)"

# ─────────────────────────────────────────────────────────────────────────────
titulo "Resultado"
printf '  %s aprovados, %s reprovados\n\n' "$(verde "$ok")" "$([ "$falhou" -gt 0 ] && vermelho "$falhou" || echo 0)"

if [ "$falhou" -gt 0 ]; then exit 1; fi
