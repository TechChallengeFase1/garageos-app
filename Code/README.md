# 🏗️ GarageOS - Arquitetura em Camadas (.NET 10)

Projeto **GarageOS** implementado seguindo **Clean Architecture** com separação clara de responsabilidades em 4 camadas.

## 📁 Estrutura do Projeto

```
GarageOS/
├── GarageOS.Domain/           → Regras de negócio puras
├── GarageOS.Application/      → Orquestração de casos de uso
├── GarageOS.Infrastructure/   → Implementações concretas
└── GarageOS.Api/              → Endpoints HTTP (ASP.NET Core)
```

## 🎯 Camadas Explicadas

### 1️⃣ **Domain** - Coração da Aplicação
- ❌ Zero dependências de frameworks
- ✅ Entities, Value Objects, Aggregates
- ✅ Interfaces de Repositório
- ✅ Exceções do domínio

**Mudança de BD SQL → MongoDB? Esta camada não muda!**

📖 Leia: [GarageOS.Domain/DomainLayer.md](GarageOS.Domain/DomainLayer.md)

### 2️⃣ **Application** - Maestro da Orquestração
- Depende APENAS de Domain
- Use Cases e DTOs
- Validação de entrada
- Coordena as operações

📖 Leia: [GarageOS.Application/ApplicationLayer.md](GarageOS.Application/ApplicationLayer.md)

### 3️⃣ **Infrastructure** - Implementação Técnica
- Depende de Domain + Application
- Entity Framework Core
- Implementações de Repositório
- Serviços externos (Email, APIs)

📖 Leia: [GarageOS.Infrastructure/InfrastructureLayer.md](GarageOS.Infrastructure/InfrastructureLayer.md)

### 4️⃣ **Api** - Camada de Apresentação
- Depende de Application + Infrastructure
- Controllers HTTP
- Middlewares
- Configuração da aplicação

📖 Leia: [GarageOS.Api/ApiLayer.md](GarageOS.Api/ApiLayer.md)

## 🚀 Quick Start

### Pré-requisitos
- ✅ .NET 10 SDK instalado
- ✅ Visual Studio, VS Code ou Rider

### 1. Abrir a solução
```bash
cd Code
# Abrir no Visual Studio
start GarageOS.slnx

# Ou via CLI
dotnet build
```

### 2. Executar a API
```bash
dotnet run --project GarageOS.Api
```

A API estará disponível em: `https://localhost:5001` (ou conforme launchSettings.json)

### 3. Visualizar a documentação Swagger
Acesse: `https://localhost:5001/swagger`

## 📚 Guia de Desenvolvimento

### Passo 1: Criar a Camada de Domínio
```
GarageOS.Domain/
├── Entities/
│   ├── Cliente.cs
│   ├── Veiculo.cs
│   ├── OrdemServico.cs
│   └── Servico.cs
├── ValueObjects/
│   ├── Endereco.cs
│   └── Telefone.cs
├── Aggregates/
│   └── ClienteAggregate.cs
├── Repositories/
│   ├── IClienteRepository.cs
│   ├── IVeiculoRepository.cs
│   └── IOrdemServicoRepository.cs
└── Exceptions/
    ├── ClienteNaoEncontradoException.cs
    └── VeiculoNaoEncontradoException.cs
```

### Passo 2: Criar a Camada de Aplicação
```
GarageOS.Application/
├── UseCases/
│   ├── Cliente/
│   │   ├── CadastrarClienteUseCase.cs
│   │   ├── ObterClienteUseCase.cs
│   │   └── ListarClientesUseCase.cs
│   └── Veiculo/
│       └── CadastrarVeiculoUseCase.cs
├── DTOs/
│   ├── ClienteRequest.cs
│   ├── ClienteResponse.cs
│   ├── VeiculoRequest.cs
│   └── VeiculoResponse.cs
└── Validators/
    ├── CadastrarClienteValidator.cs
    └── CadastrarVeiculoValidator.cs
```

### Passo 3: Implementar a Infraestrutura
```
GarageOS.Infrastructure/
├── Data/
│   ├── GarageOSDbContext.cs
│   └── Migrations/
├── Repositories/
│   ├── ClienteRepository.cs
│   ├── VeiculoRepository.cs
│   └── OrdemServicoRepository.cs
├── ExternalServices/
│   └── EmailService.cs
└── Mappings/
    └── MappingConfig.cs
```

### Passo 4: Criar os Controllers na API
```
GarageOS.Api/
├── Controllers/
│   ├── ClientesController.cs
│   ├── VeiculosController.cs
│   └── OrdensServicoController.cs
├── Middlewares/
│   └── ExceptionHandlingMiddleware.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
└── Program.cs
```

## 💾 Banco de Dados

### Configurar Conexão
Edite `GarageOS.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GarageOS;Trusted_Connection=true;"
  }
}
```

### Criar Migrations
```bash
# Criar migration
dotnet ef migrations add Initial --project GarageOS.Infrastructure

# Aplicar migration
dotnet ef database update --project GarageOS.Infrastructure
```

## 📦 Adicionar Pacotes

Veja [PACOTES_RECOMENDADOS.md](PACOTES_RECOMENDADOS.md) para uma lista completa de pacotes NuGet.

### Stack Básico
```bash
# Entity Framework Core
dotnet add GarageOS.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer

# Validação
dotnet add GarageOS.Application package FluentValidation

# Swagger
dotnet add GarageOS.Api package Swashbuckle.AspNetCore
```

## 🧪 Testes

### Criar projeto de testes
```bash
dotnet new xunit -n GarageOS.Tests
dotnet add GarageOS.Tests reference GarageOS.Domain
dotnet add GarageOS.Tests reference GarageOS.Application
dotnet add GarageOS.Tests package Moq
dotnet add GarageOS.Tests package FluentAssertions
```

### Executar testes
```bash
dotnet test
```

## 🔒 Regras de Ouro

### ✅ FAÇA
- ✅ Domain com zero dependências externas
- ✅ Application depende APENAS de Domain
- ✅ Infrastructure implementa interfaces de Domain
- ✅ Testes para cada camada
- ✅ DTOs para tráfego de dados

### ❌ NÃO FAÇA
- ❌ EF Core na Domain
- ❌ Controllers na Application
- ❌ Lógica de negócio na Api
- ❌ Infrastructure ser referenciado por Domain/Application
- ❌ Misturar camadas

## 📚 Referências Úteis

- [Clean Architecture - Robert Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [Entity Framework Core Docs](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Docs](https://learn.microsoft.com/en-us/aspnet/core/)

## 🚨 Próximas Etapas

1. [ ] Criar as Entities baseado no Domain Storytelling
2. [ ] Implementar Repositories
3. [ ] Criar Use Cases da Aplicação
4. [ ] Implementar DbContext do EF Core
5. [ ] Criar Controllers HTTP
6. [ ] Escrever testes unitários
7. [ ] Documentar as APIs no Swagger
8. [ ] Fazer testes de integração

## 📧 Suporte

Dúvidas sobre a arquitetura? Revise os arquivos `.md` em cada camada ou consulte a documentação oficial.

---

**Status**: ✅ Estrutura base pronta para desenvolvimento
**Stack**: .NET 10, Entity Framework Core, ASP.NET Core Web API
**Padrão**: Clean Architecture + Domain-Driven Design
