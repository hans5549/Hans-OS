# Frontend Auto Deploy to Azure Static Web Apps

本文件整理 2026-03-10 這次前端部署 session 實際完成的設定、填入的參數、設定原因，以及每一項設定會影響到的行為。

適用範圍：

- 前端：`frontend/apps/web-antd`
- 前端部署目標：Azure Static Web Apps
- 後端部署目標：Azure App Service
- CI/CD：GitHub Actions

## 1. 這次 session 做了什麼

這次已完成以下工作：

1. 在 Azure 建立前端用的 Static Web App。
2. 建立方式改成 `Source = Other`，不讓 Azure 直接接管 GitHub workflow。
3. 從 Azure Static Web App 複製 deployment token。
4. 在 GitHub repository secrets 新增 `AZURE_STATIC_WEB_APPS_API_TOKEN`。
5. 在 GitHub repository variables 新增 `FRONTEND_API_URL`。
6. 將前端自動部署 workflow 推上 `main`：
   - `/.github/workflows/deploy-frontend.yml`
   - commit: `04bfb56`
7. GitHub Actions 成功完成前端 build 與 deploy。
8. 前端成功部署到 Azure Static Web Apps。
9. 在後端 Azure App Service 新增 CORS 允許來源：
   - `Frontend__AllowedOrigins__0`
10. 驗證前端可以開啟，後端健康檢查正常，登入流程也有真正打到後端 API 驗證。

## 2. 部署後的架構

- 前端站台：Azure Static Web Apps
- 後端 API：Azure App Service
- 自動化入口：GitHub Actions
- 觸發方式：`push` 到 `main` 且變更 `frontend/**` 或 workflow 本身

流程如下：

1. GitHub Actions 讀取 repo variable `FRONTEND_API_URL`。
2. workflow 在 build 前建立 `frontend/apps/web-antd/.env.production.local`。
3. 內容寫入 `VITE_GLOB_API_URL=${FRONTEND_API_URL}`。
4. 執行 `pnpm run build:antd`。
5. 上傳 `frontend/apps/web-antd/dist`。
6. 使用 Azure Static Web Apps deployment token 部署靜態檔案。

## 3. 這次實際設定的值

### 3.1 Azure Static Web App

- 前端正式網址：
  - `https://kind-field-097623600.1.azurestaticapps.net`
- 登入頁：
  - `https://kind-field-097623600.1.azurestaticapps.net/#/auth/login`

### 3.2 GitHub repository secret

- `AZURE_STATIC_WEB_APPS_API_TOKEN`
  - 值：已設定，但不記錄在文件內
  - 來源：Azure Static Web App 的 `Manage deployment token`

### 3.3 GitHub repository variable

- `FRONTEND_API_URL`
  - 值：`https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net/api`

### 3.4 Azure App Service application setting

- `Frontend__AllowedOrigins__0`
  - 值：`https://kind-field-097623600.1.azurestaticapps.net`

## 4. 每個設定的用途、原因與影響

### 4.1 Azure 建立 Static Web App 時選 `Source = Other`

| 項目 | 實際設定 | 設定原因 | 影響 |
| --- | --- | --- | --- |
| Azure Static Web App source | `Other` | 這個 repo 使用自訂 GitHub Actions workflow 來 build 與 deploy，並且前端在 monorepo 子目錄，還要在 build 前寫入 `.env.production.local`。如果改用 Azure 直接連 GitHub，Azure 會傾向替你再產生一份 workflow，造成部署來源重複或流程打架。 | Azure 不會接管 GitHub Actions；部署責任完全交給 repo 內的 `deploy-frontend.yml`。未來排錯時只需要看 GitHub Actions 與 Azure deployment token。 |

### 4.2 GitHub Secret: `AZURE_STATIC_WEB_APPS_API_TOKEN`

| 項目 | 實際設定 | 設定原因 | 影響 |
| --- | --- | --- | --- |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | 已設定，值未公開 | GitHub Actions 要有權限把 build 後的靜態檔上傳到 Azure Static Web Apps。這個 token 是部署憑證，不適合直接寫在 repo 或文件中。 | 沒有這個 secret，workflow 的 deploy job 會失敗；token 若失效或被重設，也會導致之後所有自動部署失敗。 |

### 4.3 GitHub Variable: `FRONTEND_API_URL`

| 項目 | 實際設定 | 設定原因 | 影響 |
| --- | --- | --- | --- |
| `FRONTEND_API_URL` | `https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net/api` | 這個前端不是在瀏覽器執行時才讀 Azure 環境變數，而是在 build 時就把 API base URL 編進產物內。因此 GitHub Actions build 前必須知道正式 API 位址。 | 前端所有 API 呼叫都會以這個值為 base URL。若後端網域改了但這裡沒更新，部署後前端會繼續打舊 API。若誤填成 `.../health` 或漏掉 `/api`，登入與業務 API 都會失敗。 |

