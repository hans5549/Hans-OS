# EF Core Code-First Rules

## Workflow

```bash
1. 定義 / 修改 Entity
2. 若需要，更新 DbContext / Fluent API
3. 新增 Migration:
   dotnet ef migrations add <Name> --project backend/src/HansOS.Api
4. 檢查 generated migration
5. 套用:
   dotnet ef database update --project backend/src/HansOS.Api
```

## Migration Rules

- Migration 名稱使用 **PascalCase**，而且要有描述性
- 除非必要，不手動改 `Up()` / `Down()`
- **Never delete** 已在任何環境套用過的 migration
- 一定要 review generated migration
- `Down()` 必須能正確回滾
- Migration 由 `Program.cs` 的 `MigrateAsync()` 自動在啟動時套用

## Entity Design

- 新 entity 優先使用 `Guid` 主鍵
- Data annotations 盡量少用，優先 Fluent API
- Navigation properties 與 relation configuration 盡量集中在 `OnModelCreating`
- Composite key 使用 `HasKey(...)`

## Query Rules

- Read-only query 優先 `AsNoTracking()`
- 避免 N+1，該 `Include()` 就明確 `Include()`
- 優先 `Select()` projection 取必要欄位
- 篩選要盡量下推到資料庫，不要 materialize 後才在 memory filter
- `FirstOrDefaultAsync` 優先於 `SingleOrDefaultAsync`，除非要強制唯一性

## Secrets

- 不可硬編 connection string
- 不可提交明文 secret
- local 開發使用 `appsettings.Development.json`（gitignored）
- production 使用環境變數，例如 `ConnectionStrings__DefaultConnection`
