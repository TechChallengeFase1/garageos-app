---
name: JWT & Authentication Setup
description: How JWT authentication is configured in GarageOS, login flow, and Swagger integration
type: project
---

# JWT Authentication — GarageOS

## Login Endpoint

**POST** `/api/auth/login`

**Request:**
```json
{
  "username": "admin",
  "password": "admin@123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-04-20T12:34:56Z"
}
```

## Token Configuration (appsettings.json)

```json
"Jwt": {
  "SecretKey": "GarageOS@SuperSecretKey#2026!XpTo",
  "Issuer": "GarageOS.Api",
  "Audience": "GarageOS.Client",
  "ExpiresInMinutes": 60
},
"Admin": {
  "Username": "admin",
  "Password": "admin@123"
}
```

**Note:** Admin credentials are hardcoded in config for now. Future: implement User entity and password hashing.

## Protected Endpoints

All endpoints in `ServicosController` require `[Authorize]` attribute. Request without token → 401 Unauthorized.

## Swagger Integration

Security definition is configured in `ServiceCollectionExtensions.AddSwaggerWithJwt()`:
- Security scheme: HTTP Bearer (JWT)
- Click **Authorize** → paste token → all requests include `Authorization: Bearer {token}`

## Files Involved

- **AuthController.cs** — login endpoint, token generation
- **ServiceCollectionExtensions.cs** → `AddJwtAuthentication()` method
- **Program.cs** → `UseAuthentication()` + `UseAuthorization()`
- **appsettings.json** → Jwt + Admin config

## Future Improvements

1. Replace hardcoded admin with User entity + repo
2. Add password hashing (BCrypt, PBKDF2)
3. Implement refresh tokens
4. Add roles/claims-based authorization
