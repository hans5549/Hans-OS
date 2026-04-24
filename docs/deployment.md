# Hans-OS 部署文件

## 架構概覽

```
GitHub (main branch)
    │
    ├─ backend/** push → Deploy Backend Workflow
    │       └─ OIDC 認證 → Azure App Service (hans-os-api)
    │                    └─ PostgreSQL (hans-postgresql-server)
    │
    └─ frontend/** push → Frontend Static Web Apps Workflow
            └─ Static Web App Token → Azure Static Web Apps (hans-os-frontend)
```

---

## Azure 資源清單

| 資源類型 | 名稱 | 位置 | 方案 |
|----------|------|------|------|
| Resource Group | `hans-azure-resoure` | — | — |
| App Service Plan | `ASP-hansazureresoure-a6e9` | Japan West | Linux B1 |
| App Service | `hans-os-api` | Japan West | .NET 9 |
| Static Web App | `hans-os-frontend` | East Asia | Free |
| PostgreSQL Flexible Server | `hans-postgresql-server` | — | v18 |
| Entra App Registration | `hans-os-github-actions` | — | OIDC |

**App Service URL**：`https://hans-os-api.azurewebsites.net`

**Frontend URL**：`https://green-coast-01b10a200.1.azurestaticapps.net`

---

## GitHub Actions Workflows

### 1. Backend Deploy (`.github/workflows/deploy-backend.yml`)

**觸發條件**：
- `push` 到 `main` 且路徑包含 `backend/**`
- `workflow_dispatch`（手動觸發）

**流程**：
```
build-and-test job:
  checkout → setup .NET 9 → restore → build → test → publish → zip → upload

deploy job (needs: build-and-test, main branch only):
  1. OIDC 登入 Azure（無 client secret）
  2. 設定 6 個 App Settings（從 GitHub Secrets 推送）
  3. az webapp deploy --type zip
```

**認證方式**：OIDC Federated Credentials（無 client secret，不會過期）

### 2. Frontend Deploy (`.github/workflows/frontend-static-web-apps.yml`)

**觸發條件**：
- `push` 到 `main` 且路徑包含 `frontend/**`
- `workflow_dispatch`

**流程**：checkout → pnpm install → pnpm build:antd → Azure Static Web Apps 部署

---

## GitHub Secrets 清單

| Secret | 說明 | 管理方式 |
|--------|------|----------|
| `AZURE_CLIENT_ID` | OIDC SP App ID | `gh secret set` |
| `AZURE_TENANT_ID` | Azure AD Tenant ID | `gh secret set` |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID | `gh secret set` |
| `JWT_SIGNING_KEY` | Production JWT 簽名金鑰（≥32字元隨機） | `gh secret set` |
| `DB_CONNECTION_STRING` | PostgreSQL Npgsql connection string | `gh secret set` |
| `IDENTITY_SEED_ADMIN_PASSWORD` | Admin 初始帳號密碼 | `gh secret set` |
| `VITE_GLOB_API_URL` | 前端呼叫的 API URL | `gh secret set` |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | 前端部署 Token（Azure 自動產生） | Azure Portal |

**已刪除（不再使用）**：
- ~~`AZURE_CLIENT_SECRET`~~ — 改用 OIDC
- ~~`AZURE_WEBAPP_PUBLISH_PROFILE`~~ — 改用 `az webapp deploy`

---

## Azure App Settings（由 Workflow 自動管理）

每次 Backend 部署時，workflow 會自動同步以下 6 個設定：

| App Setting | 來源 |
|-------------|------|
| `Jwt__SigningKey` | GitHub Secret `JWT_SIGNING_KEY` |
| `ConnectionStrings__DefaultConnection` | GitHub Secret `DB_CONNECTION_STRING` |
| `IdentitySeed__AdminPassword` | GitHub Secret `IDENTITY_SEED_ADMIN_PASSWORD` |
| `Frontend__AllowedOrigins__0` | 硬編碼於 workflow（前端 URL） |
| `ASPNETCORE_ENVIRONMENT` | `Production`（硬編碼） |
| `WEBSITE_RUN_FROM_PACKAGE` | `1`（硬編碼） |

> ⚠️ **不要在 Azure Portal 手動修改這些設定**，下次部署會被 workflow 覆蓋。

---

## OIDC Service Principal 資訊

- **顯示名稱**：`hans-os-github-actions`
- **App ID**：`f8e18e01-f3e7-4519-af82-349f8f36399e`
- **角色**：Contributor on `hans-azure-resoure` resource group
- **Federated Credentials**：
  - `github-actions-main`：`repo:hans5549/Hans-OS:ref:refs/heads/main`
  - `github-actions-dispatch`：`repo:hans5549/Hans-OS:workflow_dispatch`

