# GarageOS — Instruções para Claude

## Visão Geral

**GarageOS** é um sistema de gerenciamento de garagem implementado em .NET 10 com Clean Architecture (4 camadas).

### Stack
- **.NET 10** — Framework
- **PostgreSQL 18** — Banco de dados (Docker)
- **Entity Framework Core 10** + Npgsql — ORM
- **ASP.NET Core Controllers** — API REST
- **JWT (JwtBearer)** — Autenticação
- **Swashbuckle 6.9.0** — Swagger/OpenAPI
- **xUnit + Moq + FluentAssertions** — Testes
- **FluentValidation** — Validação de requests

---

## Estrutura de Diretórios

```
d:\Pos-tech\GarageOS\
├── Code/
│   ├── GarageOS.Domain/              (Camada de Domínio)
│   ├── GarageOS.Application/         (Camada de Aplicação)
│   ├── GarageOS.Infrastructure/      (Camada de Infraestrutura)
│   ├── GarageOS.Api/                 (Camada de API)
│   ├── GarageOS.UnitTests/           (Testes unitários)
│   └── GarageOS.IntegrationTests/    (Testes de integração)
├── docker-compose.yml                (PostgreSQL + pgAdmin)
└── Documentação/
```

---

## Setup Local

### 1. Clonar e restaurar dependências

```bash
cd d:\Pos-tech\GarageOS
dotnet restore
```

### 2. Subir banco de dados + pgAdmin

```bash
docker compose up -d
```

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| **PostgreSQL** | `localhost:5432` | user: `postgres` / password: `dtsx` |
| **pgAdmin** | `http://localhost:5050` | email: `admin@garageos.com` / password: `dtsx` |

Para conectar ao banco no pgAdmin:
- Host: `postgres` (nome do serviço Docker, não `localhost`)
- Port: `5432`
- Database: `GarageOS`
- Username: `postgres`
- Password: `dtsx`

### 3. Rodar migrations e criar tabelas

```bash
cd Code
dotnet ef database update --project GarageOS.Infrastructure --startup-project GarageOS.Api
```

### 4. Iniciar a API

```bash
dotnet run --project GarageOS.Api
```

API roda em: `http://localhost:5129` (HTTP) ou `https://localhost:7231` (HTTPS)

Swagger: `http://localhost:5129/swagger`

---

## Autenticação JWT

### Login

```http
POST http://localhost:5129/api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin@123"
}
```

Resposta:
```json
{
  "token": "eyJhbGc...",
  "expiresAt": "2026-04-20T12:34:56Z"
}
```

### Usar token no Swagger

1. Clique em **Authorize** (ícone de cadeado)
2. Cole o token (sem `Bearer `)
3. **Authorize**
4. Faça requests autenticadas

---

## Arquitetura — Clean Architecture

### Domain Layer (`GarageOS.Domain`)
Entidades, interfaces de repositório, exceções de domínio — **sem dependências externas**.

**Exemplo:** `Domain/Entities/Servico.cs`
```csharp
public class Servico
{
    public Guid Id { get; private set; }
    public string NomeServico { get; private set; }
    public decimal Preco { get; private set; }
    
    public Servico(string nomeServico, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(nomeServico)) throw new ArgumentException("...");
        if (preco <= 0) throw new ArgumentException("...");
        Id = Guid.NewGuid();
        NomeServico = nomeServico;
        Preco = preco;
    }
}
```

### Application Layer (`GarageOS.Application`)
DTOs, Use Cases (orquestração de lógica), Validators — depende apenas de Domain.

**Estrutura:**
```
DTOs/
  Auth/
    LoginRequest.cs
    TokenResponse.cs
  Servicos/
    CriarServicoRequest.cs
    ServicoResponse.cs
UseCases/
  Servicos/
    ListarServicosUseCase.cs
    CadastrarServicoUseCase.cs
    ObterServicoUseCase.cs
    AlterarServicoUseCase.cs
Validators/
  Servicos/
    CriarServicoValidator.cs
    AtualizarServicoValidator.cs
```

### Infrastructure Layer (`GarageOS.Infrastructure`)
DbContext, Entity Mappings, Repository implementations — implementações concretas.

**Chave:** `GarageOS.Infrastructure/Data/GarageOSDbContext.cs` com FluentAPI mappings.

### API Layer (`GarageOS.Api`)
Controllers, Program.cs, middleware configuration, extensions.

**Endpoints protegidos com `[Authorize]`:**
- `GET /api/servicos` — listar (requer token)
- `POST /api/servicos` — criar (requer token)
- `GET /api/servicos/{id}` — obter (requer token)
- `PUT /api/servicos/{id}` — atualizar (requer token)
- `POST /api/auth/login` — login (público)

---

## Testes

### Unit Tests
```bash
dotnet test GarageOS.UnitTests
```

Cobrem: Entidades (domínio), Use Cases (lógica), validações.

### Integration Tests
```bash
dotnet test GarageOS.IntegrationTests
```

Usam `WebApplicationFactory` com banco `InMemory` — não dependem do PostgreSQL.

### Testes de integração com pgAdmin
No pgAdmin, você pode validar manualmente que as tabelas foram criadas:
- Schema: `public`
- Tabela: `Servicos` (campos: Id, NomeServico, Preco)

---

## Configuração de Credenciais

### `appsettings.json` (versionado)
Contém estrutura padrão com placeholders. **Nunca commit senhas aqui.**

### `appsettings.Development.json` (local, não versionado)
Cada dev sobrescreve aqui com suas credenciais locais. Arquivo adicionado ao `.gitignore`.

**Exemplo:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=GarageOS;Username=postgres;Password=dtsx"
  }
}
```

---

## Troubleshooting

### Erro: "password authentication failed for user 'postgres'"

1. Verifique se há PostgreSQL local instalado (pode estar na porta 5432):
   ```bash
   netstat -ano | findstr :5432
   ```

2. Se sim, pare o serviço local (`services.msc`) ou mude a porta Docker para `5433`.

3. Verifique que o container está rodando:
   ```bash
   docker compose ps
   ```

4. Se necessário, recrie o volume:
   ```bash
   docker compose down -v
   docker compose up -d
   ```

### Erro: "doesn't reference Microsoft.EntityFrameworkCore.Design"

```bash
dotnet restore
```

Se persistir:
```bash
cd Code/GarageOS.Api
dotnet remove package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.5
```

### Swagger retorna 404

- Acesse `http://localhost:5129/swagger` (não `/swagger/index.html`)
- Verifique que está em ambiente **Development** (env var `ASPNETCORE_ENVIRONMENT`)

---

## Próximos Passos

1. **Nova entidade:** Siga o padrão Servico (Domain → Application → Infrastructure → API)
2. **Novo use case:** Crie DTOs, Validators, UseCase, registre em `ServiceCollectionExtensions`
3. **Testes:** Unit tests para regra de negócio, Integration tests para endpoints
4. **Migrations:** Após alterar modelos, rode `dotnet ef migrations add NomeMigration`

---

## Contato / Documentação

- **Arquitetura:** Ver `Documentação/` pasta para Domain Storytelling, Layer diagrams
- **API:** Swagger em `http://localhost:5129/swagger`
- **Banco:** pgAdmin em `http://localhost:5050`
