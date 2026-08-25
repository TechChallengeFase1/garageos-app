#!/usr/bin/env bash
# Abre um tunel da sua maquina ate o RDS, para usar DBeaver, pgAdmin ou psql.
#
# POR QUE PRECISA DE TUNEL: o RDS esta em subnet privada com
# publicly_accessible = false. Nao existe rota da internet ate ele - e isso e
# proposital. O unico caminho e passar por dentro da VPC.
#
# COMO FUNCIONA:
#
#   seu SGBD -> localhost:5432 -> kubectl port-forward -> pod socat no cluster
#            -> (Security Group "cracha" do no) -> RDS:5432
#
# O pod herda os Security Groups do no onde roda, e os nos carregam o cracha.
# Por isso ele alcanca o banco sem nenhuma regra nova.
#
#   Uso:      ./scripts/tunel-banco.sh
#   Encerrar: Ctrl+C (o pod e removido automaticamente)

set -uo pipefail
export MSYS_NO_PATHCONV=1

PROJETO="${PROJETO:-garageos}"
AMBIENTE="${AMBIENTE:-producao}"
NS="${NS:-garageos}"
PORTA_LOCAL="${PORTA_LOCAL:-5432}"
POD="rds-tunel"

ssm() { aws ssm get-parameter --name "$1" --query Parameter.Value --output text 2>/dev/null; }

ENDPOINT=$(ssm "/$PROJETO/$AMBIENTE/rds/endpoint")
BANCO=$(ssm "/$PROJETO/$AMBIENTE/rds/dbname")
SEGREDO=$(ssm "/$PROJETO/$AMBIENTE/rds/secret-arn")

if [ -z "$ENDPOINT" ]; then
  echo "Nao consegui ler o endpoint do RDS no SSM."
  echo "Confira se a AWS CLI esta autenticada NESTE terminal (nao no WSL)."
  exit 2
fi

USUARIO=$(aws secretsmanager get-secret-value --secret-id "$SEGREDO" \
  --query SecretString --output text 2>/dev/null \
  | python -c "import sys,json;print(json.load(sys.stdin)['username'])")

limpar() {
  echo ""
  echo "Encerrando o tunel..."
  kubectl delete pod "$POD" -n "$NS" --ignore-not-found --wait=false >/dev/null 2>&1
  exit 0
}
trap limpar INT TERM

echo "Criando o proxy dentro do cluster..."
kubectl delete pod "$POD" -n "$NS" --ignore-not-found >/dev/null 2>&1
kubectl run "$POD" -n "$NS" --image=alpine/socat --restart=Never \
  --port=5432 -- \
  tcp-listen:5432,fork,reuseaddr "tcp-connect:${ENDPOINT}:5432" >/dev/null

kubectl wait --for=condition=ready "pod/$POD" -n "$NS" --timeout=90s >/dev/null 2>&1 || {
  echo "O pod do tunel nao ficou pronto."
  kubectl describe pod "$POD" -n "$NS" | tail -15
  limpar
}

cat <<FIM

Tunel aberto. Configure o seu SGBD assim:

  Host      localhost
  Porta     $PORTA_LOCAL
  Banco     $BANCO
  Usuario   $USUARIO
  Senha     rode o comando abaixo para ler

    aws secretsmanager get-secret-value --secret-id $SEGREDO \\
      --query SecretString --output text

  SSL       desabilitado (o tunel ja e local; o trecho ate o RDS
            continua criptografado dentro da VPC)

Deixe esta janela aberta. Ctrl+C encerra e remove o pod.

FIM

kubectl port-forward "pod/$POD" "${PORTA_LOCAL}:5432" -n "$NS"
limpar
