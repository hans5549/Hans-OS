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

呼叫 `@code-simplifier` agent（此 agent 有 edit 權限，會直接修改檔案）

## Step 2: Code Review

呼叫 `@code-review` agent

## Step 3: Security Review

呼叫 `@security-scanner` agent

## Step 4: Linus Review

呼叫 `@linus-reviewer` agent

## Step 5: gstack Review

呼叫 `@gstack-reviewer` agent

## Step 6: Build Verification

1. 執行：`dotnet build backend/HansOS.slnx`
2. 若有前端檔案修改：`cd frontend && pnpm check:type`
3. 執行：`dotnet test backend/HansOS.slnx`
4. 報告建置/測試結果

**不建立 git commit。** 使用 `commit this` 或 `@commit-workflow` 來提交。
