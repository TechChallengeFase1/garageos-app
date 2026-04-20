# GarageOS - Documentação do Projeto

Documentação da estrutura e padrões do projeto GarageOS para uso do Claude Code.

## 📁 Estrutura do Projeto

```
Code/
├── GarageOS.Domain/              → Camada de Domínio (entidades, agregados, interfaces)
├── GarageOS.Application/         → Camada de Aplicação (use cases, DTOs, validadores)
├── GarageOS.Infrastructure/      → Camada de Infraestrutura (EF Core, repositórios, serviços)
├── GarageOS.Api/                 → Camada de Apresentação (controllers, middlewares, Program.cs)
├── GarageOS.UnitTests/           → Testes unitários
├── GarageOS.IntegrationTests/    → Testes de integração
├── GarageOS.slnx                 → Arquivo de solução
├── CLAUDE.md                     → Guia para Claude Code
├── README.md                     → Documentação principal
└── PACOTES_RECOMENDADOS.md      → Pacotes NuGet por camada
```

## 🏗️ Clean Architecture - Regras Fundamentais

### Dependências Entre Camadas (Inbound Only)
```
Domain (sem dependências externas)
   ↑
Application (depende de Domain)
   ↑
Infrastructure (depende de Domain + Application)
   ↑
Api (depende de Application + Infrastructure)
```

**Regra de Ouro**: Camadas externas nunca importam camadas internas.

### Camada Domain
- **Localização**: `GarageOS.Domain/`
- **Responsabilidade**: Lógica pura de negócio
- **Sem**: EF Core, Controllers, Services externos
- **Contém**: 
  - Entities (classes de domínio com lógica)
  - Value Objects (objetos sem identidade)
  - Aggregates (agregados raíz)
  - Interfaces de Repository (definições, não implementações)
  - Exceptions de domínio

**Exemplo**:
```csharp
// GarageOS.Domain/Entities/Cliente.cs
public class Cliente
{
    public int Id { get; private set; }
    public string Nome { get; private set; }
    
    // Lógica de negócio pura
    public bool PodeAgendar() => Status == ClienteStatus.Ativo;
}
```

### Camada Application
- **Localização**: `GarageOS.Application/`
- **Responsabilidade**: Orquestração de casos de uso
- **Depende de**: Domain apenas
- **Contém**:
  - Use Cases (para cada ação do sistema)
  - DTOs (Request/Response para tráfego de dados)
  - Validators (FluentValidation)
  - Interfaces de serviços (não implementações)

**Padrão de Use Case**:
```csharp
// GarageOS.Application/UseCases/Cliente/CadastrarClienteUseCase.cs
public class CadastrarClienteUseCase
{
    private readonly IClienteRepository _repository;
    
    public CadastrarClienteUseCase(IClienteRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<ClienteResponse> Execute(CadastrarClienteRequest request)
    {
        // Validar (usar validador)
        // Criar entidade
        // Persisten via repository
        // Retornar DTO
    }
}
```

### Camada Infrastructure
- **Localização**: `GarageOS.Infrastructure/`
- **Responsabilidade**: Implementações técnicas concretas
- **Depende de**: Domain + Application
- **Contém**:
  - DbContext (Entity Framework Core)
  - Implementações de Repository (herdam IRepository de Domain)
  - Serviços externos (Email, APIs)
  - Configurações de banco de dados

**Exemplo**:
```csharp
// GarageOS.Infrastructure/Repositories/ClienteRepository.cs
public class ClienteRepository : IClienteRepository
{
    private readonly GarageOSDbContext _context;
    
    public async Task<Cliente> ObterPorIdAsync(int id)
    {
        return await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);
    }
}
```

### Camada Api
- **Localização**: `GarageOS.Api/`
- **Responsabilidade**: Endpoints HTTP, configuração, middlewares
- **Depende de**: Application + Infrastructure
- **Contém**:
  - Controllers (endpoints)
  - Middlewares (tratamento de erros, logging)
  - Extensions (configuração de serviços)
  - Program.cs (setup da aplicação)

**Padrão de Controller**:
```csharp
// GarageOS.Api/Controllers/ClientesController.cs
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly CadastrarClienteUseCase _useCase;
    
    [HttpPost]
    public async Task<ActionResult<ClienteResponse>> Cadastrar(CadastrarClienteRequest request)
    {
        var response = await _useCase.Execute(request);
        return CreatedAtAction(nameof(Obter), new { id = response.Id }, response);
    }
}
```

## 🔧 Comandos Principais

### Build e Execução
```bash
# Build
cd Code && dotnet build

# Executar API
dotnet run --project GarageOS.Api

# Swagger: https://localhost:5001/swagger
```

### Banco de Dados (PostgreSQL)
```bash
# Iniciar containers (da raiz do projeto)
docker compose up -d

# Criar migration
dotnet ef migrations add NomeMigration --project GarageOS.Infrastructure

# Aplicar migrations
dotnet ef database update --project GarageOS.Infrastructure
```

