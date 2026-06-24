---
name: project-structure
description: "Estrutura completa do GarageOS — Clean Architecture .NET 10, stack, configuração e como rodar"
metadata: 
  node_type: memory
  type: project
  originSessionId: 30c16610-3cae-44f4-be13-18f823207726
---

## GarageOS — Estrutura do Projeto (atualizado 2026-06-23)

Sistema de gestão de oficina mecânica implementado em .NET 10 com Clean Architecture.

### Stack Tecnológico

| Componente | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET | 10.0 |
| API Web | ASP.NET Core Web API | 10.0 |
| ORM | Entity Framework Core | 10.0.5 |
| Database | PostgreSQL | 16 |
| Validação | FluentValidation | 12.1.1 |
| Auth | JWT Bearer (built-in) | — |
| Documentação | Swagger / Swashbuckle | 6.9.0 |
| Container | Docker + Docker Compose | — |
| Qualidade | SonarQube Community | — |

### Estrutura de Pastas

```
D:\Pos-tech\GarageOS\
├── Code/
│   ├── GarageOS.Domain/           (entidades, value objects, interfaces de repositório)
│   ├── GarageOS.Application/      (use cases, DTOs, validators)
│   ├── GarageOS.Infrastructure/   (EF Core, repositórios, migrations)
│   ├── GarageOS.Api/              (controllers, middlewares, extensions, Program.cs)
│   ├── GarageOS.UnitTests/
│   ├── GarageOS.IntegrationTests/
│   └── Postman/
├── Documentação/
│   └── Fase 1/SONARQUBE.md
├── docker-compose.yml             (postgres, pgadmin, api, sonarqube)
├── Dockerfile
├── .env.example
└── README.md
```

### Dependências entre Camadas

- **Domain**: sem dependências externas
- **Application** → Domain
- **Infrastructure** → Domain + Application
- **Api** → Application + Infrastructure

### Arquivos Críticos

| O quê | Caminho |
|---|---|
| Solution | `Code/GarageOS.slnx` |
| DbContext | `Code/GarageOS.Infrastructure/Data/GarageOSDbContext.cs` |
| Program.cs | `Code/GarageOS.Api/Program.cs` |
| Extensions | `Code/GarageOS.Api/Extensions/ServiceCollectionExtensions.cs` |
| Middleware | `Code/GarageOS.Api/Middlewares/ExceptionMiddleware.cs` |
| appsettings | `Code/GarageOS.Api/appsettings.json` |
| docker-compose | `docker-compose.yml` (raiz) |

### Como Rodar

```bash
cp .env.example .env
docker compose up -d --build
dotnet ef database update --project Code/GarageOS.Infrastructure --startup-project Code/GarageOS.Api
```

Acessos:
- API: http://localhost:8080
- Swagger: http://localhost:8080/swagger
- PgAdmin: http://localhost:5050
- SonarQube: http://localhost:9000
- Login padrão: POST `/api/Auth/login` com admin/admin@123 → JWT

### Serviços Docker Compose

1. `garageos-postgres` — PostgreSQL 16
2. `garageos-pgadmin` — Interface web do banco
3. `garageos-api` — API na porta 8080
4. `garageos-sonar-db` — PostgreSQL para SonarQube
5. `garageos-sonarqube` — SonarQube na porta 9000
