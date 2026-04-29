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

---

## Como rodar (Docker)

Tudo o que o avaliador precisa para subir o projeto está aqui. Pré-requisito: **Docker** e **Docker Compose** instalados.

### 1. Configurar variáveis de ambiente

Na raiz do projeto, copie o template e preencha:

```bash
cp .env.example .env
```

Variáveis principais (sugestão de valores para avaliação):

```env
# Banco principal
POSTGRES_USER=garageos
POSTGRES_PASSWORD=garageos
POSTGRES_DB=garageos

# PgAdmin
[email protected]
PGADMIN_DEFAULT_PASSWORD=admin

# JWT
JWT_SECRET_KEY=uma-chave-secreta-com-no-minimo-32-caracteres
JWT_ISSUER=GarageOS
JWT_AUDIENCE=GarageOS

# Admin (login inicial da API)
ADMIN_USERNAME=admin
ADMIN_PASSWORD=admin

# SonarQube DB
SONAR_DB_USER=sonar
SONAR_DB_PASSWORD=sonar
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
│   └── feats/                    # Documentação por feature
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

## Documentação adicional

- [Enunciado do desafio](Documentação/Fase%201/Fase%201%20-%20Tech%20Challenge.pdf)
- [Domain Storytelling](Documentação/Fase%201/Domain%20Storytelling)
- [Arquitetura em camadas](Code/README.md)
- [Feature: Ordem de Serviço](Code/feats/OrdemDeServico.md)
- [Feature: Testes de integração](Code/feats/Testes_integração.md)

---

## Autores

Trabalho desenvolvido pelo grupo da Pós em Arquitetura de Software — Fase 1.