### Testes
```bash
# Todos os testes
dotnet test

# Testes unitários
dotnet test GarageOS.UnitTests

# Testes de integração
dotnet test GarageOS.IntegrationTests

# Teste específico
dotnet test --filter "NomeDoTeste"
```

## 📋 Fluxo de Desenvolvimento para Nova Feature

### 1. Design no Domain
```
GarageOS.Domain/
├── Entities/NovaEntidade.cs        (a entidade com lógica)
├── ValueObjects/NovoVO.cs           (se aplicável)
└── Repositories/INovaRepository.cs   (interface)
```

### 2. Aplicação (Use Cases)
```
GarageOS.Application/
├── UseCases/NovaFeature/
│   ├── CriarUseCase.cs
│   └── ObterUseCase.cs
├── DTOs/
│   ├── CriarRequest.cs
│   ├── CriarResponse.cs
│   └── NovaResponse.cs
└── Validators/
    └── CriarValidator.cs
```

### 3. Infraestrutura (Persistência)
```
GarageOS.Infrastructure/
├── Data/GarageOSDbContext.cs        (configurar mapping)
└── Repositories/NovaRepository.cs   (implementar INovaRepository)
```

### 4. API (Endpoints)
```
GarageOS.Api/
└── Controllers/NovaController.cs    (HTTP endpoints)
```

### 5. Testes
```
GarageOS.UnitTests/
└── UseCases/CriarUseCaseTests.cs

GarageOS.IntegrationTests/
└── Controllers/NovaControllerTests.cs
```

## 🔐 Autenticação (JWT)

- **Configuração**: `GarageOS.Api/Program.cs` (AddJwtAuthentication)
- **Secrets**: `GarageOS.Api/appsettings.json` (Jwt section)
- **Admin padrão**: username: `admin`, password: `admin@123`
- **Expiração**: 60 minutos
- **Middleware**: Aplicado globalmente em Program.cs

Qualquer controller que necessite autenticação deve ter `[Authorize]`:
```csharp
[Authorize]
[HttpPost]
public async Task<ActionResult> AcaoProtegida()
{
    // ...
}
```

## 📊 Entity Framework Core

- **DbContext**: `GarageOS.Infrastructure/Data/GarageOSDbContext.cs`
- **Migrations**: `GarageOS.Infrastructure/Data/Migrations/`
- **Connection String**: `GarageOS.Api/appsettings.json` (ConnectionStrings.DefaultConnection)
- **Database**: PostgreSQL 18

### Padrão OnConfiguring (não usar)
Use `appsettings.json` em vez de hardcoding no DbContext.

### Padrão de Mapeamento
Implemente `IEntityTypeConfiguration<T>` para cada entidade:
```csharp
public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nome).HasMaxLength(100).IsRequired();
    }
}
```

## 🧪 Padrão de Testes

### Testes Unitários
- Testam lógica de domínio em isolamento
- Use Moq para mockar dependências
- Nenhuma dependência do banco de dados

```csharp
[Fact]
public void CadastrarCliente_DeveValidarNome()
{
    // Arrange
    var validator = new CadastrarClienteValidator();
    var request = new CadastrarClienteRequest { Nome = "" };
    
    // Act
    var result = validator.Validate(request);
    
    // Assert
    Assert.False(result.IsValid);
}
```

### Testes de Integração
- Testam fluxo completo: controller → use case → banco
- Usam banco de dados de teste
- Verificam responses HTTP reais

```csharp
[Fact]
public async Task Post_DeveRetornar201_QuandoCadastroValido()
{
    // Arrange com dados de teste
    // Act chamando HTTP
    // Assert verificando response e banco
}
```

## ⚙️ Configuração (Program.cs)

Estrutura padrão:
1. Criar builder
2. Adicionar serviços (infraestrutura, aplicação, autenticação)
3. Build app
4. Configurar middlewares (swagger, autenticação, autorização)
5. Map controllers e rodar

Verificar `GarageOS.Api/Extensions/ServiceCollectionExtensions.cs` para helpers.

## 🚫 Antigos Erros Comuns

- ❌ Lógica de negócio em controllers
- ❌ EF Core imports na Domain
- ❌ Testes mocking o banco de dados sem usar repositório
- ❌ DTOs diretamente com Entities (sempre mapear)
- ❌ Infraestrutura sendo referenciada por Domain/Application
- ❌ Use cases retornando Entities em vez de DTOs

## 📚 Referências

- [CLAUDE.md](../CLAUDE.md) - Guia rápido para Claude Code
- [README.md](../README.md) - Documentação principal
- [PACOTES_RECOMENDADOS.md](../PACOTES_RECOMENDADOS.md) - NuGet packages
- [GarageOS.Domain/DomainLayer.md](../GarageOS.Domain/DomainLayer.md) - Detalhes da Domain
- [GarageOS.Application/ApplicationLayer.md](../GarageOS.Application/ApplicationLayer.md) - Detalhes da Application
- [GarageOS.Infrastructure/InfrastructureLayer.md](../GarageOS.Infrastructure/InfrastructureLayer.md) - Detalhes da Infrastructure
- [GarageOS.Api/ApiLayer.md](../GarageOS.Api/ApiLayer.md) - Detalhes da Api
