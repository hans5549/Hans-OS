---
name: commit-workflow
description: 完整提交流程（五步驟），從程式碼簡化到建置驗證再到 git commit
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

呼叫 `@code-simplifier` agent（model: `gpt-5.4`）：
- 提示：「Review and simplify the following modified files: [list files]. Focus on: primary constructors, guard clauses, expression bodies, method length (max 20 lines), nesting depth (max 3). Apply changes directly.」
- **必須**呼叫 agent，不可自行執行簡化

## Step 1.5: Spec Check

此步驟由你直接執行（不需 agent）：
1. 重新閱讀使用者原始需求或計畫
2. 比對：所有需求都已實作？沒有多餘的東西（YAGNI）？
3. 報告結果

## Step 2: Code Review（與 Step 3 **平行**派遣）

呼叫 `@code-review` agent（model: `gpt-5.4`）：
- 提示：「Review these modified files: [list files]. Changes summary: [summary]. Check: code quality, architecture compliance, naming conventions, error handling, potential bugs. ALSO check structural issues: SQL safety, race conditions, trust boundary, shared DbContext, async anti-patterns. Auto-fix obvious mechanical issues (dead code, missing AsNoTracking, style fixes). Use [ASK] for anything requiring human judgment. Use [ARCH] for architecture-level issues.」
- **必須**呼叫 agent
- 與 Step 3 同時派遣（一條訊息，兩個 agent 呼叫）

## Step 3: Security Review（與 Step 2 **平行**派遣）

呼叫 `@security-scanner` agent（model: `gpt-5.4`）：
- 提示：「Scan these modified files for security vulnerabilities: [list files]. Check: OWASP Top 10, injection, XSS, auth bypass, sensitive data exposure.」
- **必須**呼叫 agent
- 與 Step 2 同時派遣

## Step 4: Linus Review（等 Step 2+3 完成後）

呼叫 `@linus-reviewer` agent（model: `gpt-5.4`）：
- 提示：「Review these modified files applying Linus Torvalds criteria: [list files]. Check: Good Taste, Never Break Userspace, Pragmatism, Simplicity.」
- **必須**呼叫 agent
- 等待 Step 2 和 Step 3 都完成後才派遣

## Step 5: Build & Commit

1. 執行：`dotnet build backend/HansOS.slnx`
2. 若有前端檔案修改：`cd frontend && pnpm check:type`
3. 執行：`dotnet test backend/HansOS.slnx`
4. 若建置/測試失敗 → 修復後重試
5. 全部通過後，建立 git commit：
   - 只 stage 修改的檔案（具體路徑，不用 `git add .`）
   - 自動生成 commit message：使用 `git diff --staged --stat` + review 結果判斷 type 和 scope
   - 使用 Conventional Commits 格式，繁體中文描述
   - 範例：`feat(auth): 新增 JWT refresh token 自動續期機制`
   - 加入 trailer：`Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`
6. Commit 成功後，建議下一步：
   - `git push` 推送到遠端
   - `merge this` 合併回 main 並清理 worktree

## 重要規則

- 每一步都**必須**呼叫對應的 agent，不可自行替代
- Step 2 + Step 3 **平行**派遣，Step 4 等它們都完成後才派遣
- 遇到嚴重問題（CRITICAL findings）時，停下來讓使用者決定是否繼續
