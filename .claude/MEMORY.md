# GarageOS — Memory Index

## Project Information
- [Project Structure](projects/project_structure.md) — 4-layer Clean Architecture layout
- [Technology Stack](projects/tech_stack.md) — .NET 10, PostgreSQL 18, Swashbuckle 6.9.0, JWT auth
- [JWT & Authentication](projects/jwt_auth.md) — Login flow, token config, protected endpoints
- [Setup & Troubleshooting](projects/setup_troubleshooting.md) — Docker setup, common issues & fixes

## Key File Locations
- **Main project:** `d:\Pos-tech\GarageOS\Code`
- **Docker:** `d:\Pos-tech\GarageOS\docker-compose.yml` (PostgreSQL + pgAdmin)
- **Configuration:** `Code/GarageOS.Api/appsettings.json` (versionado) + `appsettings.Development.json` (local)
- **Tests:** `GarageOS.UnitTests/`, `GarageOS.IntegrationTests/`

## Quick Reference
- 📍 **API:** `http://localhost:5129` (HTTP) / `https://localhost:7231` (HTTPS)
- 📊 **Swagger:** `http://localhost:5129/swagger`
- 💾 **pgAdmin:** `http://localhost:5050` (user: `admin@garageos.com` / pwd: `dtsx`)
- 🐘 **PostgreSQL:** `localhost:5432` (user: `postgres` / pwd: `dtsx`)
- 🔐 **Admin login:** username: `admin` / password: `admin@123`