---

## 重建或初始化 SOP

### 重建 App Service（完整步驟）

```bash
# 1. 設定變數
RG="hans-azure-resoure"
PLAN="ASP-hansazureresoure-a6e9"
APP="hans-os-api"

# 2. 刪除舊資源
az webapp delete --name $APP --resource-group $RG
az appservice plan delete --name $PLAN --resource-group $RG --yes

# 3. 重建
az appservice plan create --name $PLAN --resource-group $RG \
  --location japanwest --sku B1 --is-linux

az webapp create --name $APP --resource-group $RG \
  --plan $PLAN --runtime "DOTNETCORE:9.0"

az webapp update --name $APP --resource-group $RG --https-only true
az webapp config set --name $APP --resource-group $RG \
  --http20-enabled true --ftps-state Disabled --min-tls-version 1.2

# 4. 觸發部署（App Settings 由 workflow 自動設定）
gh workflow run deploy-backend.yml --repo hans5549/Hans-OS --ref main
```

### 重建 OIDC Service Principal

```bash
# 1. 刪除舊的
az role assignment delete --assignee <APP_ID> --role Contributor \
  --scope /subscriptions/a38dbc16-0e38-47ab-b988-d917c93ee968/resourceGroups/hans-azure-resoure
az ad app delete --id <APP_OBJ_ID>

# 2. 建立新的
APP_ID=$(az ad app create --display-name "hans-os-github-actions" --query appId -o tsv)
az ad sp create --id "$APP_ID"
APP_OBJ_ID=$(az ad app show --id "$APP_ID" --query id -o tsv)

# 3. 設定 Federated Credentials
az ad app federated-credential create --id "$APP_OBJ_ID" --parameters '{
  "name": "github-actions-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:hans5549/Hans-OS:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

az ad app federated-credential create --id "$APP_OBJ_ID" --parameters '{
  "name": "github-actions-dispatch",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:hans5549/Hans-OS:workflow_dispatch",
  "audiences": ["api://AzureADTokenExchange"]
}'

# 4. 指派角色
az role assignment create --assignee "$APP_ID" --role Contributor \
  --scope /subscriptions/a38dbc16-0e38-47ab-b988-d917c93ee968/resourceGroups/hans-azure-resoure

# 5. 更新 GitHub Secret
echo "$APP_ID" | gh secret set AZURE_CLIENT_ID --repo hans5549/Hans-OS
```

### 更新 GitHub Secrets

```bash
# 更新某個 Secret
echo "new-value" | gh secret set SECRET_NAME --repo hans5549/Hans-OS

# 列出所有 Secrets
gh secret list --repo hans5549/Hans-OS

# 刪除 Secret
gh secret delete SECRET_NAME --repo hans5549/Hans-OS
```

---

## 常見問題

### Backend 部署後 API 無回應

1. 查看 Log Stream：
   ```bash
   az webapp log tail --name hans-os-api --resource-group hans-azure-resoure
   ```
2. 確認 App Settings 已正確設定：
   ```bash
   az webapp config appsettings list --name hans-os-api --resource-group hans-azure-resoure -o table
   ```

### OIDC 認證失敗（`Error: AADSTS70021`）

Federated Credential 的 subject 必須完全符合 GitHub Actions 的 token claims。
確認 `AZURE_CLIENT_ID` Secret 是最新 OIDC SP 的 App ID。

### 前端 API 呼叫失敗（CORS 錯誤）

確認 `Frontend__AllowedOrigins__0` App Setting 等於前端實際 URL：
```bash
az webapp config appsettings set --name hans-os-api --resource-group hans-azure-resoure \
  --settings "Frontend__AllowedOrigins__0=https://green-coast-01b10a200.1.azurestaticapps.net"
```

---

## 重要 IDs（參考）

| 項目 | 值 |
|------|-----|
| Subscription ID | `a38dbc16-0e38-47ab-b988-d917c93ee968` |
| Tenant ID | `686899fd-c428-4184-b497-0efdc5139892` |
| OIDC SP App ID | `f8e18e01-f3e7-4519-af82-349f8f36399e` |
| PostgreSQL FQDN | `hans-postgresql-server.postgres.database.azure.com` |
| Frontend URL | `https://green-coast-01b10a200.1.azurestaticapps.net` |
| Backend URL | `https://hans-os-api.azurewebsites.net` |
