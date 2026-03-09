# Azure App Service Deployment

This document describes the baseline production deployment flow for the backend API on Azure App Service with Azure Database for PostgreSQL.

## 1. Create or confirm the App Service

Create a Web App in Azure App Service or confirm an existing one.

- Publish: `Code`
- Operating system: `Linux`
- Runtime stack: choose the supported `.NET` runtime that matches the project target framework
- Region: keep it close to the Azure PostgreSQL server

This repository currently targets `net9.0` in [CGMSportFinance.Api.csproj](/Users/hansh/Documents/Personal Assistant/Hans-OS/backend/src/CGMSportFinance.Api/CGMSportFinance.Api.csproj). If the Azure portal in your region doesn't offer `.NET 9`, stop there and verify the supported runtimes before deploying.

Useful checks:

```bash
az webapp list-runtimes --os linux | grep DOTNET
az webapp config show --resource-group <resource-group-name> --name <app-name> --query linuxFxVersion
```

Record these values because they are needed in the next steps:

- App Service name
- Resource group
- Default hostname, for example `https://<app-name>.azurewebsites.net`

## 2. Configure App Service application settings

In Azure Portal, open the App Service and go to `Settings > Environment variables`.

Add the following app settings:

| Key | Required | Example / notes |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Yes | `Production` |
| `PGHOST` | Yes | `<server>.postgres.database.azure.com` |
| `PGPORT` | Yes | `5432` |
| `PGDATABASE` | Yes | your Azure PostgreSQL database name |
| `PGUSER` | Yes | Azure PostgreSQL login, often `<username>@<server>` |
| `PGPASSWORD` | Yes | database password |
| `PGSSLMODE` | Yes | `Require` |
| `Frontend__AllowedOrigins__0` | Yes | `https://<your-frontend-domain>` |
| `Jwt__SigningKey` | Yes | at least 32 random characters |
| `Jwt__Issuer` | No | defaults to `CGMSportFinance.Api` |
| `Jwt__Audience` | No | defaults to `CGMSportFinance.Frontend` |
| `BootstrapAdmin__Username` | Yes | initial production admin username |
| `BootstrapAdmin__Email` | Yes | initial production admin email |
| `BootstrapAdmin__Password` | Yes | initial production admin password |
| `BootstrapAdmin__RealName` | No | defaults to `Administrator` |
| `BootstrapAdmin__HomePath` | No | defaults to `/workspace` |
| `BootstrapAdmin__Avatar` | No | optional avatar URL |
| `Seeding__EnableDemoData` | No | keep `false` in production |

Notes:

- Azure App Service app settings override `appsettings.json`.
- Hierarchical ASP.NET Core configuration keys on Linux use the `__` separator.
- The API accepts standard PostgreSQL environment variables, so `PG*` settings are the simplest production configuration.

## 3. Production seeding behavior

Production startup does the following automatically:

- applies EF Core migrations
- ensures base roles, permissions, and menus exist
- creates or updates the bootstrap admin if `BootstrapAdmin__*` settings are present

Production startup does not create the demo accounts `vben`, `admin`, and `jack` unless `Seeding__EnableDemoData=true` is explicitly set.

If the app starts in `Production` without a configured bootstrap admin and no privileged user already exists, startup fails on purpose.

## 4. Enable Health Check

The API exposes `GET /health`.

In Azure Portal:

1. Open the App Service.
2. Go to `Monitoring > Health check`.
3. Enable it and set the path to `/health`.
4. Save.

Recommended related setting:

- `HTTPS Only`: `On`

## 5. Configure GitHub deployment

This repository includes [deploy-backend.yml](/Users/hansh/Documents/Personal Assistant/Hans-OS/.github/workflows/deploy-backend.yml).

Set these GitHub repository values before the first deployment:

- Repository variable: `AZURE_WEBAPP_NAME`
- Repository variable: `AZURE_WEBAPP_HEALTHCHECK_URL`
- Repository secret: `AZURE_WEBAPP_PUBLISH_PROFILE`

To get the publish profile:

1. Open the App Service in Azure Portal.
2. Go to `Overview` or `Get publish profile`.
3. Download the publish profile file.
4. Copy its XML content into the GitHub secret `AZURE_WEBAPP_PUBLISH_PROFILE`.

Workflow behavior:

- restore
- build
- test
- publish the API
- deploy to Azure App Service
- call the exact URL stored in `AZURE_WEBAPP_HEALTHCHECK_URL`

Use the App Service default hostname for `AZURE_WEBAPP_HEALTHCHECK_URL`, for example:

- `https://<default-hostname>/health`
- `https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net/health`

## 6. First deployment checklist

Before pushing to `main`, confirm:

- Azure PostgreSQL firewall and network rules allow the App Service to connect
- all required app settings are present
- `ASPNETCORE_ENVIRONMENT=Production`
- `Seeding__EnableDemoData=false`
- GitHub secret and variables are configured

After deployment:

1. Open `https://<app-name>.azurewebsites.net/health`
2. Verify the GitHub Actions run passed
3. Sign in with the bootstrap admin account
4. Confirm no demo accounts were created in production

## 7. Operational notes

- The encrypted file `appsettings.secrets.enc.json` can still support local or fallback scenarios, but it should not be the source of truth for production secrets.
- App Service restarts the app after app setting changes.
- If health checks show redirects instead of `200`, confirm `HTTPS Only` is enabled and the health check path is `/health`.
