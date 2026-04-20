---
name: GarageOS Project Structure
description: Clean Architecture 4-layer project structure for garage management system
type: project
---

# GarageOS — Estrutura do Projeto

**Localização:** `d:\Pos-tech\GarageOS\Code`

## Camadas (Clean Architecture)

### 1. Domain Layer (`GarageOS.Domain`)
- Entidades pure (Servico)
- Interfaces de repositório (IServicoRepository)
- Exceções de domínio (ServicoNaoEncontradoException)
- **Sem dependências externas**

### 2. Application Layer (`GarageOS.Application`)
- DTOs (LoginRequest, TokenResponse, CriarServicoRequest, etc.)
- Use Cases (ListarServicosUseCase, CadastrarServicoUseCase, etc.)
- Validators (FluentValidation)
- **Depende apenas de Domain**

### 3. Infrastructure Layer (`GarageOS.Infrastructure`)
- DbContext (GarageOSDbContext)
- Entity Mappings (ServicoConfiguration)
- Repository implementations (ServicoRepository)
- **Concretiza interfaces do Domain**

### 4. API Layer (`GarageOS.Api`)
- Controllers (ServicosController, AuthController)
- Program.cs + middleware setup
- ServiceCollectionExtensions (DI)
- appsettings.json + appsettings.Development.json

## Test Projects

- **GarageOS.UnitTests** — Unit tests (xUnit + Moq)
- **GarageOS.IntegrationTests** — Integration tests com WebApplicationFactory + InMemory DB

## Padrão: Vertical Slice

Cada feature (ex: Servicos) é implementada end-to-end:
1. Entity no Domain
2. DTOs + UseCase + Validator na Application
3. Repository + DbConfiguration na Infrastructure
4. Controller na API
5. Tests em ambos projetos de testes
