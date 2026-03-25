---
description: 'Perform janitorial tasks on C#/.NET code including cleanup, modernization, and tech debt remediation.'
name: '.NET Upgrade'
tools: ['codebase', 'edit/editFiles', 'search', 'runCommands', 'runTasks', 'runTests', 'problems', 'changes', 'usages', 'findTestFiles', 'testFailure', 'terminalLastCommand', 'terminalSelection', 'web/fetch']
---

# .NET Upgrade Collection

.NET Framework upgrade specialist for comprehensive project migration

**Tags:** dotnet, upgrade, migration, framework, modernization

## Quick Start

1. Run a discovery pass to enumerate all `*.sln` and `*.csproj` files in the repository.
2. Detect the current .NET version(s) used across projects.
3. Identify the latest available stable .NET version (LTS preferred).
4. Generate an upgrade plan to move from current → next stable version.
5. Upgrade one project at a time, validate builds, update tests, and modify CI/CD accordingly.

## Auto-Detect Current .NET Version

```bash
# Check global SDKs installed
dotnet --list-sdks

# Detect project-level TargetFrameworks
find . -name "*.csproj" -exec grep -H "<TargetFramework" {} \;

# Verify runtime environment
dotnet --info | grep "Version"
```

## Per-Project Upgrade Flow

1. **Create branch:** `upgrade/<project>-to-<targetVersion>`
2. **Edit `<TargetFramework>`** in `.csproj` to the suggested version
3. **Restore & update packages:**
   ```bash
   dotnet restore
   dotnet list package --outdated
   dotnet add package <PackageName> --version <LatestVersion>
   ```
4. **Build & test:**
   ```bash
   dotnet build backend/HansOS.slnx
   dotnet test backend/HansOS.slnx
   ```
5. **Fix issues** — resolve deprecated APIs, adjust configurations, modernize JSON/logging/DI.
6. **Commit & push** PR with test evidence and checklist.

## Breaking Changes & Modernization

- Use `.NET Upgrade Assistant` for initial recommendations.
- Apply analyzers to detect obsolete APIs.
- Modernize startup logic (`Startup.cs` → `Program.cs` top-level statements).

## Validation Checklist

- [ ] TargetFramework upgraded to next stable version
- [ ] All NuGet packages compatible and updated
- [ ] Build and test pipelines succeed locally and in CI
- [ ] Integration tests pass
- [ ] Deployed to a lower environment and verified

## Hans-OS Project Context

- **Solution**: `backend/HansOS.slnx`
- **Current Stack**: .NET 9.0 / C# 12+ / PostgreSQL / EF Core Code-First
- **CI/CD**: GitHub Actions (deploy-backend.yml, frontend-static-web-apps.yml)
- **Frontend**: Vue 3 + Ant Design Vue (separate upgrade path via pnpm)
