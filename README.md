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

`frontend/apps/web-antd` is the production frontend app. It runs in backend access mode and reads the API base URL from `VITE_GLOB_API_URL`.

Local development keeps Vben's `/api` proxy and mock capability so the UI can boot before the backend is finished. Production builds are expected to override `VITE_GLOB_API_URL` in CI.

```bash
cd frontend
corepack enable
pnpm install
pnpm dev:antd
```

For local production-like builds:

```bash
cd frontend
$env:VITE_GLOB_API_URL="https://your-api-host/api"
pnpm build:antd
```

## Deployment

- Push to `main` triggers GitHub Actions
- Frontend changes under `frontend/**` deploy to Azure Static Web Apps
- The frontend workflow builds `frontend/apps/web-antd/dist` and uploads the prebuilt assets
- Required GitHub secrets:
  - `AZURE_STATIC_WEB_APPS_API_TOKEN`
  - `VITE_GLOB_API_URL`
- Backend deployment stays separate from the frontend workflow

## Notes

- Refresh token rotation uses an HttpOnly cookie named `refresh_token`
- Single environment (personal project, no staging/production split)
