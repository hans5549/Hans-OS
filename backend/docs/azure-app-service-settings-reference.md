# Azure App Service 設定說明

這份文件不是操作步驟，而是說明每個設定為什麼存在、在哪裡設定、會在哪裡產生作用，以及設定錯誤時會出現什麼影響。

## 1. GitHub Actions 端設定

這些值不是給應用程式執行時讀取，而是給 GitHub Actions workflow 在 CI/CD 階段使用。

### `AZURE_WEBAPP_NAME`

- 設定位置：GitHub repository `Settings > Secrets and variables > Actions > Variables`
- 用途：告訴 `azure/webapps-deploy@v3` 要部署到哪一個 Azure App Service
- 生效位置：[deploy-backend.yml](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/.github/workflows/deploy-backend.yml)
- 作用時機：`deploy` job 執行 `Deploy to Azure App Service` step 時
- 如果設定錯誤：
  - 部署到錯的 App Service
  - 或 deploy step 直接失敗

### `AZURE_WEBAPP_HEALTHCHECK_URL`

- 設定位置：GitHub repository `Settings > Secrets and variables > Actions > Variables`
- 用途：告訴 workflow 部署完成後要打哪一個完整 URL 做健康檢查
- 生效位置：[deploy-backend.yml](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/.github/workflows/deploy-backend.yml)
- 作用時機：`Health check` step
- 如果設定錯誤：
  - 即使站台其實已經部署成功，workflow 仍可能在最後失敗
- 為什麼不能硬組：
  - 這次實際 App Service 可用網址是 `https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net`
  - 並不是簡單的 `https://hans-os-api.azurewebsites.net`

### `AZURE_WEBAPP_PUBLISH_PROFILE`

- 設定位置：GitHub repository `Settings > Secrets and variables > Actions > Secrets`
- 用途：提供 Azure App Service 部署驗證所需的 publish profile
- 生效位置：[deploy-backend.yml](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/.github/workflows/deploy-backend.yml)
- 作用時機：`Deploy to Azure App Service` step
- 如果設定錯誤：
  - deploy step 驗證失敗
  - artifact 不會被部署上去

## 2. Azure App Service 平台層設定

這些設定不一定直接進入 ASP.NET Core `Configuration`，有些是 Azure 平台本身用來控制部署與站台行為。

### `SCM Basic Auth Publishing Credentials`

- 設定位置：Azure Portal `App Service > Configuration > General settings`
- 用途：允許 App Service 的 publishing 通道接受 Basic Auth credentials
- 生效位置：Azure App Service 的部署端點
- 作用時機：GitHub Actions 使用 publish profile 部署時
- 如果關閉：
  - 可能無法成功使用 publish profile 部署
- 為什麼需要：
  - `AZURE_WEBAPP_PUBLISH_PROFILE` 最終是透過 Azure 的 publishing credentials 生效，不是應用程式本身的登入機制

### `FTP Basic Auth Publishing Credentials`

- 設定位置：Azure Portal `App Service > Configuration > General settings`
- 用途：同樣屬於 publishing credentials 的控制範圍
- 生效位置：Azure App Service 的 publishing 機制
- 作用時機：使用 publish profile 的部署流程
- 如果關閉：
  - 某些 publish-profile 型部署可能被 Azure 拒絕

### `HTTPS Only`

- 設定位置：Azure Portal 的 App Service 設定頁
- 用途：強制所有站台流量走 HTTPS
- 生效位置：Azure 平台入口
- 作用時機：HTTP 請求進站時
- 補充：
  - ASP.NET Core 內部也有 `UseHttpsRedirection()`，但平台層與應用程式層一起開比較穩定

### Health check path

- 設定位置：
  - Azure 平台若要設平台 health check，可在 App Service 介面配置 path
  - GitHub workflow 則透過 `AZURE_WEBAPP_HEALTHCHECK_URL`
- 用途：驗證站台是否活著
- 生效位置：
  - ASP.NET Core 端點在 [Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs) 的 `MapHealthChecks("/health")`
  - CI 驗證點在 [deploy-backend.yml](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/.github/workflows/deploy-backend.yml)
- 作用時機：
  - 站台啟動後
  - 部署完成後

## 3. ASP.NET Core 應用程式設定

這些值會進入 ASP.NET Core 的 configuration pipeline，主要在 [Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs) 被讀取、驗證與套用。

