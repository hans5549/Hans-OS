---
name: commit-workflow
description: 完整提交流程（含 TDD gate 與 task traceability），Combined Review → Linus → Build → Commit
tools: ["read", "search", "edit", "execute", "agent"]
---

# Commit Workflow Agent

完整的提交流程。

## 準備工作

1. 讀取 `.github/workflow/state.json` 取得 `modifiedFiles` 清單
2. 若無追蹤檔案，告知使用者：「沒有追蹤的檔案。請先編輯程式碼。」
3. 執行 `git diff --name-only` 確認變更檔案
4. 準備每個檔案的變更摘要（1-2 句）

## Step 0: Task Context & TDD Gate

此步驟由你直接執行（不需 agent）：
1. 讀取目前 `plan.md`、track plan 或工作清單，找出當前 phase 的單一 task
2. 若變更同時涵蓋多個 task，停止並要求先拆分
3. 確認行為變更有對應測試變更：feature / bug fix 必須有測試；refactor 至少要有 characterization tests
4. 若找不到測試證據，先補 RED 測試與 GREEN 實作，再進入 review pipeline

## Step 1: Combined Code Review（三個 agent **平行**派遣）

同時呼叫以下三個 agent（model: `gpt-5.4`）：
- `@code-simplifier` — 簡化程式碼（primary constructors, guard clauses, expression bodies, method length ≤ 20 lines, nesting ≤ 3）
- `@code-review` — 程式碼品質、架構合規、命名、錯誤處理、SQL safety、race conditions、trust boundary、async patterns
- `@security-scanner` — OWASP Top 10、injection、XSS、auth bypass、sensitive data exposure

三個 agent 全部完成後，標記 `codeReview` 步驟完成。

## Step 1.5: Spec / Plan Check

此步驟由你直接執行（不需 agent）：
1. 重新閱讀使用者原始需求、目前 task 與 plan
2. 比對：所有需求都已實作？沒有多餘的東西（YAGNI）？有沒有偏離原本 phase 順序？
3. 若有 deviation，要求在 plan / task 註記中說明
4. 報告結果

## Step 2: Linus Review（等 Step 1 完成後）

呼叫 `@linus-reviewer` agent（model: `gpt-5.4`）：
- 提示：「Review these modified files applying Linus Torvalds criteria: [list files]. Check: Good Taste, Never Break Userspace, Pragmatism, Simplicity.」
- **必須**呼叫 agent
- 等待 Step 1 全部完成後才派遣

## Step 3: Build & Commit

1. 先跑目前 task 相關的測試，確認 TDD 最後停在綠燈
2. 執行：`dotnet build backend/HansOS.slnx`
3. 若有前端檔案修改：`cd frontend && pnpm check:type`
4. 執行：`dotnet test backend/HansOS.slnx`
5. 若建置/測試失敗 → 修復後重試
6. 全部通過後，建立 implementation commit：
   - 只 stage 修改的檔案（具體路徑，不用 `git add .`）
   - 自動生成 commit message：使用 `git diff --staged --stat` + review 結果判斷 type 和 scope
   - 使用 Conventional Commits 格式，繁體中文描述
   - 範例：`feat(auth): 新增 JWT refresh token 自動續期機制`
   - Footer 需帶 task 參照（若有 task id / title）
   - 加入 trailer：`Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`
7. 為 implementation commit 附加 git note，摘要 task、測試、關鍵決策與變更檔案
8. 若 repo 追蹤 versioned `plan.md` / track plan：
   - 將 task 標記為完成 `[x]`
   - 記錄 implementation commit 的 short SHA
   - 將 plan 更新獨立成另一個 docs commit
9. 若當前 phase 的所有 task 都完成，提醒使用者進入合併驗證

## 重要規則

- 每一步都**必須**呼叫對應的 agent，不可自行替代
- Step 1 三個 agent **平行**派遣，Step 2 等它們都完成後才派遣
- 遇到嚴重問題（CRITICAL findings）時，停下來讓使用者決定是否繼續
- Commit 前必須先通過 TDD gate；沒有測試的行為變更不得直接提交
