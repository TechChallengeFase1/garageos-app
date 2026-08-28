# ADR 0002 — Uso do Horizontal Pod Autoscaler

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-08-27 |
| **Contexto** | Tech Challenge — Fase 3 |
| **Repositórios afetados** | `garageos-app`, `garageos-infra-k8s` |
| **Relacionadas** | [RFC 0004](../rfcs/0004-ferramenta-de-observabilidade.md), [ADR 0001](0001-comunicacao-entre-repositorios.md) |

## Contexto

O enunciado exige um **cluster Kubernetes com escalabilidade**. Declarar um `HorizontalPodAutoscaler` no YAML é a parte fácil; fazê-lo *funcionar* depende de quatro condições que não são óbvias, e cada uma já causou um modo de falha conhecido neste projeto.

## Decisão

Usar o **HPA v2 sobre utilização de CPU**, com 2 réplicas mínimas e 10 máximas, alvo de 70% — e tratar como parte da decisão as quatro pré-condições abaixo, sem as quais o autoscaling é apenas declarativo.

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: garageos-api-hpa
spec:
  scaleTargetRef: { apiVersion: apps/v1, kind: Deployment, name: garageos-api }
  minReplicas: 2
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target: { type: Utilization, averageUtilization: 70 }
```

### Pré-condição 1 — `metrics-server` instalado

O HPA lê CPU e memória pela **Metrics API**, que o Kubernetes **não implementa por padrão**. Sem o `metrics-server`, o HPA mostra `<unknown>/70%` e nunca escala: o requisito ficaria declarado no YAML sem funcionar.

Instalado via Helm pelo `garageos-infra-k8s`, com `wait = true` — o apply só retorna quando os pods estiverem prontos, em vez de a falha aparecer depois, no primeiro HPA que não escala.

> No cluster `kind` da fase anterior, o chart precisava de `--kubelet-insecure-tls`, porque o kubelet usava certificado self-signed. No EKS o kubelet usa certificado assinado pela CA do próprio cluster, então a flag foi **removida** — ela apenas desabilitaria a verificação de TLS sem necessidade.

### Pré-condição 2 — `resources.requests.cpu` declarado

A utilização é calculada como **uso ÷ request**. Um container sem `requests.cpu` não tem denominador, e o HPA não consegue calcular percentual nenhum. Daí `requests: { cpu: 100m, memory: 128Mi }` no Deployment.

### Pré-condição 3 — migrations fora do startup

Esta é a razão de ordem mais importante da Fase 4. Com o HPA ativo, o Kubernetes sobe **várias réplicas simultaneamente**; se as migrations rodassem no startup, todas tentariam migrar ao mesmo tempo, disputando a mesma tabela de histórico do EF Core.

A solução foi mover as migrations para um **Kubernetes Job** (`k8s/jobs/migrations-job.yaml`), que roda **uma vez**, com a mesma imagem da aplicação (`args: ["--migrate"]`), antes do rollout do Deployment. A migration no startup segue disponível, mas desligada por padrão — fica ligada apenas no `docker compose`, onde há uma instância só.

> Por isso o Job era **pré-requisito obrigatório** do HPA no plano de execução, e não um refinamento posterior.

### Pré-condição 4 — health checks HTTP corretos

Escalar cria pods novos, e o Service só deve mandar tráfego para os que estiverem prontos. A separação entre os dois endpoints é deliberada:

| Endpoint | Consulta o banco? | Efeito na falha |
|---|---|---|
| `/health/ready` | **Sim** | O pod sai do balanceamento e volta sozinho quando o banco voltar. Ninguém é reiniciado |
| `/health/live` | **Não** | Se consultasse, uma oscilação do RDS reprovaria a probe em **todos** os pods ao mesmo tempo e o Kubernetes mataria a aplicação inteira — transformando instabilidade de banco em queda total |

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **Réplicas fixas** | Não atenderia ao requisito de escalabilidade |
| **HPA por memória** | Em .NET, o GC segura memória alocada; o consumo sobe e não desce proporcionalmente à carga. O HPA escalaria e não voltaria |
| **HPA por métrica customizada** (RPS, fila de OS) | Exigiria `prometheus-adapter` ou o adaptador da New Relic e uma métrica estável na aplicação. Complexidade desproporcional para o volume do projeto — anotado como evolução natural |
| **KEDA** | Mesma observação: ótimo para escala orientada a evento, sem ganho aqui |
| **Cluster Autoscaler / Karpenter** | Escalaria **nós**, não pods. Ver Consequências |

## Consequências

**Positivas**

- Escalabilidade real e verificável: `kubectl top pods` e `kubectl get hpa` mostram métrica e réplicas durante a demonstração.
- `minReplicas: 2` garante que a aplicação nunca fique com réplica única, então o rollout e a substituição de nó acontecem sem indisponibilidade.
- O `metrics-server` também alimenta `kubectl top`, útil para diagnóstico ao vivo.
- Com as migrations no Job, subir de 2 para 10 réplicas é seguro.

**Trade-offs conscientes**

- **O HPA escala pods, não nós.** Não há Cluster Autoscaler instalado: `node_max_size = 4` é teto para crescimento **manual**. Se as 10 réplicas não couberem nos 2 nós, os pods excedentes ficam `Pending`.
- **O teto de slots vem da ENI; o dimensionamento vem da memória.** Um `t3.small` suporta até 11 pods por limite de ENI, então 2 nós dão 22 slots, ocupados pelos componentes de sistema (`coredns`, `aws-node`, `kube-proxy`, `metrics-server`, `nri-bundle`) e pelas réplicas da aplicação. A conta que orienta o crescimento é a de memória: cada nó tem cerca de **1,4 GB alocável** e opera perto de **56%** com a aplicação no ar — medição registrada em `garageos-infra-k8s/newrelic.tf`, e o critério que definiu quais componentes do `nri-bundle` ficaram ligados. Ampliar a folga para testes de carga mais agressivos é questão de subir `node_instance_type` para `t3.medium`.
- **`maxReplicas: 10` contra um RDS `db.t4g.micro`.** Dez réplicas abrindo pool de conexões podem esgotar o `max_connections` da instância antes de a CPU dos pods virar gargalo. Em um teste de carga sério, o banco é o próximo limite — e o sintoma apareceria como erro de conexão, não como falta de réplica.
- **A observabilidade entra no dimensionamento do pod.** O profiler do CLR roda dentro do container da aplicação, então o `requests.memory = 128Mi` e o `limit` de 512Mi já contam com ele. Acompanhar `kubectl top pods -n garageos` sob carga é o que mantém a escala do HPA ancorada em uso real de CPU.
- **Custo.** Escalar pods não custa nada por si só, mas escalar nós custaria. O teto de 4 nós é também um teto de gasto.