### 4.4 Azure App Service 設定: `Frontend__AllowedOrigins__0`

| 項目 | 實際設定 | 設定原因 | 影響 |
| --- | --- | --- | --- |
| `Frontend__AllowedOrigins__0` | `https://kind-field-097623600.1.azurestaticapps.net` | 後端有啟用 CORS policy，而且設定啟動時要求至少要有一個允許來源。前端與後端現在是不同網域，所以必須把前端網域加入白名單。 | 沒有這個設定，瀏覽器會擋住跨網域 API 呼叫；前端頁面會開得起來，但登入、讀資料、送資料都會在瀏覽器端失敗。 |

## 5. GitHub Actions workflow 內的重要參數

以下是 `/.github/workflows/deploy-frontend.yml` 目前的重要設定。

| 位置 | 參數 | 實際值 | 為什麼這樣設定 | 影響 |
| --- | --- | --- | --- | --- |
| `name` | workflow 名稱 | `Deploy Frontend` | 讓 Actions 頁面能明確辨識前端部署流程。 | 之後排查前端部署時，直接看這個 workflow。 |
| `on.push.branches` | 觸發分支 | `main` | 正式環境部署只跟主線同步。 | 只有 push 到 `main` 才會自動部署 production。 |
| `on.push.paths` | 觸發路徑 | `frontend/**`, `.github/workflows/deploy-frontend.yml` | 避免後端或其他檔案變更時也重跑前端部署。 | 降低不必要的 build/deploy 次數。 |
| `workflow_dispatch` | 手動執行 | 啟用 | 保留手動重跑入口。 | 即使沒有新 commit，也能手動重新部署。 |
| `concurrency.group` | 併發群組 | `frontend-production` | 避免多個 production 前端部署同時跑。 | 新的部署會接管同一組流程，減少部署互相覆蓋的風險。 |
| `env.NODE_VERSION` | Node 版本 | `22.22.0` | 鎖定 workflow 執行環境。 | 版本漂移造成的 build 差異會變少。 |
| `env.PNPM_VERSION` | pnpm 版本 | `10.28.2` | 鎖定套件管理器版本。 | 可降低 lockfile 與 install 行為差異。 |
| `env.FRONTEND_WORKDIR` | 工作目錄 | `frontend` | repo 是 monorepo，前端不在根目錄。 | 安裝依賴與 build 都在正確子目錄執行。 |
| `env.FRONTEND_DIST_PATH` | 輸出目錄 | `frontend/apps/web-antd/dist` | 這是 `build:antd` 的實際輸出位置。 | artifact 上傳與 Azure deploy 都會讀這個目錄。 |
| build step | 產生 `.env.production.local` | `VITE_GLOB_API_URL=${FRONTEND_API_URL}` | 這個專案的 API URL 是 build-time 注入。 | 若這一步拿掉，前端會不知道正式 API 在哪裡。 |
| build command | 建置命令 | `pnpm run build:antd` | 對應目前實際前端 app。 | 產出 `web-antd` 的 production 靜態檔。 |
| deploy step | `skip_app_build` | `true` | build 已在 GitHub Actions 完成，不要讓 Azure 再 build 一次。 | 避免 Azure 端重複建置與 build 環境差異。 |
| deploy job environment | GitHub environment | `production` | 將正式部署標示為 production。 | 後續若要加保護規則、審批流程或環境變數，可以直接掛在 `production` environment。 |

## 6. 為什麼 `FRONTEND_API_URL` 一定要包含 `/api`

後端登入 API 實際路由在：

- `backend/src/CGMSportFinance.Api/Features/Auth/AuthController.cs`

控制器基底路由是：

- `[Route("api/auth")]`

因此前端如果只知道主網域，例如：

- `https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net`

那還不夠，因為前端的 API 基底設定預期就是 `/api` 這一層。這次實際填的是：

- `https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net/api`

這樣前端後續拼接 `/auth/login` 才會正確變成：

- `https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net/api/auth/login`

如果誤填：

- 漏掉 `/api`：大部分 API 會打到不存在的路徑
- 填成 `/health`：前端會把所有業務 API 都拼到 health endpoint 底下，全部失敗

## 7. 為什麼 CORS 設定要填「網域 origin」，不能填完整頁面網址

這次登入頁網址是：

- `https://kind-field-097623600.1.azurestaticapps.net/#/auth/login`

但 CORS 要填的是：

- `https://kind-field-097623600.1.azurestaticapps.net`

