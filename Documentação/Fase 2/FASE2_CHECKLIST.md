# GarageOS — Checklist Fase 2

> Análise gerada em 2026-06-23, atualizada após merge da branch de evolução em `analise-fase-2`.
> A coluna **"Como resolver"** indica o caminho de implementação para cada item pendente.

---

## 1. Evolução da Aplicação

### 1.1 Qualidade de Código

| Item | Status | Evidência |
|---|---|---|
| Clean Code (nomes claros, coesão, simplicidade) | ✅ | Nomenclatura em PT-BR consistente com o domínio, métodos coesos |
| Clean Architecture (separação de camadas) | ✅ | 4 camadas: Domain → Application → Infrastructure → Api |
| Testes unitários cobrindo fluxos críticos | ✅ | 86,5% de cobertura confirmada pelo SonarQube (Fase 1) |
| Testes de integração | ✅ | WebApplicationFactory + InMemoryDatabase |

### 1.2 APIs Obrigatórias

| Requisito | Status | Evidência | Como resolver |
|---|---|---|---|
| Abertura de OS recebendo cliente, veículo, serviços e peças em **um único request** | ❌ | `POST /api/OrdensDeServico` ainda recebe apenas `ClienteId` + `VeiculoId`. Serviços e peças continuam sendo adicionados por endpoints separados | Criar DTO unificado `AbrirOrdemDeServicoRequest` com listas de serviços e peças, e use case que orquestre criação + vinculação em uma única transação |
| Consulta de status da OS | ✅ | `GET /api/OrdensDeServico/acompanhar/{numeroOS}` (público) + `GET /api/OrdensDeServico/{id}` | — |
| Aprovação/recusa de orçamento por notificação externa | ✅ | `PATCH /api/OrdensDeServico/{id}/orcamento/resposta` — endpoint público sem autenticação | — |
| Listagem com ordenação: EmExecucao > AguardandoAprovacao > EmDiagnostico > Recebida | ✅ | Implementado no `OrdemDeServicoRepository.ListarTodosAsync()` via `OrderBy` com peso numérico por status, `ThenBy(CriadoEm)` | — |
| Listagem excluindo OS Finalizadas e Entregues | ✅ | `.Where(x => x.Status != Finalizada && x.Status != Entregue)` no repositório | — |
| Atualização de status da OS via ferramenta externa (e-mail) | ✅ | `PATCH /api/OrdensDeServico/{id}/orcamento/resposta` — endpoint público que permite notificação externa de aprovação/recusa, atendendo o requisito conforme validado pelo professor | — |

---

## 2. Infraestrutura

### 2.1 Conteinerização (Docker)

| Item | Status | Arquivo | Como resolver |
|---|---|---|---|
| Dockerfile funcional (multi-stage build) | ✅ | `Dockerfile` | — |
| Dockerfile roda como usuário **não-root** | ❌ | `Dockerfile` — sem instrução `USER` | Adicionar `RUN adduser --disabled-password appuser && chown -R appuser /app` + `USER appuser` na stage `runtime` |
| `.dockerignore` presente | ❌ | Arquivo inexistente | Criar `.dockerignore` excluindo `bin/`, `obj/`, `.git/`, `.env`, `*.md` |
| `docker-compose.yml` para desenvolvimento local | ✅ | `docker-compose.yml` | — |
| `healthcheck` no serviço `postgres` | ❌ | `docker-compose.yml` — nenhum serviço tem healthcheck | Adicionar `healthcheck` com `pg_isready` ao serviço `postgres` |
| `condition: service_healthy` no `depends_on` da API | ❌ | `docker-compose.yml` — `depends_on: - postgres` sem condition | Alterar `depends_on` da API para `condition: service_healthy` após adicionar healthcheck |

### 2.2 Kubernetes

| Item | Status | Arquivo esperado | Como resolver |
|---|---|---|---|
| Namespace | ❌ | `/k8s/namespace.yaml` | Criar namespace `garageos` |
| ConfigMap (variáveis não-sensíveis) | ❌ | `/k8s/configmap.yaml` | Mapear `ASPNETCORE_ENVIRONMENT`, `Jwt__Issuer`, `Jwt__Audience` |
| Secret (variáveis sensíveis) | ❌ | `/k8s/secret.yaml` | Mapear `Jwt__SecretKey`, `ConnectionStrings__DefaultConnection`, credenciais do admin — valores em base64 |
| StatefulSet do PostgreSQL | ❌ | `/k8s/postgres-statefulset.yaml` | StatefulSet com 1 réplica, PVC de 5Gi, variáveis via Secret |
| Service do PostgreSQL (ClusterIP) | ❌ | `/k8s/postgres-service.yaml` | ClusterIP na porta 5432, acessível apenas internamente no cluster |
| PersistentVolumeClaim do PostgreSQL | ❌ | `/k8s/postgres-pvc.yaml` | PVC com 5Gi e `ReadWriteOnce` |
| Deployment da API | ❌ | `/k8s/api-deployment.yaml` | 2 réplicas mínimas, imagem do Docker Hub, vars de ConfigMap + Secret, liveness/readiness probes |
| Service da API (LoadBalancer ou NodePort) | ❌ | `/k8s/api-service.yaml` | NodePort na porta 30080 (para kind local) |
| HorizontalPodAutoscaler (HPA) | ❌ | `/k8s/hpa.yaml` | Min 2 / Max 10 réplicas, escala com CPU > 70% |

