# Azure App Service 後端部署教學

這份文件是實際操作版教學，目標是把 `CGMSportFinance.Api` 經由 GitHub Actions 部署到 Azure App Service，並在部署完成後確認站台真的可用。

## 適用範圍

- 專案：`backend/src/CGMSportFinance.Api`
- 部署平台：Azure App Service
- CI/CD：GitHub Actions
- 資料庫：Azure Database for PostgreSQL 或其他 PostgreSQL 相容環境

## 先理解整體流程

這次部署拆成兩段：

1. GitHub Actions 在 runner 上執行 `restore -> build -> test -> publish`
2. GitHub Actions 使用 publish profile，把 publish 出來的檔案部署到 Azure App Service，然後打 `/health` 驗證

這代表部署成功不只是一個 zip 被送上去，而是包含：

- 程式可以在 CI 環境成功建置
- 測試可以通過
- Azure 接受部署
- 部署完成後站台真的可以回應

## 事前準備

### Azure 端

你需要先有：

- 一個 App Service
- 一個可連線的 PostgreSQL 資料庫
- App Service 能存取資料庫

這次實際使用的 App Service：

- 名稱：`hans-os-api`
- 預設網址：[https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net](https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net)

### GitHub 端

你需要有這個 repository 的設定權限，因為要新增：

- Repository variables
- Repository secrets

## 第 1 步：確認後端程式已具備 production 啟動條件

部署前，先確認這些設計已在程式內成立：

- production 會驗證必要設定
- `/health` 存在
- Swagger 只在 development 開啟
- production 不會自動建立任何使用者帳號
- 角色由 EF Core Migration 自動建立

這些行為主要在這裡生效：

- [Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)

## 第 2 步：建立或確認 Azure App Service

如果 App Service 已存在，可以直接跳到下一步。若是第一次做，建立時至少要確認：

- Runtime 與 .NET 版本符合專案
- 開啟 HTTPS
- 部署區域與資料庫連線位置合理

建立完成後，記下：

- App Service 名稱
- 真正可用的預設網址

這一步很重要，因為後面的 health check URL 不能只靠 App Service 名稱硬組。

## 第 3 步：設定 App Service 的應用程式設定

到 Azure Portal：

1. 打開 App Service
2. 進 `Settings > Environment variables` 或 `Settings > Configuration`
3. 新增後端所需的環境變數

至少需要確認這些值：

- `ASPNETCORE_ENVIRONMENT=Production`
- `PGHOST`
- `PGPORT`
- `PGDATABASE`
- `PGUSER`
- `PGPASSWORD`
- `Frontend__AllowedOrigins__0`
- `Jwt__SigningKey`

可選但建議明確設定：

- `PGSSLMODE=Require`
- `Jwt__Issuer`
- `Jwt__Audience`

通常不必額外設定：

- `Database__Provider`

原因是 [appsettings.json](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/appsettings.json) 已預設為 `Postgres`。

## 第 4 步：開啟 Azure 部署用 Basic Auth

如果你使用 GitHub Actions 搭配 publish profile 部署，Azure App Service 端必須允許 publishing credentials。

到 Azure Portal：

1. 打開 App Service `hans-os-api`
2. 進 `Configuration > General settings`
3. 把 `SCM Basic Auth Publishing Credentials` 設為 `On`
4. 把 `FTP Basic Auth Publishing Credentials` 設為 `On`
5. 按 `Save`

### 這一步的用意

GitHub Actions 的 `azure/webapps-deploy@v3` 在使用 publish profile 時，背後需要 Azure 接受這組 deployment credentials。這不是應用程式登入用帳密，而是部署通道的帳密。

如果這兩個值是關閉的，常見結果是：

- publish profile 下載了，但部署時驗證失敗
- GitHub Actions 的 deploy step 直接失敗

## 第 5 步：下載 publish profile

在 App Service 頁面：

1. 回到 `Overview`
2. 點 `Get publish profile`
3. 下載 `.PublishSettings` 檔
4. 用文字編輯器打開，整份 XML 都要保留

這份內容接下來會放進 GitHub secret。

## 第 6 步：設定 GitHub Actions 需要的 variables 與 secret

到 GitHub repository：

1. 點 `Settings`
2. 進 `Secrets and variables > Actions`

### Variables

新增這兩個 Repository variables：

- `AZURE_WEBAPP_NAME = hans-os-api`
- `AZURE_WEBAPP_HEALTHCHECK_URL = https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net/health`

### Secret

新增這個 Repository secret：

