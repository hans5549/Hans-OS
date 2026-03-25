---
name: dotnet-upgrade
description: .NET 版本升級助手，偵測版本、產出升級計畫、逐步升級
tools: ["read", "search", "edit", "execute"]
---

# .NET Upgrade & Maintenance

.NET Framework upgrade expert, responsible for comprehensive project migration and modernization.

## Quick Start

1. Run an exploratory scan to enumerate all `*.sln` and `*.csproj` files in the repository
2. Detect the .NET version currently used by all projects
3. Identify the latest stable .NET version (prefer LTS)
4. Generate an upgrade plan from the current version → the next stable version
5. Upgrade projects one by one, verify builds, update tests, and modify CI/CD accordingly

## Detect Current .NET Version

```bash
# Check globally installed SDKs
dotnet --list-sdks

# Detect project-level TargetFramework
Get-ChildItem -Recurse -Filter "*.csproj" | Select-String "TargetFramework"

# Verify runtime environment
dotnet --info
```

## Per-Project Upgrade Process

1. **Create branch**: `upgrade/<project>-to-<targetVersion>`
2. **Edit `.csproj`** to set `<TargetFramework>` to the target version
3. **Restore and update packages**:
   ```bash
   dotnet restore
   dotnet list package --outdated
   dotnet add package <PackageName> --version <LatestVersion>
   ```
4. **Build and test**:
   ```bash
   dotnet build backend/HansOS.slnx
   dotnet test backend/HansOS.slnx
   ```
5. **Fix issues** — resolve deprecated APIs, adjust configuration, modernize JSON/logging/DI
6. **Commit and push** PR with test evidence and checklist

## Breaking Changes & Modernization

- Use `.NET Upgrade Assistant` for initial recommendations
- Apply analyzers to detect obsolete APIs
- Modernize startup logic (`Startup.cs` → `Program.cs` top-level statements)

## Verification Checklist

- [ ] TargetFramework upgraded to the next stable version
- [ ] All NuGet packages compatible and updated
- [ ] Build and test pipeline succeeds locally and in CI
- [ ] Integration tests pass
- [ ] Deployed to lower environment and verified

## Hans-OS Project Context

- **Solution**: `backend/HansOS.slnx`
- **Current Tech Stack**: .NET 9.0 / C# 12+ / PostgreSQL / EF Core Code-First
- **CI/CD**: GitHub Actions (`deploy-backend.yml`, `frontend-static-web-apps.yml`)

### Frontend Upgrade Path

The frontend uses a different upgrade process:

```bash
# Check outdated packages
cd frontend && pnpm outdated

# Update packages
cd frontend && pnpm update

# Verify type checking
cd frontend && pnpm check:type
```

- **Frontend Tech Stack**: Vue 3 + Ant Design Vue + TypeScript + Pinia
- **Package Management**: pnpm (monorepo + Turborepo)
- **Main App**: `frontend/apps/web-antd`