### 2.3 Infraestrutura como Código (Terraform)

| Item | Status | Arquivo esperado | Como resolver |
|---|---|---|---|
| Provider configuration | ❌ | `/infra/providers.tf` | Configurar provider `tehcyx/kind` para cluster local |
| Criação do cluster kind | ❌ | `/infra/main.tf` | Recurso `kind_cluster` com 1 control-plane + 2 workers |
| Variáveis | ❌ | `/infra/variables.tf` | `cluster_name`, `node_image`, `k8s_version` |
| Outputs | ❌ | `/infra/outputs.tf` | `kubeconfig`, `cluster_endpoint` |
| Documentação dos recursos | ❌ | `/infra/README.md` | Listar recursos criados + comandos `terraform init/plan/apply` |

### 2.4 CI/CD (GitHub Actions)

| Item | Status | Arquivo esperado | Como resolver |
|---|---|---|---|
| Pasta `.github/workflows/` | ❌ | `.github/workflows/ci-cd.yml` | Criar pasta e arquivo de pipeline |
| Job: Build da aplicação | ❌ | — | `dotnet build` em todas as camadas |
| Job: Execução dos testes | ❌ | — | `dotnet test` para unit + integration tests |
| Job: Build da imagem Docker | ❌ | — | `docker build` com tag versionada |
| Job: Push para Docker Hub | ❌ | — | `docker push` usando secrets `DOCKERHUB_USERNAME` + `DOCKERHUB_TOKEN` |
| Job: Deploy no cluster K8s | ❌ | — | `kubectl apply -f k8s/` usando kubeconfig via secret |
| Deploy do banco (migration automática) | ❌ | — | Job de migration via `dotnet ef database update` ou init container no K8s |
| Aplicação dos manifestos YAML | ❌ | — | `kubectl apply -f k8s/` na etapa de deploy |

---

## 3. Documentação (README.md)

| Item | Status | Como resolver |
|---|---|---|
| Descrição da Fase 2 e seus objetivos | ❌ | Adicionar seção no `README.md` descrevendo evolução para infra escalável |
| Desenho da arquitetura (componentes + infra + fluxo de deploy) | ❌ | Adicionar diagrama Mermaid ou imagem no README |
| Instruções de execução local (docker-compose) | ✅ | Já existe no README atual |
| Instruções de deploy em Kubernetes | ❌ | Adicionar seção com comandos `kubectl` e pré-requisitos |
| Instruções de provisionamento com Terraform | ❌ | Adicionar seção com `terraform init`, `plan`, `apply` |
| Link para coleção Postman / Swagger | ✅ | Collection Postman existe em `Code/Postman/` |
| Link para vídeo demonstrativo (YouTube/Vimeo, até 15 min) | ❌ | Gravar e publicar vídeo mostrando deploy, CI/CD, consumo das APIs e escalabilidade |

---

## 4. Melhorias Sugeridas pelo Professor

| Item | Status | Como resolver |
|---|---|---|
| Dockerfile com usuário não-root | ❌ | Ver seção 2.1 |
| `.dockerignore` | ❌ | Ver seção 2.1 |
| `healthcheck` no Postgres + `condition: service_healthy` | ❌ | Ver seção 2.1 |
| Migrar testes de integração de InMemoryDatabase para Testcontainers | ⚠️ Opcional | Adicionar pacote `Testcontainers.PostgreSql`, ajustar `ApiFactory.cs` para subir container real de Postgres nos testes |

---

## 5. Resumo por Área

| Área | Concluído | Pendente | % |
|---|---|---|---|
| Evolução da Aplicação — Qualidade | 4 | 0 | 100% |
| Evolução da Aplicação — APIs | 5 | 1 | 83% |
| Docker | 2 | 4 | 33% |
| Kubernetes | 0 | 9 | 0% |
| Terraform (IaC) | 0 | 5 | 0% |
| CI/CD | 0 | 8 | 0% |
| Documentação | 2 | 5 | 29% |

---

## 6. Ordem de Execução Recomendada

```
1. [CÓDIGO]       Endpoint de abertura de OS com payload único (cliente + veículo + serviços + peças)
2. [DOCKER]       Dockerfile não-root + .dockerignore + healthcheck no docker-compose
3. [K8S]          Criar manifests em /k8s (namespace → secret/configmap → postgres → api → hpa)
4. [TERRAFORM]    Criar /infra com provider kind + cluster
5. [CI/CD]        Criar .github/workflows/ci-cd.yml
6. [README]       Atualizar documentação com arquitetura, K8s, Terraform e link do vídeo
7. [VÍDEO]        Gravar demonstração do ambiente completo (≤ 15 min)
```
