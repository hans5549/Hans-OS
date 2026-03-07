# CGMSportFinance

Monorepo bootstrap for `vue-vben-admin` frontend plus an ASP.NET Core 9 backend API.

## Structure

- `frontend/`: imported `vue-vben-admin` monorepo, with `apps/web-antd` configured for backend-driven auth
- `backend/`: ASP.NET Core 9 Web API, EF Core, PostgreSQL, Identity, Swagger, and integration tests
- `infra/`: environment examples for infrastructure setup

## Backend

### Seeded users

- `vben` / `123456`
- `admin` / `123456`
- `jack` / `123456`

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

The backend now loads its default database connection string and JWT signing key from the encrypted file `backend/src/CGMSportFinance.Api/appsettings.secrets.enc.json`.

The encryption key is currently hardcoded in `backend/src/CGMSportFinance.Secrets/EmbeddedSecretsKey.cs` as a temporary convenience tradeoff.

Migrations are stored under `backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Migrations`.

At runtime the API still supports overrides through standard PostgreSQL environment variables:

- `PGHOST`
- `PGPORT`
- `PGDATABASE`
- `PGUSER`
- `PGPASSWORD`
- optional: `PGSSLMODE`

ASP.NET Core configuration overrides also still work:

- `ConnectionStrings__DefaultConnection`
- `Jwt__SigningKey`

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
- The current encrypted secrets approach is convenience-first and should be replaced by environment-managed secrets later.
