# GarageOS

Sistema de gestão para oficinas mecânicas — controle de clientes, veículos, serviços, estoque e ordens de serviço (OS), com fluxo completo de orçamento, execução e acompanhamento público pelo cliente.

Projeto desenvolvido como **Tech Challenge** da Pós-graduação em Arquitetura de Software.

---

## Objetivo

Entregar uma API REST robusta que cubra a operação ponta a ponta de uma oficina:

- Cadastro de **clientes**, **veículos**, **serviços** e **itens de estoque**.
- Criação e gestão de **ordens de serviço** (OS), com status, serviços executados e peças consumidas.
- Fluxo de **orçamento**: geração, envio ao cliente, aprovação/recusa.
- **Acompanhamento público** da OS pelo cliente (sem autenticação).
- **Aging** das OS para análise gerencial do tempo médio de execução.
- Autenticação **JWT** com usuário administrador.

---

## Stack

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 10 |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Banco | PostgreSQL 16 |
| Validação | FluentValidation |
| Auth | JWT Bearer |
| Testes | xUnit, Moq, FluentAssertions, Testcontainers |
| Qualidade | SonarQube (Community) |
| Container | Docker + Docker Compose |
| Deploy | Kubernetes (kind) + Terraform |
| CI/CD | GitHub Actions |

Arquitetura: **Clean Architecture** com separação em `Domain`, `Application`, `Infrastructure` e `Api`.

Mais detalhes:

- [Desenho de arquitetura](docs/arquitetura.md)
- [Infraestrutura Terraform](infra/README.md)
- [Documentação SonarQube](Documentação/Fase%201/SONARQUBE.md)

### Por que PostgreSQL?

Para este projeto, optou-se pela utilização do PostgreSQL como sistema gerenciador de banco de dados. A escolha foi motivada principalmente por ser uma solução open source e gratuita, permitindo reduzir custos de licenciamento sem comprometer desempenho, segurança ou confiabilidade. Trata-se de uma tecnologia amplamente consolidada no mercado, com excelente documentação e forte suporte da comunidade.

Outro fator relevante foi a experiência prévia da equipe com bancos de dados relacionais e com o próprio PostgreSQL, o que contribuiu para uma curva de aprendizado menor, maior produtividade no desenvolvimento e mais segurança na implementação.

O PostgreSQL também se encaixa diretamente nas necessidades do sistema: o projeto exige diversas relações entre entidades como clientes, veículos, ordens de serviço, serviços executados, peças utilizadas, insumos e controle de tempo de execução. O modelo relacional facilita a organização dos dados, garante integridade referencial e permite consultas estruturadas para acompanhamento operacional e geração de relatórios. Dessa forma, o PostgreSQL apresentou-se como uma solução adequada tanto do ponto de vista técnico quanto estratégico.

---

## Como rodar localmente com Docker

Pré-requisitos:

- Docker e Docker Compose instalados.
- SDK .NET 10, caso queira rodar comandos `dotnet` localmente.
- `dotnet-ef`, caso queira executar migrations manualmente.

### 1. Configurar variáveis de ambiente

Na raiz do projeto, copie o template e preencha:

```bash
cp .env.example .env
```

Exemplo de preenchimento:

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=dtsx
POSTGRES_DB=GarageOS

PGADMIN_EMAIL=admin@garageos.com
PGADMIN_DEFAULT_PASSWORD=dtsx

JWT_SECRET_KEY=troque-esta-chave-em-ambientes-reais
JWT_ISSUER=GarageOS.Api
JWT_AUDIENCE=GarageOS.Client

ADMIN_USERNAME=admin
ADMIN_PASSWORD=admin@123

