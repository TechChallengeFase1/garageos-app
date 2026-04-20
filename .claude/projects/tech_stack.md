---
name: Technology Stack & Versions
description: GarageOS tech stack, all NuGet packages and their versions
type: project
---

# Stack Técnico — GarageOS

## Framework & Runtime
- **.NET 10.0.5** (latest stable)
- **C# 13**

## Database & ORM
- **PostgreSQL 18** (via Docker)
- **Entity Framework Core 10.0.5** (`Microsoft.EntityFrameworkCore`)
- **Npgsql 10.0.1** (PostgreSQL driver for EF)
- **Microsoft.EntityFrameworkCore.Design 10.0.5** (migrations CLI)

## API & Authentication
- **ASP.NET Core Controllers** (traditional, not Minimal APIs)
- **JWT (JwtBearer)** via `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.5`
- **Swashbuckle.AspNetCore 6.9.0** (Swagger/OpenAPI)
  - **NOT 10.x** — downgraded from 10.1.7 due to `Microsoft.OpenApi 2.x` namespace issues
  - **Stable 6.9.0** uses `Microsoft.OpenApi 1.x` with standard `Models` namespace

## Validation
- **FluentValidation 12.1.1**

## Testing
- **xUnit 2.9.3** 
  - **xunit.runner.visualstudio 3.1.4** (discovery in VS)
  - NOTE: Test discovery issue if xunit 2.x + runner 3.x mismatch
- **Moq** (mocking)
- **FluentAssertions 8.9.0** (readable assertions)
- **Microsoft.AspNetCore.Mvc.Testing 10.0.5** (integration tests)

## Docker
- **postgres:18** image
- **dpage/pgadmin4** image
- **docker-compose.yml** (no explicit version, uses latest)

## Development Dependencies
- **Coverlet.collector 6.0.4** (code coverage in tests)
- **Microsoft.NET.Test.Sdk 17.14.1** (test runner framework)

## Key Version Decisions

- ✅ **.NET 10.0** — latest, aligned with C# 13
- ✅ **Swashbuckle 6.9.0** — stable, well-documented
  - ❌ Swashbuckle 10.1.7 caused `Microsoft.OpenApi.Models` namespace issue
  - 📝 Safe to upgrade 6.9.0 → 7.x when released
- ✅ **PostgreSQL 18** — latest stable
- ✅ **JWT authentication** — standard, industry practice

## Connection String (appsettings.Development.json)
```
Host=localhost;Database=GarageOS;Username=postgres;Password=dtsx
```
