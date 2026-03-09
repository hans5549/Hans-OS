# CGMSportFinance

Monorepo bootstrap for `vue-vben-admin` frontend plus an ASP.NET Core 9 backend API.

## Structure

- `frontend/`: imported `vue-vben-admin` monorepo, with `apps/web-antd` configured for backend-driven auth
- `backend/`: ASP.NET Core 9 Web API, EF Core, PostgreSQL, Identity, Swagger, and integration tests
- `infra/`: environment examples for infrastructure setup

## Backend

### Seeded users

Development and test environments seed these demo users by default:

- `vben` / `123456`
- `admin` / `123456`
- `jack` / `123456`

Production does not create these demo users unless `Seeding__EnableDemoData=true` is explicitly configured.

### Production bootstrap admin

Production startup expects a bootstrap admin to be provided through environment variables when no privileged user exists yet:

- `BootstrapAdmin__Username`
- `BootstrapAdmin__Email`
- `BootstrapAdmin__Password`

Optional bootstrap admin settings:

- `BootstrapAdmin__RealName`
- `BootstrapAdmin__HomePath`
- `BootstrapAdmin__Avatar`

### Run

```bash
cd backend
dotnet build CGMSportFinance.sln
dotnet test CGMSportFinance.sln
dotnet run --project src/CGMSportFinance.Api
```

### API docs

- Swagger UI: `http://localhost:5180/swagger`
- OpenAPI JSON: `http://localhost:5180/swagger/v1/swagger.json`

### Database

For production, prefer environment-managed settings such as Azure App Service `Environment variables`. The application supports standard PostgreSQL environment variables:

- `PGHOST`
- `PGPORT`
- `PGDATABASE`
- `PGUSER`
- `PGPASSWORD`
- optional: `PGSSLMODE`

ASP.NET Core configuration overrides also work:

- `ConnectionStrings__DefaultConnection`
- `Frontend__AllowedOrigins__0`
- `Jwt__SigningKey`
- `BootstrapAdmin__Username`
- `BootstrapAdmin__Email`
- `BootstrapAdmin__Password`

Migrations are stored under `backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Migrations`.

The encrypted file `backend/src/CGMSportFinance.Api/appsettings.secrets.enc.json` is still available for local convenience and fallback scenarios, but it should not be the source of truth for production secrets.

### Encrypted secrets maintenance

Regenerate the encrypted secrets file from a plaintext JSON file with this helper:

```bash
cd backend
dotnet run --project tools/CGMSportFinance.SecretsCli -- encrypt /path/to/plain-secrets.json src/CGMSportFinance.Api/appsettings.secrets.enc.json
```

To inspect the current encrypted file locally:

```bash
cd backend
dotnet run --project tools/CGMSportFinance.SecretsCli -- decrypt src/CGMSportFinance.Api/appsettings.secrets.enc.json
```

## Frontend

`apps/web-antd` now runs in backend access mode and points development API traffic at `http://localhost:5180/api`.

```bash
cd frontend
corepack enable
pnpm install
pnpm dev:antd
```

## Notes

- Refresh token rotation uses an HttpOnly cookie named `refresh_token`.
- Frontend mock mode is disabled in development.
- Production deployment guidance lives in [backend/docs/azure-app-service.md](/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/docs/azure-app-service.md).
