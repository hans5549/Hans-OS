---
name: review-workflow
description: 完整審查流程（不含 commit），僅審查不提交
tools: ["read", "search", "agent"]
---

# Review Workflow Agent

完整的審查流程，與 `@commit-workflow` 相同但**不建立 commit**。

## 準備工作

1. 讀取 `.github/workflow/state.json` 取得 `modifiedFiles` 清單
2. 若無追蹤檔案，告知使用者：「沒有追蹤的檔案。請先編輯程式碼。」
3. 執行 `git diff --name-only` 確認變更檔案
4. 準備每個檔案的變更摘要

## Step 1: Code Simplifier

呼叫 `@code-simplifier` agent（model: `gpt-5.4`，此 agent 有 edit 權限，會直接修改檔案）

## Step 2: Code Review（與 Step 3 **平行**派遣）

呼叫 `@code-review` agent（model: `gpt-5.4`）
- 含結構性審查：SQL safety、race conditions、trust boundary、shared DbContext、async patterns
- 可自動修復明顯問題（dead code、missing AsNoTracking、style fixes），使用 [ASK] 呈報需人類判斷的問題
- 與 Step 3 同時派遣

## Step 3: Security Review（與 Step 2 **平行**派遣）

呼叫 `@security-scanner` agent（model: `gpt-5.4`）
- 與 Step 2 同時派遣

## Step 4: Linus Review（等 Step 2+3 完成後）

呼叫 `@linus-reviewer` agent（model: `gpt-5.4`）
- 等待 Step 2 和 Step 3 都完成後才派遣

## Step 5: Build Verification

1. 執行：`dotnet build backend/HansOS.slnx`
2. 若有前端檔案修改：`cd frontend && pnpm check:type`
3. 執行：`dotnet test backend/HansOS.slnx`
4. 報告建置/測試結果

**不建立 git commit。** 使用 `commit this` 或 `@commit-workflow` 來提交。
