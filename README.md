# GarageOS

Sistema de gestão para oficinas mecânicas — controle de clientes, veículos, serviços, estoque e ordens de serviço (OS) com fluxo completo de orçamento, execução e acompanhamento público pelo cliente.

Projeto desenvolvido como **Tech Challenge — Fase 1** da Pós-graduação em Arquitetura de Software.

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
| Testes | xUnit, Moq, FluentAssertions |
| Qualidade | SonarQube (Community) |
| Container | Docker + Docker Compose |

Arquitetura: **Clean Architecture** com separação em `Domain`, `Application`, `Infrastructure` e `Api` — detalhes em [Code/README.md](Code/README.md).

### Por que PostgreSQL?

Para este projeto, optou-se pela utilização do PostgreSQL como sistema gerenciador de banco de dados. A escolha foi motivada principalmente por ser uma solução open source e gratuita, permitindo reduzir custos de licenciamento sem comprometer desempenho, segurança ou confiabilidade. Trata-se de uma tecnologia amplamente consolidada no mercado, com excelente documentação e forte suporte da comunidade.

Outro fator relevante foi a experiência prévia da equipe com bancos de dados relacionais e com o próprio PostgreSQL, o que contribuiu para uma curva de aprendizado menor, maior produtividade no desenvolvimento e mais segurança na implementação.

O PostgreSQL também se encaixa diretamente nas necessidades do sistema: o projeto exige diversas relações entre entidades como clientes, veículos, ordens de serviço, serviços executados, peças utilizadas, insumos e controle de tempo de execução. O modelo relacional facilita a organização dos dados, garante integridade referencial e permite consultas estruturadas para acompanhamento operacional e geração de relatórios. Dessa forma, o PostgreSQL apresentou-se como uma solução adequada tanto do ponto de vista técnico quanto estratégico.

---

## Como rodar (Docker)

Tudo o que o avaliador precisa para subir o projeto está aqui. Pré-requisito: **Docker** e **Docker Compose** instalados.

### 1. Configurar variáveis de ambiente

Na raiz do projeto, copie o template e preencha:

```bash
cp .env.example .env
```

Variáveis principais (valores reais para avaliação):

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=dtsx        
POSTGRES_DB=GarageOS

PGADMIN_EMAIL=admin@garageos.com
PGADMIN_DEFAULT_PASSWORD=dtsx

JWT_SECRET_KEY=GarageOS@SuperSecretKey#2026!XpTo
JWT_ISSUER=GarageOS.Api
JWT_AUDIENCE=GarageOS.Client

ADMIN_USERNAME=admin
ADMIN_PASSWORD=admin@123

SONAR_DB_USER=sonar
SONAR_DB_PASSWORD=sonarsenha123

SONAR_TOKEN=sqa_97ad0979eff1583c655df83642233cfd18e1b69f // seu token do SonarQube, obtenha em: http://localhost:9000/account/security
SONAR_HOST_URL=http://localhost:9000
SONAR_PROJECT_KEY=GarageOSToken // chave do projeto no SonarQube, geralmente o nome do projeto, por exemplo: GarageOSProject
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
| `garageos-sonar-db` | — | Banco do Sonar |

> A API roda **migrations automaticamente** no startup, não é preciso aplicar nada manualmente.

### 3. Acessar

- **API**: <http://localhost:8080>
- **Swagger** (documentação interativa): <http://localhost:8080/swagger>
- **PgAdmin**: <http://localhost:5050>
- **SonarQube**: <http://localhost:9000> (login inicial: `admin` / `admin`)

### 4. Login

`POST /api/Auth/login` com o `ADMIN_USERNAME` e `ADMIN_PASSWORD` definidos no `.env`. O token JWT retornado deve ser usado no header `Authorization: Bearer <token>` para as rotas protegidas.

### 5. Parar os containers

```bash
docker compose down
```

Para remover também os volumes (zera o banco):

```bash
docker compose down -v
```

---

## Endpoints principais

Coleção Postman pronta em [Code/Postman/GarageOS.postman_collection.json](Code/Postman/GarageOS.postman_collection.json).

| Recurso | Endpoint base | Operações |
|---|---|---|
| Auth | `/api/Auth` | login |
| Clientes | `/api/Clientes` | CRUD |
| Veículos | `/api/Veiculos` | CRUD + vincular cliente |
| Serviços | `/api/Servicos` | CRUD |
| Estoques | `/api/Estoques` | CRUD |
| Ordens de Serviço | `/api/OrdensDeServico` | criar, listar, adicionar serviços/peças, alterar status, gerar/enviar orçamento, registrar resposta do cliente, **aging**, **acompanhamento público** |

---

## Estrutura do repositório

```
GarageOS/
├── Code/
│   ├── GarageOS.Api/             # Controllers, middlewares, Program.cs
│   ├── GarageOS.Application/     # Use cases, DTOs, validators
│   ├── GarageOS.Domain/          # Entidades, regras de negócio puras
│   ├── GarageOS.Infrastructure/  # EF Core, repositórios, migrations
│   ├── GarageOS.UnitTests/
│   ├── GarageOS.IntegrationTests/
│   ├── Postman/                  # Coleção pronta para importar
│   ├── sonar-scan.sh / .ps1      # Scripts de análise SonarQube
├── Documentação/
│   └── Fase 1/                   # Domain Storytelling, enunciado, SonarQube
├── Dockerfile
├── docker-compose.yml
└── .env.example
```

---

## Testes

Sem Docker, dentro de `Code/`:

```bash
# Unitários
dotnet test GarageOS.UnitTests/GarageOS.UnitTests.csproj

# Integração (sobe um banco em container via Testcontainers)
dotnet test GarageOS.IntegrationTests/GarageOS.IntegrationTests.csproj
```

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

Mais detalhes em [Documentação/Fase 1/SONARQUBE.md](Documentação/Fase%201/SONARQUBE.md).

---

## Autores

Trabalho desenvolvido pelo grupo da Pós em Arquitetura de Software — Fase 1.
