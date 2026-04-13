# 📦 Pacotes NuGet Recomendados por Camada

## 🎯 GarageOS.Domain
*Camada de domínio - Mínimas dependências*

```bash
# Nenhum pacote essencial - use apenas .NET base
```

**Opcional (se necessário):**
```bash
dotnet add GarageOS.Domain package FluentValidation
```

---

## 📋 GarageOS.Application
*Orquestração de casos de uso*

```bash
# Validação de dados
dotnet add GarageOS.Application package FluentValidation

# MediatR para padrão CQRS (opcional)
dotnet add GarageOS.Application package MediatR

# AutoMapper para DTO mapping (opcional)
dotnet add GarageOS.Application package AutoMapper
```

---

## 🔧 GarageOS.Infrastructure
*Implementações concretas*

```bash
# Entity Framework Core
dotnet add GarageOS.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add GarageOS.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add GarageOS.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer

# Ou para PostgreSQL (trocar SQL Server)
# dotnet add GarageOS.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL

# Logging
dotnet add GarageOS.Infrastructure package Serilog
dotnet add GarageOS.Infrastructure package Serilog.Extensions.Logging

# Serviços de Email (opcional)
dotnet add GarageOS.Infrastructure package SendGrid
# ou
dotnet add GarageOS.Infrastructure package MailKit

# Integração com APIs (opcional)
dotnet add GarageOS.Infrastructure package HttpClientFactory
```

---

## 🚀 GarageOS.Api
*Camada de apresentação*

```bash
# Swagger/OpenAPI
dotnet add GarageOS.Api package Swashbuckle.AspNetCore

# CORS
dotnet add GarageOS.Api package Microsoft.AspNetCore.Cors

# Health Checks
dotnet add GarageOS.Api package AspNetCore.HealthChecks.SqlServer

# Logging
dotnet add GarageOS.Api package Serilog.AspNetCore
```

---

## 🧪 Testes (Projeto Separado - GarageOS.Tests)
*Para criar projetos de teste*

```bash
dotnet new xunit -n GarageOS.Tests

# Framework de testes
dotnet add GarageOS.Tests package xunit
dotnet add GarageOS.Tests package xunit.runner.visualstudio

# Mocking
dotnet add GarageOS.Tests package Moq

# Assertions fluentes
dotnet add GarageOS.Tests package FluentAssertions

# Dados de teste
dotnet add GarageOS.Tests package Bogus
```

---

## 📊 Stack Recomendado Completo

### Mínimo Viável (Essencial)
- ✅ Entity Framework Core
- ✅ FluentValidation
- ✅ Swashbuckle (Swagger)

### Padrão (Recomendado)
- ✅ Tudo acima +
- ✅ Serilog (Logging)
- ✅ AutoMapper (DTO mapping)
- ✅ MediatR (CQRS - opcional)

### Completo (Robusto)
- ✅ Tudo acima +
- ✅ Health Checks
- ✅ Testes (xUnit + Moq + FluentAssertions)
- ✅ Serviço de Email
- ✅ CORS

---

## 🔄 Comando Rápido - Instalar Stack Recomendado

```bash
cd Code

# Domain (vazio)
# Já feito

# Application
dotnet add GarageOS.Application/GarageOS.Application.csproj package FluentValidation
dotnet add GarageOS.Application/GarageOS.Application.csproj package MediatR

# Infrastructure
dotnet add GarageOS.Infrastructure/GarageOS.Infrastructure.csproj package Microsoft.EntityFrameworkCore
dotnet add GarageOS.Infrastructure/GarageOS.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add GarageOS.Infrastructure/GarageOS.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add GarageOS.Infrastructure/GarageOS.Infrastructure.csproj package Serilog
dotnet add GarageOS.Infrastructure/GarageOS.Infrastructure.csproj package AutoMapper

# Api
dotnet add GarageOS.Api/GarageOS.Api.csproj package Swashbuckle.AspNetCore
dotnet add GarageOS.Api/GarageOS.Api.csproj package Serilog.AspNetCore
dotnet add GarageOS.Api/GarageOS.Api.csproj package Microsoft.AspNetCore.Cors
```

---

## 💡 Dicas

1. **Versões**: Use versões estáveis e recentes (LTS quando possível)
2. **Compatibilidade**: Todos os pacotes acima são compatíveis com .NET 10
3. **Gradual**: Instale conforme necessário - não adicione tudo de uma vez
4. **Documentação**: Consulte a documentação oficial de cada pacote
5. **NuGet.org**: Verifique em https://www.nuget.org/ para versões mais recentes

---

## 🎯 Arquitetura Clean com os Pacotes

```
Domain (Puro .NET)
    ↓
Application (FluentValidation, MediatR)
    ↓
Infrastructure (EF Core, Serilog, AutoMapper)
    ↓
Api (Swashbuckle, Serilog.AspNetCore, CORS)
```

A hierarquia de dependências é mantida!