### `ASPNETCORE_ENVIRONMENT`

- 設定位置：Azure App Service 的 Environment variables / Configuration
- 用途：切換應用程式所處環境，常見值是 `Development`、`Staging`、`Production`
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 作用：
  - 控制 Swagger 是否開啟
  - 控制 production 的必要設定驗證
  - 控制 seed 行為
- 這次建議值：`Production`

### `PGHOST`

- 設定位置：Azure App Service Environment variables
- 用途：指定 PostgreSQL 主機名稱
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 作用時機：啟動時組裝 Npgsql connection string
- 如果有設：
  - 程式優先走 `PG*` 系列組出的連線字串
- 如果沒設：
  - 程式改讀 `ConnectionStrings:DefaultConnection`

### `PGPORT`

- 設定位置：Azure App Service Environment variables
- 用途：指定 PostgreSQL port
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 作用時機：與 `PGHOST` 一起組 connection string

### `PGDATABASE`

- 設定位置：Azure App Service Environment variables
- 用途：指定資料庫名稱
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 作用時機：與其他 `PG*` 變數一起組 connection string

### `PGUSER`

- 設定位置：Azure App Service Environment variables
- 用途：指定資料庫登入帳號
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 作用時機：與其他 `PG*` 變數一起組 connection string

### `PGPASSWORD`

- 設定位置：Azure App Service Environment variables
- 用途：指定資料庫登入密碼
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 作用時機：與其他 `PG*` 變數一起組 connection string
- 建議：
  - 視為敏感值管理，不要放進程式碼或一般文件

### `PGSSLMODE`

- 設定位置：Azure App Service Environment variables
- 用途：指定 PostgreSQL SSL 模式
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 作用時機：組 Npgsql connection string 時
- 補充：
  - 如果沒設，且 `PGHOST` 包含 `.postgres.database.azure.com`，程式會預設用 `Require`
- 建議：
  - 雖然程式有自動判斷，production 仍建議明確設成 `Require`

### `ConnectionStrings__DefaultConnection`

- 設定位置：Azure App Service Environment variables
- 用途：當你不想用 `PG*` 系列時，直接提供完整 connection string
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 作用時機：`PG*` 系列不足或未提供時
- 如果 `PG*` 與 `DefaultConnection` 都沒有：
  - production 啟動時會直接拋錯，拒絕啟動

### `Database__Provider`