SONAR_DB_USER=sonar
SONAR_DB_PASSWORD=sonarsenha123
SONAR_TOKEN=
SONAR_HOST_URL=http://localhost:9000
SONAR_PROJECT_KEY=GarageOS
```

### 2. Subir os containers

Na raiz do projeto:

```bash
docker compose up -d --build
```

Esse comando sobe:

| Serviço | Porta | Descrição |
|---|---|---|
| `garageos-api` | **8080** | API GarageOS |
| `garageos-postgres` | 5432 | Banco principal |
| `garageos-pgadmin` | 5050 | Interface web do Postgres |
| `garageos-sonarqube` | 9000 | Análise de qualidade (opcional) |
| `garageos-sonar-db` | — | Banco do SonarQube |

A API aplica automaticamente as migrations pendentes ao iniciar. Para desenvolvimento local, também é possível executar manualmente:

```bash
dotnet ef database update --project Code/GarageOS.Infrastructure --startup-project Code/GarageOS.Api
```

### 3. Acessar

- **API**: <http://localhost:8080>
- **Swagger** (documentação interativa): <http://localhost:8080/swagger>
- **PgAdmin**: <http://localhost:5050>
- **SonarQube**: <http://localhost:9000> (login inicial: `admin` / `admin`)

### 4. Login

`POST /api/Auth/login` com o `ADMIN_USERNAME` e `ADMIN_PASSWORD` definidos no `.env`.

O token JWT retornado deve ser usado no header das rotas protegidas:

```http
Authorization: Bearer <token>
```

### 5. Parar os containers

```bash
docker compose down
```

Para remover também os volumes (zera o banco):

```bash
docker compose down -v
```

---

## Documentação da API

A documentação interativa está disponível via Swagger em `http://localhost:8080/swagger` após subir o ambiente. Para explorar e testar os endpoints, importe a coleção Postman pronta: [Code/Postman/GarageOS.postman_collection.json](Code/Postman/GarageOS.postman_collection.json).



---

## Testes

Sem Docker Compose, dentro de `Code/`:

```bash
# Unitários
dotnet test GarageOS.UnitTests/GarageOS.UnitTests.csproj

# Integração (sobe um banco em container via Testcontainers)
dotnet test GarageOS.IntegrationTests/GarageOS.IntegrationTests.csproj
```

Os testes de integração usam Testcontainers com PostgreSQL, então o Docker precisa estar em execução.

---

## Análise de qualidade (SonarQube)

O SonarQube já está incluso no `docker compose`. Para rodar a análise completa do código:

```bash
# Linux / macOS
cd Code && ./sonar-scan.sh

# Windows (PowerShell)
cd Code; ./sonar-scan.ps1
```

O resultado aparece em <http://localhost:9000>. Pré-requisitos: `dotnet-sonarscanner` instalado como ferramenta global e `SONAR_TOKEN` preenchido no `.env`.

---

## Deploy com Kubernetes e Terraform

Além do ambiente local com Docker Compose, o projeto possui infraestrutura para execução em Kubernetes local usando **kind** e **Terraform**.

A divisão de responsabilidades segue o enunciado do Tech Challenge:

| Camada | Diretório | Responsabilidade |
|---|---|---|
| IaC | `infra/` | Provisiona a **base**: cluster kind, namespace, metrics-server e PostgreSQL (PVC, StatefulSet, Service, ConfigMap, Secret) |
| App | `k8s/` | Manifestos da **aplicação**: Deployment, Service, ConfigMap, Secret e HPA |
| Pipelines | `.github/workflows/` | CI (build + testes) e CD (push de imagem + deploy completo) |
| Arquitetura | `docs/arquitetura.md` | Desenho da arquitetura, pipeline e infraestrutura |

### Infraestrutura como Código (Terraform)

O Terraform provisiona toda a base necessária sem uso de `local-exec`:

| Arquivo | Recurso criado |
|---|---|
| `cluster.tf` | Cluster kind `garageos`, com NodePort 30080 mapeado para `localhost` |
| `namespace.tf` | Namespace `garageos` |
| `metrics-server.tf` | Helm release do metrics-server (necessário para o HPA funcionar) |
| `database.tf` | ConfigMap, Secret, PVC, StatefulSet e Service do PostgreSQL |

