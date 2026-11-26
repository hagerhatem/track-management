# Track Management API

A track management API for a MENA music distribution company: artists, tracks, and the distribution of each track to DSPs (Spotify, Apple Music, YouTube).

- Backend: .NET 9 Web API, EF Core 9, SQL Server, FluentValidation, JWT Bearer (Clean Architecture)
- Frontend: Angular 21 (standalone components) in `frontend/track-ui`

## Prerequisites

- .NET 9 SDK
- SQL Server (Express, LocalDB or any instance reachable from your machine)
- Node.js 22 and npm
- Angular CLI (`npm install -g @angular/cli`)
- dotnet-ef tool (`dotnet tool install --global dotnet-ef`)

## Configuration

Secrets are never committed. Set them with dotnet user-secrets from `src/TrackManagement.Api`:

```bash
cd src/TrackManagement.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Issuer" "TrackManagement.Api"
dotnet user-secrets set "Jwt:Audience" "TrackManagement.Client"
dotnet user-secrets set "Jwt:Key" "<random string, at least 32 characters>"
dotnet user-secrets set "Demo:Username" "<demo username>"
dotnet user-secrets set "Demo:Password" "<demo password>"
# optional: use your own SQL Server instead of LocalDB
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS;Database=TrackManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
```

In the Development environment the default connection string is LocalDB (`appsettings.Development.json`). Other environments must set `ConnectionStrings__DefaultConnection`, `AllowedHosts` and `Cors__AllowedOrigins__0` explicitly, otherwise the API refuses to start or rejects requests. `TrustServerCertificate=True` is for local development only.

The API fails at startup if `Jwt:Key` is shorter than 32 characters.

## Database and migrations

The initial migration is committed in `src/TrackManagement.Infrastructure/Migrations`. In the Development environment the API applies migrations and seeds sample data at startup (only into empty tables). To apply migrations manually:

```bash
dotnet ef database update --project src/TrackManagement.Infrastructure --startup-project src/TrackManagement.Api
```

To create a new migration after changing the model:

```bash
dotnet ef migrations add <Name> --project src/TrackManagement.Infrastructure --startup-project src/TrackManagement.Api
```

Seed data (Development only): 3 DSPs, 4 artists, 10 tracks and 17 distributions. Seeding is idempotent: each table is seeded only if it is empty. Tracks and distributions are seeded together and only when both tables are empty, so a database with manual data is left untouched. To see the seed data, point the connection string at a new, empty database name.

## Run the API

```bash
dotnet run --project src/TrackManagement.Api --launch-profile http
```

The API listens on http://localhost:5288. OpenAPI document: /openapi/v1.json, Swagger UI: /swagger (Development only).

## Get a JWT token

The token endpoint exists only in the Development environment (it returns 404 elsewhere). Use the demo credentials you stored in user-secrets:

```bash
curl -s -X POST http://localhost:5288/api/auth/token \
  -H "Content-Type: application/json" \
  --data-binary '{"username":"<demo username>","password":"<demo password>"}'
```

The response contains `accessToken`, `tokenType` and `expiresAtUtc`. The token is valid for 60 minutes. Send it as `Authorization: Bearer <accessToken>`.

The token carries the `Admin` role. The `distribute` and `status` endpoints require it; the other endpoints are public.

## Endpoints

| Method | Route | Auth |
|---|---|---|
| POST | /api/artists | public |
| GET | /api/artists | public |
| POST | /api/tracks | public |
| GET | /api/tracks?artistId=&genre=&status= | public |
| GET | /api/tracks/{id} | public |
| POST | /api/tracks/{id}/distribute | JWT (Admin) |
| PATCH | /api/tracks/{id}/status | JWT (Admin) |
| POST | /api/auth/token | Development only |

Status rules: Draft becomes Submitted only through `distribute`. `PATCH /status` allows Submitted to Distributed (needs at least one Live distribution) and Submitted to Draft. Distributed is final. Errors use ProblemDetails (400 validation, 404 not found, 409 conflict).

## Run the frontend

```bash
cd frontend/track-ui
npm install
ng serve
```

Open http://localhost:4200. The UI reads from http://localhost:5288 (set in `src/environments/environment.ts`), so start the API first. If `npm install` fails with `Cannot read properties of null (reading 'edgesOut')`, use `npm install --legacy-peer-deps` (the project already has a `.npmrc` with this setting). The UI is read-only and has no login; CORS allows only http://localhost:4200.

## Security notes

- Secrets live in user-secrets or environment variables; nothing sensitive is committed. `appsettings.Example.json` has placeholders only.
- JWT: issuer, audience, lifetime and signing key are validated, only HS256 is accepted, and the Admin role is required on the protected endpoints.
- The token endpoint exists only in Development and uses constant-time credential comparison. There is no rate limiting on it.
- Errors never expose stack traces or serializer messages.
- CORS allows only the origins in `Cors:AllowedOrigins`; the API responds with security headers and uses HSTS outside Development.
- Known limits: the UI has no login, there is no pagination, and Production needs explicit configuration (connection string, AllowedHosts, CORS origin) and has no way to obtain a token.

See `DECISIONS.md` for what AI generated, the security issues found, and a mistake that was fixed.
