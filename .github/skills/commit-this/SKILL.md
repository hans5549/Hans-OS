---
name: commit-this
description: '輕量提交流程：整理異動檔案、自動生成 Conventional Commits 訊息並 commit。Use when: commit, 提交, commit this, 快速提交。跳過 review pipeline，適合快速迭代。'
argument-hint: '可選：commit 描述或 scope 提示'
---

# Commit This — 輕量提交流程

跳過完整 review pipeline，快速整理異動並提交。適用於小修改、文件更新、或已完成 review 的程式碼。

## 適用時機

- 小幅度修改（< 50 行）已確認正確
- 純文件/設定變更
- 已通過 review pipeline 但尚未 commit
- 快速迭代中需要頻繁 checkpoint

> **大型變更或未 review 的程式碼**，請使用 `@commit-workflow` agent 走完整流程。

## 流程

### Step 1: 收集異動

1. 執行 `git status --short` 取得所有異動檔案
2. 執行 `git diff --stat` 取得變更統計
3. 若有 untracked 新檔案，列出並確認是否要一併提交
4. 若沒有任何異動，告知使用者「沒有需要提交的變更」並結束

### Step 2: 分類異動檔案

將異動檔案依類型分類，用於判斷 commit type 和 scope：

| 分類 | 路徑模式 | 說明 |
|------|----------|------|
| Backend | `backend/src/**/*.cs`, `*.csproj` | C# 程式碼 |
| Frontend | `frontend/**/*.{vue,ts,tsx,css}` | Vue/TS 前端 |
| Migration | `backend/**/Migrations/**` | EF Core 遷移 |
| Test | `backend/tests/**`, `**/*.test.*` | 測試檔案 |
| Config | `*.json`, `*.yml`, `*.yaml`, `*.mts` | 設定檔 |
| Doc | `*.md`, `*.txt`, `*.rst` | 文件 |

### Step 3: 自動判斷 Commit Type

根據變更內容自動推斷 type：

```
新增檔案為主 → feat
修改既有邏輯 → fix 或 refactor（看 diff 內容）
純刪除/清理 → refactor
只有測試 → test
只有文件 → docs
只有設定 → chore
Migration 相關 → feat 或 fix（依上下文）
```

決策依據：
- `git diff --staged` 的內容分析
- 新增 vs 修改 vs 刪除的比例
- 是否有新增 public API / endpoint

### Step 4: 決定 Scope

Scope 規則：
- 若所有異動集中在單一 feature 資料夾 → 該資料夾名稱（如 `auth`, `finance`, `menu`）
- 若跨多個 feature 但有明確主題 → 主題名稱
- 若異動分散且無明確主題 → 省略 scope
- 前端專屬變更 → 加 `ui` 前綴（如 `ui-finance`）

### Step 5: 生成 Commit Message

格式：**Conventional Commits 1.0.0**，描述使用**繁體中文**。

```
<type>(<scope>): <簡短描述>

<正文：列出主要變更項目（選填，多檔案時建議加）>
```

規則：
- 標題行 ≤ 72 字元
- 簡短描述以動詞開頭（新增、修正、重構、移除、更新）
- 正文用 `- ` bullet list 列出主要變更
- 若使用者提供了描述提示，優先採用

### Step 6: 確認與提交

1. 向使用者展示：
   - 將要 stage 的檔案清單
   - 生成的 commit message
2. 等待使用者確認（或修改 message）
3. 執行：
   ```bash
   git add <具體檔案路徑>   # 逐一 stage，禁止 git add .
   git commit -m "<message>"
   ```
4. 顯示 commit 結果（short SHA + 統計）

## 重要規則

- **禁止** `git add .` 或 `git add -A`（專案 hook 會攔截）
- **禁止** 在 `main`/`master` 分支上 commit 程式碼變更
- Stage 必須使用具體檔案路徑
- Commit message 必須是繁體中文描述
- 若異動包含敏感檔案（`appsettings.*.json`, `.env`），警告使用者並跳過該檔案
