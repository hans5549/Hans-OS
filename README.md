# Hans-OS

Personal assistant monorepo: Vue Vben Admin frontend + ASP.NET Core 9 backend API.

## Structure

- `frontend/`: Vben Admin monorepo (`apps/web-antd`), backend-driven auth and routing
- `backend/`: ASP.NET Core 9 Web API, EF Core, PostgreSQL, Identity, Swagger

## Backend

### Roles

One role (`admin`) and one user (`hans`) are created by the `SeedEssentialData` EF Core migration.

### Run

```bash
cd backend
dotnet build HansOS.slnx
dotnet test HansOS.slnx
dotnet run --project src/HansOS.Api
```

### API docs

- Swagger UI: `http://localhost:5180/swagger`
- Health check: `http://localhost:5180/health`

### Database

PostgreSQL. Connection string configured via environment variables in production:

- `ConnectionStrings__DefaultConnection`
- `Jwt__SigningKey`
- `Frontend__AllowedOrigins__0`

Migrations are stored under `backend/src/HansOS.Api/Migrations/` and auto-applied on startup.

## Frontend

`apps/web-antd` runs in backend access mode. Dev API traffic proxied to `http://localhost:5180`.

```bash
cd frontend
corepack enable
pnpm install
pnpm dev:antd
```

## Deployment

- Push to `main` triggers GitHub Actions
- Backend → Azure App Service (migrations auto-applied on startup)
- Frontend → Azure Static Web Apps

## Notes

- Refresh token rotation uses an HttpOnly cookie named `refresh_token`
- Single environment (personal project, no staging/production split)
