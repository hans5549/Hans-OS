# Azure App Service 部署文件總覽

這組文件整理了 `CGMSportFinance.Api` 部署到 Azure App Service 的實際流程、必要設定、設定用途，以及這次部署過程中真正遇到並修正的問題。

## 目前狀態

- App Service 已建立：`hans-os-api`
- 目前可用網址：[https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net](https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net)
- 健康檢查端點：[https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net/health](https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net/health)
- `GET /health` 目前回應 `Healthy`
- 成功完成 GitHub Actions 部署的 commit：`fa45e9c`

## 這次完成了什麼

### 後端程式調整

- production 啟動時會驗證必要設定，避免缺設定卻啟動成功
- 保留 `/health` 供平台與部署驗證使用
- Swagger 只在 development 開啟，production 不暴露 `/swagger`
- production 不再自動建立 demo 帳號
- production 會依 `BootstrapAdmin` 設定建立或更新 bootstrap admin

### CI/CD 調整

- 建立 GitHub Actions workflow，自動執行 restore、build、test、publish、deploy
- deploy 後會主動打 health check URL 驗證站台是否真的可用
- health check URL 改為獨立變數 `AZURE_WEBAPP_HEALTHCHECK_URL`，不再硬組網址

### 部署問題修正

- 修正 workflow 原本假設 Azure 網址為 `https://<app-name>.azurewebsites.net/health` 的問題
- 修正 `dotnet publish --no-build` 在 GitHub runner 上找不到 `CGMSportFinance.Secrets.dll` 的問題

## 文件導覽

- 操作教學：[azure-app-service-deployment-guide.md](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/docs/azure-app-service-deployment-guide.md)
- 設定說明：[azure-app-service-settings-reference.md](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/docs/azure-app-service-settings-reference.md)

## 相關檔案

- GitHub Actions workflow：[deploy-backend.yml](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/.github/workflows/deploy-backend.yml)
- API 入口與設定驗證：[Program.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Program.cs)
- 資料初始化與 production seed 行為：[DatabaseSeeder.cs](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs)
- 預設設定檔：[appsettings.json](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/src/CGMSportFinance.Api/appsettings.json)
- Solution 檔案：[CGMSportFinance.sln](/C:/Users/hansh/Documents/Personal%20Assistant/Hans-OS/backend/CGMSportFinance.sln)

## 建議閱讀順序

1. 先看操作教學，照流程完成 Azure 與 GitHub 的設定
2. 再看設定說明，理解每個值的用途、作用點與錯誤影響
3. 需要排錯時，回頭對照 workflow、`Program.cs` 與 `DatabaseSeeder.cs`
