---
name: test-projects
description: "Projetos de teste unitário e integração do GarageOS — organização, ferramentas e comandos"
metadata: 
  node_type: memory
  type: project
  originSessionId: 30c16610-3cae-44f4-be13-18f823207726
---

## Projetos de Teste (atualizado 2026-06-23)

### GarageOS.UnitTests

- **Finalidade**: Testes rápidos de Domain e Application (sem I/O real)
- **Ferramentas**: xUnit 2.9.3, Moq, FluentAssertions 8.9.0, Bogus 35.6.1
- **Referências**: Domain + Application apenas
- **Estrutura**:
  - `Domain/Entities/`
  - `Domain/ValueObjects/`
  - `Domain/Aggregates/`
  - `Application/UseCases/`
  - `Application/Validators/`
- **Guia**: `Code/GarageOS.UnitTests/UNIT_TESTS_README.md`

### GarageOS.IntegrationTests

- **Finalidade**: Testes HTTP de controllers + repositórios contra banco real
- **Ferramentas**: xUnit 2.9.3, Microsoft.AspNetCore.Mvc.Testing 10.0.5, EF InMemory 10.0.5, FluentAssertions 8.9.0, Bogus 35.6.1, coverlet
- **Referências**: Api + Infrastructure + Application + Domain
- **Estrutura**:
  - `Api/Controllers/`
  - `Infrastructure/Repositories/`
  - `Auth/`
  - `Fixtures/ApiFactory.cs` — WebApplicationFactory customizada
  - `Helpers/`
- **Guia**: `Code/GarageOS.IntegrationTests/INTEGRATION_TESTS_README.md`

### Comandos

```bash
dotnet test                                              # todos os testes
dotnet test Code/GarageOS.UnitTests                     # unitários
dotnet test Code/GarageOS.IntegrationTests              # integração
dotnet test --logger "console;verbosity=detailed"
dotnet watch test Code/GarageOS.UnitTests
```
