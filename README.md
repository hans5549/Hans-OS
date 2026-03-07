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

The backend defaults to PostgreSQL via `ConnectionStrings:DefaultConnection` in `backend/src/CGMSportFinance.Api/appsettings.json`. Replace it with the connection details you provide later.

Migrations are stored under `backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Migrations`.

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
- Once you provide the PostgreSQL connection info, only environment/config updates should be needed.