- 設定位置：Azure App Service Environment variables
- 用途：指定資料庫提供者，例如 `Postgres` 或 `Sqlite`
- 生效位置：
  - [Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
  - [DatabaseSeeder.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs)
- 作用：
  - 決定註冊哪一種 `DbContext`
  - 決定 seed 時使用 `MigrateAsync()` 或 `EnsureCreatedAsync()`
- 補充：
  - [appsettings.json](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/appsettings.json) 已預設 `Postgres`
  - 只有在你要覆寫成別的 provider 時才需要額外設定

### `Frontend__AllowedOrigins__0`

- 設定位置：Azure App Service Environment variables
- 用途：指定允許跨來源請求的前端網址
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 作用時機：CORS middleware 執行時
- 如果沒設正確：
  - 瀏覽器從前端呼叫 API 時可能被 CORS 擋下
- 如果有多個前端來源：
  - 繼續設定 `Frontend__AllowedOrigins__1`、`Frontend__AllowedOrigins__2` 等等

### `Jwt__SigningKey`

- 設定位置：Azure App Service Environment variables
- 用途：作為 JWT 簽章金鑰
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 作用時機：
  - JWT token 簽發
  - JWT token 驗證
- 如果缺少或太弱：
  - 啟動驗證可能失敗
  - 或造成安全風險

### `Jwt__Issuer`

- 設定位置：Azure App Service Environment variables
- 用途：指定 JWT issuer
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 補充：
  - 有預設值 `CGMSportFinance.Api`
  - 若前後端或多環境要明確區分，建議在 production 額外設定

### `Jwt__Audience`

- 設定位置：Azure App Service Environment variables
- 用途：指定 JWT audience
- 生效位置：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 補充：
  - 有預設值 `CGMSportFinance.Frontend`
  - 若 production 前端網域或應用識別不同，建議明確設定

### `BootstrapAdmin__Username`

- 設定位置：Azure App Service Environment variables
- 用途：production 初始高權限管理者的登入帳號
- 生效位置：[DatabaseSeeder.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs)
- 作用時機：站台啟動、執行 `SeedAsync()` 時
- 如果 production 沒有任何高權限帳號，且這組設定又缺少：
  - 啟動 seed 會直接失敗

### `BootstrapAdmin__Email`

- 設定位置：Azure App Service Environment variables
- 用途：bootstrap admin 的 email
- 生效位置：[DatabaseSeeder.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs)
- 作用時機：建立或更新 bootstrap admin 時

### `BootstrapAdmin__Password`

- 設定位置：Azure App Service Environment variables
- 用途：bootstrap admin 的初始密碼
- 生效位置：[DatabaseSeeder.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs)
- 作用時機：建立或更新 bootstrap admin 時
- 建議：
  - 視為敏感值管理
  - 第一次登入後應更換

### `BootstrapAdmin__RealName`

- 設定位置：Azure App Service Environment variables
- 用途：bootstrap admin 顯示名稱
- 生效位置：[DatabaseSeeder.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs)
- 是否必填：否

### `BootstrapAdmin__HomePath`

- 設定位置：Azure App Service Environment variables
- 用途：bootstrap admin 預設首頁路徑
- 生效位置：[DatabaseSeeder.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs)
- 是否必填：否

### `BootstrapAdmin__Avatar`

- 設定位置：Azure App Service Environment variables
- 用途：bootstrap admin 頭像路徑或 URL
- 生效位置：[DatabaseSeeder.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs)
- 是否必填：否

### `Seeding__EnableDemoData`

- 設定位置：Azure App Service Environment variables
- 用途：控制是否建立 demo users
- 生效位置：[DatabaseSeeder.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs)
- 作用時機：站台啟動 seed 階段
- production 建議值：`false`
- 為什麼：
  - production 不應自動建立 `vben`、`admin`、`jack` 這類展示帳號

## 4. 設定是怎麼被程式讀進來的

ASP.NET Core 這裡的設定來源順序重點是：

1. `appsettings.json`
2. 加密設定檔 `appsettings.secrets.enc.json`（如果存在）
3. 環境變數

對 Azure App Service 來說，最常用的是環境變數覆寫。也就是說：

- 本地開發可以靠 `appsettings.json`
- Azure production 主要靠 App Service Environment variables 覆寫

這段行為定義在：

- [Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)

## 5. 啟動時有哪些地方會吃到這些設定

### 連線字串與資料庫

- `PG*` 系列或 `ConnectionStrings__DefaultConnection`
- 由 `Program.cs` 組裝與驗證
- 交給 EF Core / Npgsql 使用

### CORS

- `Frontend__AllowedOrigins__*`
- 由 `Program.cs` 註冊 CORS policy
- 作用在瀏覽器呼叫 API 時

### JWT 驗證

- `Jwt__SigningKey`、`Jwt__Issuer`、`Jwt__Audience`
- 由 `Program.cs` 綁定並驗證
- 作用在 token 簽發與 API 授權驗證

### Seed 與 bootstrap admin

- `BootstrapAdmin__*`
- `Seeding__EnableDemoData`
- 由 `DatabaseSeeder.cs` 在啟動時執行

## 6. 這次文件特別要記住的兩個實務重點

### 重點 1：health check URL 是部署驗證設定，不是應用程式設定

它存在 GitHub Actions，不存在 ASP.NET Core 的 app configuration 裡。  
也就是說：

- `AZURE_WEBAPP_HEALTHCHECK_URL` 影響 workflow 成功與否
- 它不影響 API 執行邏輯

### 重點 2：publish profile 是部署憑證，不是系統登入憑證

它只讓 GitHub Actions 能把 artifact 發佈到 App Service。  
它不會讓使用者登入你的系統，也不會變成 API 的帳密。

## 7. 建議的 production 管理原則

- 對外網址、前端網址變更時，同步更新：
  - `Frontend__AllowedOrigins__*`
  - `AZURE_WEBAPP_HEALTHCHECK_URL`
- 資料庫主機或帳密變更時，同步更新：
  - `PGHOST`
  - `PGPORT`
  - `PGDATABASE`
  - `PGUSER`
  - `PGPASSWORD`
  - `PGSSLMODE`
- 不要在 production 開 `Seeding__EnableDemoData=true`
- 定期輪替：
  - `Jwt__SigningKey`
  - `BootstrapAdmin__Password`
  - publish profile
