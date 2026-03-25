---
name: commit-workflow
description: 完整提交流程（六步驟），從程式碼簡化到建置驗證再到 git commit
tools: ["read", "search", "edit", "execute", "agent"]
---

# Commit Workflow Agent

完整的提交流程，等效 Claude Code 的 `/commit-this`。

## 準備工作

1. 讀取 `.github/workflow/state.json` 取得 `modifiedFiles` 清單
2. 若無追蹤檔案，告知使用者：「沒有追蹤的檔案。請先編輯程式碼。」
3. 執行 `git diff --name-only` 確認變更檔案
4. 準備每個檔案的變更摘要（1-2 句）

## Step 1: Code Simplifier

呼叫 `@code-simplifier` agent：
- 提示：「Review and simplify the following modified files: [list files]. Focus on: primary constructors, guard clauses, expression bodies, method length (max 20 lines), nesting depth (max 3). Apply changes directly.」
- **必須**呼叫 agent，不可自行執行簡化

## Step 1.5: Spec Check

此步驟由你直接執行（不需 agent）：
1. 重新閱讀使用者原始需求或計畫
2. 比對：所有需求都已實作？沒有多餘的東西（YAGNI）？
3. 報告結果

## Step 2: Code Review

呼叫 `@code-review` agent：
- 提示：「Review these modified files: [list files]. Changes summary: [summary]. Check: code quality, architecture compliance, naming conventions, error handling, potential bugs.」
- **必須**呼叫 agent

## Step 3: Security Review

呼叫 `@security-scanner` agent：
- 提示：「Scan these modified files for security vulnerabilities: [list files]. Check: OWASP Top 10, injection, XSS, auth bypass, sensitive data exposure.」
- **必須**呼叫 agent

## Step 4: Linus Review

呼叫 `@linus-reviewer` agent：
- 提示：「Review these modified files applying Linus Torvalds criteria: [list files]. Check: Good Taste, Never Break Userspace, Pragmatism, Simplicity.」
- **必須**呼叫 agent

## Step 5: gstack Review

呼叫 `@gstack-reviewer` agent：
- 提示：「Pre-landing structural review: [list files]. Two-pass: CRITICAL (SQL safety, race conditions, trust boundary, async void) then INFORMATIONAL (magic numbers, dead code, test gaps).」
- **必須**呼叫 agent

## Step 6: Build & Commit

1. 執行：`dotnet build backend/HansOS.slnx`
2. 若有前端檔案修改：`cd frontend && pnpm check:type`
3. 執行：`dotnet test backend/HansOS.slnx`
4. 若建置/測試失敗 → 修復後重試
5. 全部通過後，建立 git commit：
   - 只 stage 修改的檔案（具體路徑，不用 `git add .`）
   - 使用 Conventional Commits 格式，繁體中文描述
   - 範例：`feat(auth): 新增 JWT refresh token 自動續期機制`

## 重要規則

- 每一步都**必須**呼叫對應的 agent，不可自行替代
- 按順序執行，每步完成後再進行下一步
- 遇到嚴重問題（CRITICAL findings）時，停下來讓使用者決定是否繼續