Pré-requisitos:

- Docker em execução.
- [Terraform](https://developer.hashicorp.com/terraform/install) >= 1.5
- [kind](https://kind.sigs.k8s.io/docs/user/quick-start/#installation)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)

Instalação rápida no Windows (PowerShell):

```powershell
winget install HashiCorp.Terraform
winget install Kubernetes.kind
```

Para provisionar a infraestrutura local:

```bash
cd infra
terraform init
terraform plan
terraform apply
```

Depois, a partir da raiz do repositório, aplique os manifestos da aplicação:

```bash
kubectl apply -f k8s/
```

Após o deploy, a API fica disponível em:

<http://localhost:30080/swagger>

### Escalabilidade horizontal (HPA)

O manifesto `k8s/hpa.yaml` configura um **Horizontal Pod Autoscaler** para a API. Ele monitora o uso de CPU e escala automaticamente o número de réplicas do Deployment dentro dos limites definidos, garantindo disponibilidade sob carga sem intervenção manual. O metrics-server provisionado pelo Terraform é o componente que alimenta o HPA com as métricas de uso.

Mais detalhes:

- [Infraestrutura como Código](infra/README.md)
- [Desenho da arquitetura proposta](docs/arquitetura.md)

---

## Pipelines CI/CD (GitHub Actions)

O projeto tem dois workflows independentes em `.github/workflows/`:

### CI — `ci.yml`

Roda em todo **push para `main`** e em **pull requests** abertos para `main`.

| Etapa | O que faz |
|---|---|
| Build | `dotnet build` na solução completa |
| Testes unitários | `dotnet test` em `GarageOS.UnitTests` |
| Testes de integração | `dotnet test` em `GarageOS.IntegrationTests` (sobe PostgreSQL via Testcontainers) |
| Docker build | Constrói a imagem `garageos-api:{sha}` para validar o Dockerfile |

### CD — `cd.yml`

Roda no **merge para `main`** e também pode ser disparado manualmente via `workflow_dispatch`.

| Etapa | O que faz |
|---|---|
| Build + testes | Mesmos passos do CI (garantia antes do deploy) |
| Push da imagem | Build e push para o Docker Hub: `garageosfiap/garageos-api:latest` e `garageosfiap/garageos-api:{sha}` |
| Terraform apply | Provisiona cluster kind + namespace + metrics-server + banco no runner |
| kubectl apply | Aplica os manifestos de `k8s/` no cluster |
| Smoke test | `curl` na rota `/swagger/index.html` para confirmar que a API respondeu 200 |

A imagem pública está disponível no Docker Hub em: [`garageosfiap/garageos-api`](https://hub.docker.com/r/garageosfiap/garageos-api)

---

## Estrutura do repositório

```text
GarageOS/
├── .github/workflows/          # CI/CD com GitHub Actions
├── Code/
│   ├── GarageOS.Api/           # Controllers, middlewares, Program.cs
│   ├── GarageOS.Application/   # Use cases, DTOs, validators
│   ├── GarageOS.Domain/        # Entidades, value objects e regras de domínio
│   ├── GarageOS.Infrastructure/ # EF Core, repositórios, migrations
│   ├── GarageOS.UnitTests/
│   ├── GarageOS.IntegrationTests/
│   ├── Postman/                # Coleção pronta para importar
│   └── sonar-scan.sh / .ps1    # Scripts de análise SonarQube
├── docs/                       # Desenho e documentação de arquitetura
├── infra/                      # Terraform para infraestrutura Kubernetes local
├── k8s/                        # Manifestos Kubernetes da aplicação
├── Documentação/
│   └── Fase 1/                 # Domain Storytelling, enunciado, SonarQube
├── Dockerfile
├── docker-compose.yml
└── .env.example
```

---

## Autores

Trabalho desenvolvido pelo grupo da Pós em Arquitetura de Software.
