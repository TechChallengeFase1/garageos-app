---
name: Setup & Troubleshooting Guide
description: Common setup issues, solutions, and verified configurations for GarageOS
type: project
---

# Setup & Troubleshooting

## Quick Start (Verified Steps)

```bash
# 1. Restore dependencies
dotnet restore

# 2. Start Docker services
docker compose up -d

# 3. Run migrations
dotnet ef database update --project Code/GarageOS.Infrastructure --startup-project Code/GarageOS.Api

# 4. Start API
dotnet run --project Code/GarageOS.Api

# 5. Access
# API: http://localhost:5129
# Swagger: http://localhost:5129/swagger
# pgAdmin: http://localhost:5050
```

## Common Issues & Fixes

### 1. "Password authentication failed for user 'postgres'"

**Cause:** Local PostgreSQL installed on port 5432 is intercepting Docker connection.

**Solution:**
```bash
# Check what's on port 5432 (Windows)
netstat -ano | findstr :5432

# If local PostgreSQL found: stop it (services.msc) OR change Docker port
# In docker-compose.yml:
ports:
  - "5433:5432"

# Then update connection string Port: 5433
```

### 2. "EF Core Design package not found" error

**Cause:** NuGet cache issue after package changes.

**Solution:**
```bash
dotnet restore Code/GarageOS.Api
# If persists:
cd Code/GarageOS.Api
dotnet remove package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.5
```

### 3. Swagger returns 404

**Cause:** Wrong URL or development environment not detected.

**Solution:**
- Use `http://localhost:5129/swagger` (not `/swagger/index.html`)
- Check `ASPNETCORE_ENVIRONMENT=Development` is set
- Verify port 5129 matches launchSettings.json

### 4. pgAdmin can't connect to PostgreSQL

**Cause:** Using `localhost` instead of service name `postgres`.

**Solution:** In pgAdmin connection form:
- Host: `postgres` ← (service name in docker-compose, NOT localhost)
- Port: `5432`
- User: `postgres`
- Password: `dtsx`

### 5. Migration shows "0 tests available"

**Cause:** xUnit 2.x + xunit.runner.visualstudio 3.x incompatibility.

**Status:** Not yet fixed in this project. Tests run fine via `dotnet test` despite warning.

## Verified Working Configuration

- **OS:** Windows 11
- **Docker Desktop:** Latest
- **PostgreSQL:** 18 (container)
- **.NET SDK:** 10.0.201
- **VS Code:** Latest + C# DevKit
- **Visual Studio:** 2022+ (for Package Manager Console)

## Credential Security Notes

- ✅ `appsettings.json` → committed with placeholder `Password=CHANGE_ME`
- ✅ `appsettings.Development.json` → **NOT committed** (in .gitignore)
- Each dev creates their own `appsettings.Development.json` locally
- Local override values take precedence over `appsettings.json` in Development environment

## Database Reset (Development Only)

```bash
# Destroy and recreate PostgreSQL with fresh data
docker compose down -v
docker compose up -d
dotnet ef database update --project Code/GarageOS.Infrastructure --startup-project Code/GarageOS.Api
```

## Docker Compose Services

| Service | Port | Credentials | Note |
|---------|------|-------------|------|
| PostgreSQL | 5432 | user: `postgres` / pwd: `dtsx` | Persistent volume `garageos_pgdata` |
| pgAdmin | 5050 | email: `admin@garageos.com` / pwd: `dtsx` | UI for database management |

