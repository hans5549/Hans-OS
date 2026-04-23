# Hans-OS Project Fit Review Checklist

這是 `code-review-specialist` agent 在 **Gate B（Project Fit）** 審查時的專案特定檢查清單。Agent 本身保持精簡的 persona 與 process，具體檢查項目由本檔集中管理。

---

## 1. 架構合規（讀 `.claude/ARCHITECTURE.md` 對照）

### Features-based Folder Structure
- 新增功能應落在 `backend/src/HansOS.Api/Features/{FeatureName}/` 下
- 每個 Feature 內含 Controllers / Services / Repositories / Models 子目錄
- 跨 Feature 共用的抽象才放到 `Shared/` 或 `Infrastructure/`
- **Flag**: 傳統 MVC 分層（根目錄 `Controllers/`、`Services/`）→ 違反 Features-based

### API Envelope Pattern
- 所有 API 回應**必須**包在 `ApiEnvelope<T>` 結構內
  - 成功：`ApiEnvelope<T>.Success(data)` → `{ code: 0, data: T, message: "" }`
  - 失敗：`ApiEnvelope.Failure(code, message)` → `{ code: !=0, data: null, message }`
- **Flag**:
  - `return Ok(data)` 直接回裸物件
  - 匿名型別 `return new { success: true, ... }`
  - 自訂 response wrapper（應統一用 `ApiEnvelope<T>`）

### DI Lifetime 規則
- `ApplicationDbContext` → `Scoped`
- Business services → `Scoped`（Web API 的預設，request-scoped 就夠）
- Stateless helpers → `Transient`
- Cross-request caches / singletons → `Singleton`（需謹慎評估 thread safety）
- **Flag**:
  - `AddSingleton<DbContext>` → 連線洩漏
  - `AddTransient<IApplicationDbContextFactory>` 然後每個呼叫都 new → 不必要的 overhead

### 三層調用規則
- Controller → Service → Repository
- Controller **不**直接 inject DbContext
- Service **不**繞過 Repository 直接 EF 查詢（除非明確的 domain 理由）
- Repository **不**回傳 EF tracked entity 給 Controller
- **Flag**: 任一層級跳過

---

## 2. 認證授權（JWT + Refresh Token）

### JWT Access Token
- 生命期 5-15 分鐘（不要過長）
- 儲存位置：前端 memory（Pinia store），**絕對不**放 localStorage
- Audience + Issuer 驗證啟用
- 簽章金鑰來自環境變數，非硬編碼

### Refresh Token Cookie
- `HttpOnly=true`、`Secure=true`、`SameSite=Strict`（或 `Lax` 若明確跨站）
- Path 限制在 `/auth/refresh`
- Rotation：每次 refresh 都產新 token + 無效化舊的
- Revoke：logout 必須無效化 server 端 refresh token

### 授權檢查位置
- `[Authorize]` 或 `[AllowAnonymous]` **每個** controller action 必須明確標記（不允許 implicit default）
- 具體權限檢查（permission code / role）**在 Service 層**做，不只 Controller 層
- UI 層的隱藏（`v-if="can.read"`）是**附加保護**，不是主要機制
- **Flag**:
  - Controller 層以外看不到授權檢查
  - 只依賴 `[Authorize]` 做粗粒度授權，未檢查具體 permission code
  - UI 層是唯一授權檢查（伺服器端應該也擋）

---

## 3. EF Core Code-First

### Migration 規則
- 使用 Code-First：從 Entity + `OnModelCreating` 產生 migration，不從 DB schema 反推
- Migration 必須**additive**（加欄位 OK，刪欄位需數據遷移策略）
- `MigrateAsync()` 在 `Program.cs` 啟動時自動執行
- Migration PascalCase 命名：`AddUserRefreshTokenTable`、`AlterMenuAddOrderIndex`
- **Flag**:
  - Migration 內有 `DropColumn` 沒有對應的數據 backfill
  - 任何 `dotnet ef database` 以外的手動 SQL 管理
  - 引用 Database-First 工具（如 `Scaffold-DbContext`）

### Query 規則
- Read-only query 必須 `.AsNoTracking()`
- List query 必須 `.Take(pageSize)` + `.Skip()` pagination（除非明確的小結果集，如 menu tree）
- N+1 嫌疑：`foreach` 裡面有 `await ... ToListAsync()` → flag
- **Flag**:
  - 返回 `List<TrackedEntity>` 給 Controller
  - 缺 `AsNoTracking()` 的 list query
  - 缺 pagination 的 unbounded query

---

