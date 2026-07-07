# Infraestrutura como Código (Terraform) — GarageOS

Este diretório provisiona, via **Terraform**, toda a **base** da aplicação: o
cluster Kubernetes, o namespace, o metrics-server e o **banco de dados PostgreSQL**.

A aplicação em si (Secret, ConfigMap, Deployment, Service e HPA) **não** é
provisionada aqui — ela é aplicada pela pipeline de **CI/CD** a partir de `../k8s`.
Essa divisão segue o enunciado da Fase 2:

| Camada | Responsável | Recursos |
| --- | --- | --- |
| **IaC (Terraform)** | `infra/` | Cluster kind, namespace, metrics-server, PostgreSQL (config, secret, PVC, StatefulSet, Service) |
| **CI/CD** | `.github/workflows` | Build, testes, imagem Docker e `kubectl apply -f k8s/` (manifestos da app) |

Todos os recursos são declarados como **resources de verdade** (`kind_cluster`,
`kubernetes_*`, `helm_release`, `kubectl_manifest`) — **sem `local-exec`**.

## Recursos criados

| Arquivo | Recurso | Descrição |
| --- | --- | --- |
| `cluster.tf` | `kind_cluster.garageos` | Cluster Kubernetes local (kind), com NodePort 30080 mapeado para `localhost` |
| `namespace.tf` | `kubernetes_namespace.garageos` | Namespace `garageos` |
| `metrics-server.tf` | `helm_release.metrics_server` | metrics-server (necessário para o HPA), com `--kubelet-insecure-tls` |
| `database.tf` | `kubernetes_config_map.db_config` | `garageos-db-config` (POSTGRES_USER, POSTGRES_DB) |
| `database.tf` | `kubernetes_secret.db_secret` | `garageos-db-secret` (POSTGRES_PASSWORD) |
| `database.tf` | `kubectl_manifest.postgres_*` | PVC, StatefulSet e Service do PostgreSQL (de `manifests/`) |

## Pré-requisitos

- [Docker](https://www.docker.com/) em execução
- [Terraform](https://developer.hashicorp.com/terraform/install) >= 1.5
- [kind](https://kind.sigs.k8s.io/docs/user/quick-start/#installation)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)

Instalação rápida no Windows (PowerShell):

```powershell
winget install HashiCorp.Terraform
winget install Kubernetes.kind
```

## Como aplicar

```bash
cd infra
terraform init
terraform plan
terraform apply        # cria o cluster + base + banco
```

Ao final, o Terraform imprime os outputs (nome do cluster, kubeconfig, namespace,
URL da API). O `kubectl` já fica apontando para o contexto `kind-garageos`.

### Variáveis configuráveis (`variables.tf`)

| Variável | Padrão | Descrição |
| --- | --- | --- |
| `cluster_name` | `garageos` | Nome do cluster kind |
| `namespace` | `garageos` | Namespace da aplicação |
| `api_node_port` | `30080` | NodePort da API exposto no host |
| `postgres_user` | `garageos` | Usuário do PostgreSQL |
| `postgres_db` | `garageos` | Nome do banco |
| `postgres_password` | `garageos@123` | Senha do PostgreSQL (deve casar com a connection string em `k8s/secret.yaml`) |

Para sobrescrever, use um arquivo `terraform.tfvars` ou `-var`:

```bash
terraform apply -var="postgres_password=umaSenhaForte"
```

## Fluxo completo (IaC → CI/CD)

```
terraform apply (infra/)            →  cluster + namespace + metrics-server + banco
kubectl apply -f k8s/               →  manifestos da app (feito pela pipeline de CD)
```

Depois do deploy da app, a API fica disponível em <http://localhost:30080/swagger>.

## Destruir

```bash
terraform destroy      # remove o cluster kind e tudo que está dentro
```