原因是瀏覽器 CORS 比對的是 `scheme + host + port`，不看 hash，也不看頁面路徑。這也是為什麼 `Frontend__AllowedOrigins__0` 不能填成帶有 `#/auth/login` 的完整頁面網址。

## 8. 驗證結果

這次 session 已完成以下驗證：

- GitHub Actions workflow `Deploy Frontend` 執行成功
- 推送 commit：`04bfb56`
- Actions 執行結果：`Success`
- 前端首頁可打開：
  - `https://kind-field-097623600.1.azurestaticapps.net/`
- 前端登入頁可打開：
  - `https://kind-field-097623600.1.azurestaticapps.net/#/auth/login`
- 後端健康檢查正常：
  - `https://hans-os-api-b8axc6apdaerbjda.japanwest-01.azurewebsites.net/health`
- 補上 CORS 設定後，前端已可與後端正常互動

## 9. 登入行為驗證結果

這次在登入頁觀察到一個容易誤判的行為：

- 畫面上有 `Super / Admin / User` 下拉選單
- 選到 `Admin` 之後可以登入
- 選到 `Super` 則未必可登入

這不是因為前端直接寫死權限放行，而是因為前端的下拉選單只是「幫你自動帶入預設帳密」。

前端登入頁位於：

- `frontend/apps/web-antd/src/views/_core/authentication/login.vue`

目前內建示範帳號：

- `Super` -> `vben / 123456`
- `Admin` -> `admin / 123456`
- `User` -> `jack / 123456`

真正送出登入時，前端仍然會呼叫後端 API：

- `POST /api/auth/login`

後端會在：

- `backend/src/CGMSportFinance.Api/Features/Auth/AuthController.cs`

透過以下流程驗證：

1. `FindByNameAsync`
2. `CheckPasswordAsync`
3. `GetRolesAsync`

也就是說：

- `Admin` 能登入，是因為後端真的接受該帳密
- 不是因為前端選了 `Admin` 就直接放行

### 為什麼 `Super` 可能失敗

demo 使用者是否存在，要看：

- `backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs`

在 production 環境下，只有在 `Seeding:EnableDemoData = true` 時才會保證建立 demo 帳號。這代表：

- `vben / 123456` 不一定存在
- `admin / 123456` 是否存在，也取決於資料庫初始化與正式環境帳號策略

因此目前看到「切到 `Admin` 才能登入」的結論應該理解為：

- 目前後端存在可用的 `admin` 帳號
- `Super` 對應的 demo 帳號未必存在

## 10. 後續維運時要知道的事

### 10.1 如果後端網址改了

要同步更新：

- GitHub variable `FRONTEND_API_URL`

原因：

- 前端 API URL 是 build-time 注入
- 改完 variable 後，要重新執行一次前端部署，新的網址才會寫進產物

### 10.2 如果前端網址改了

要同步更新：

- Azure App Service 的 `Frontend__AllowedOrigins__*`

原因：

- 後端 CORS 白名單是用前端網域做比對
- 若改成自訂網域或重新建立新的 Static Web App，舊值會失效

### 10.3 如果之後有第二個前端網域

可以新增：

- `Frontend__AllowedOrigins__1`
- `Frontend__AllowedOrigins__2`

原因：

- ASP.NET Core 會把 `Frontend:AllowedOrigins` 綁成清單
- 多前端網域時，需要逐一列入

### 10.4 如果部署失敗，先看哪裡

1. GitHub Actions 的 `Deploy Frontend`
2. `AZURE_STATIC_WEB_APPS_API_TOKEN` 是否仍有效
3. `FRONTEND_API_URL` 是否正確
4. Azure Static Web App 是否仍存在且可接受 deployment token

## 11. 相關檔案

- GitHub Actions workflow：
  - `/.github/workflows/deploy-frontend.yml`
- 前端登入頁：
  - `/frontend/apps/web-antd/src/views/_core/authentication/login.vue`
- 後端登入 API：
  - `/backend/src/CGMSportFinance.Api/Features/Auth/AuthController.cs`
- 後端 CORS 設定：
  - `/backend/src/CGMSportFinance.Api/Program.cs`
- 後端 demo seed：
  - `/backend/src/CGMSportFinance.Api/Infrastructure/Persistence/Seeding/DatabaseSeeder.cs`

## 12. 這份文件回答了哪些問題

這份文件可用來回答以下問題：

- 這次前端到底部署到哪裡
- GitHub 與 Azure 到底各設了哪些值
- 為什麼 `Source` 要選 `Other`
- 為什麼 `FRONTEND_API_URL` 要包含 `/api`
- 為什麼還要補 `Frontend__AllowedOrigins__0`
- 這次登入到底是不是有真的打 API 驗證
- 為什麼登入頁切到 `Admin` 才成功