## 4. Menu Tree 與 Permission Code

### Menu Tree 結構
- Hans-OS 的 UI 由 Menu tree 驅動（前端 router + permission 都讀它）
- `GET /menu/tree` 回自己有權限的部分
- `GET /auth/codes` 回當前使用者所有 permission codes
- **Flag**:
  - Menu 查詢未過濾 permission → 潛在權限洩漏
  - 前端硬編碼 route 未對應 menu 設定

### Permission Code 使用
- Permission codes 以字串常數定義於 `MenuPermission` 或類似集中處
- 不散落在 Service code 裡的字面量
- **Flag**:
  - Business logic 裡有 `if (userCodes.Contains("menu.edit"))` 硬編碼字串

---

## 5. Vue 3 Frontend

### Composition API 慣例
- `<script setup lang="ts">` 必備
- 避免 Options API（`export default defineComponent(...)` ）
- Composables 放 `frontend/src/composables/useXxx.ts`
- 重複的 3+ 次 ref/watch/onMounted 模式 → 應抽 composable

### Pinia Store
- 使用 setup-style store（`defineStore('id', () => { ... })`）
- Store 負責非 UI state（認證、使用者、menu、全域設定）
- **不要**把 component-local state 塞到 store
- **Flag**:
  - Options-style store（`defineStore('id', { state, actions, getters })`）與 setup-style 混用

### TypeScript Strict
- `tsconfig` 必須 `"strict": true`
- 避免 `any`（`unknown` + type guard 是正確做法）
- Props / emits 都要型別宣告：`defineProps<{ ... }>()` / `defineEmits<{ ... }>()`
- **Flag**:
  - `any` 出現（除非明確標註 ESLint disable + 理由）
  - `// @ts-ignore` 沒有伴隨理由

### Ant Design Vue 使用慣例
- 優先使用 Ant Design Vue 元件，避免重新造輪子
- 表單用 `<a-form>` + `<a-form-item>`，驗證用 `rules` prop
- Dialog / Modal 用 `<a-modal>` 或 `Modal.confirm()`
- **Flag**:
  - 自製 `<div class="dialog">` 而非用 `<a-modal>`

### API Client Pattern
- 前端呼叫 API 時，型別與後端 `ApiEnvelope<T>` 對齊
- 統一的 axios instance 處理 error envelope（`code !== 0` 轉成 throw）
- **Flag**:
  - 直接在 component 裡 `axios.get(...)` 不走統一 client
  - 前端 response 型別用 `any` 或 `unknown` 未 narrow

---

## 6. Spec Compliance

### Plan / Spec 對照
- 若 `.claude/plans/*.md` 存在當前 task 的 plan：
  - 逐項驗證 plan 宣告的變更是否完整實作
  - 檢查是否有 plan 沒提但被實作的功能（scope creep / YAGNI 違反）
  - 任何 plan 偏離都 **flag 給使用者決定**，不要默默接受

### Deviation 處理
- Plan 與 implementation 的偏離不一定是錯——但必須**明確記錄理由**
- Review 報告中列出所有 deviation 並評估合理性

---

## 7. Code Style（簡版，語法類交給 simplifier）

### Naming
- Classes / Properties / Methods：PascalCase
- Private fields：`_camelCase`
- Parameters / local vars：camelCase
- `*Async` suffix 在 async 方法

### Namespace
- File-scoped namespace（`namespace X;`）
- Nullable reference types 啟用
- Implicit usings（.NET 9 default）

### XML Doc
- Public API 的 XML doc 用**繁體中文**（專案明文要求）

### Structured Logging
- `_logger.LogInformation("User {UserId} logged in", userId)` — placeholder
- **Flag**: `_logger.LogInformation($"User {userId} logged in")` — 字串插值禁止

---

## 不在本 checklist 範圍的事項

以下由**其他 agent** 負責，specialist **不**重複檢查：

- OWASP Top 10 細節 → `security-vuln-scanner`（Gate A）
- 現代 C# 12 語法機會（Primary Constructor / Record / Expression Body / Collection Expression） → `code-simplifier`（Gate D）
- Vue/TS 簡化機會（`ref` vs `reactive` / 型別推斷 / `as` → type guard） → `code-simplifier`（Gate D）
- 方法長度 / 嵌套深度 / 品味判斷 → `linus-reviewer`（Gate C）
- 跨模型第二意見 → `gatex-codex-reviewer`（Gate X）

specialist 的 mission 是 **Project Fit**——專案特定的架構、慣例、spec 合規。不重複其他 gate 的工作。