- `AZURE_WEBAPP_PUBLISH_PROFILE =` 整份 publish profile XML

### 為什麼 health check URL 要獨立成變數

這次實際踩到的問題就是：workflow 一開始假設 health check URL 一定是 `https://<app-name>.azurewebsites.net/health`，但 Azure 實際給的預設 hostname 是：

- `https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net`

如果 workflow 用錯網址，即使部署成功，最後 health check 還是會失敗。現在 workflow 直接讀 `AZURE_WEBAPP_HEALTHCHECK_URL`，讓部署驗證永遠以實際可用網址為準。

## 第 7 步：確認 workflow 檔案內容

部署流程定義在：

- [deploy-backend.yml](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/.github/workflows/deploy-backend.yml)

目前流程是：

1. `dotnet restore backend/CGMSportFinance.sln`
2. `dotnet build backend/CGMSportFinance.sln --configuration Release --no-restore`
3. `dotnet test backend/CGMSportFinance.sln --configuration Release --no-build`
4. `dotnet publish backend/src/CGMSportFinance.Api/CGMSportFinance.Api.csproj --configuration Release --no-build --output backend/publish`
5. 上傳 artifact
6. 下載 artifact
7. 驗證 GitHub variables / secret 是否存在
8. 部署到 Azure App Service
9. 呼叫 `AZURE_WEBAPP_HEALTHCHECK_URL`

## 第 8 步：推送到 `main` 或手動執行 workflow

這個 workflow 支援兩種啟動方式：

- push 到 `main`
- 在 GitHub Actions 頁面手動 `Run workflow`

如果你已經把程式與 workflow 推到 `main`，通常會自動觸發。

## 第 9 步：觀察 workflow 執行結果

在 GitHub 的 `Actions > Deploy Backend` 頁面，重點看兩個 job：

- `build-test-publish`
- `deploy`

### 如果 `build-test-publish` 失敗

代表 CI 端還沒過，Azure 還沒開始部署。常見原因：

- build 失敗
- test 失敗
- publish 失敗

### 如果 `deploy` 失敗

代表 build/test/publish 已經過了，但 Azure 端設定仍有問題，常見原因：

- publish profile 錯誤
- Basic Auth publishing credentials 沒開
- health check URL 錯誤

## 第 10 步：驗證部署結果

部署完成後，直接打：

- [https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net/health](https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net/health)

目前正確結果是：

- HTTP 200
- 內容為 `Healthy`

如果第一次較慢，通常是 App Service 冷啟動，重新整理一次即可。

## 第 11 步：做 production 功能驗證

health check 只代表站台活著，不代表所有功能都正常。部署完成後，建議至少驗證：

- 登入流程
- JWT 能正常簽發與驗證
- 需要資料庫的查詢能正常回應
- 角色資料已由 Migration 建立（super, admin, user）
- 可透過 API 建立第一個管理者帳號

## 這次實際踩到的兩個問題

### 問題 1：health check URL 不能硬組

現象：

- deploy 完成，但 workflow 最後仍失敗

原因：

- workflow 假設 App Service 網址是 `https://<app-name>.azurewebsites.net`
- Azure 實際提供的是帶隨機與區域後綴的 hostname

修正：

- workflow 改成讀 `AZURE_WEBAPP_HEALTHCHECK_URL`

生效位置：

- [deploy-backend.yml](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/.github/workflows/deploy-backend.yml)

### 問題 2：`dotnet publish --no-build` 找不到 `CGMSportFinance.Secrets.dll`

現象：

- GitHub Actions 在 `Publish API` step 失敗
- 錯誤訊息指出 `CGMSportFinance.Secrets.dll` 不存在

原因：

- workflow 的 publish 使用 `--no-build`
- 但 solution 當時沒有把 `CGMSportFinance.Secrets` 納進來
- CI 前面的 solution build 沒有先把那個相依專案編出來

修正：

- 把 `CGMSportFinance.Secrets` 加入 [CGMSportFinance.sln](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/CGMSportFinance.sln)

生效位置：

- solution build 與後續 publish 的相依輸出

## 部署後的維運建議

- 在 App Service 啟用 log stream，方便看啟動錯誤
- 將 production 的 JWT signing key 保持為長且隨機的祕密值
- 部署後需手動建立第一個管理者帳號
- 若之後要更換自訂網域，記得同步更新 `AZURE_WEBAPP_HEALTHCHECK_URL`
- 若前端網址變更，記得同步更新 `Frontend__AllowedOrigins`
