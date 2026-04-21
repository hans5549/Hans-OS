---
name: review-workflow
description: 完整審查流程（含 TDD gate，不含 commit），Combined Review → Linus → Build
tools: ["read", "search", "agent"]
---

# Review Workflow Agent

完整的審查流程，與 `@commit-workflow` 相同但**不建立 commit**。

## 準備工作

1. 讀取 `.github/workflow/state.json` 取得 `modifiedFiles` 清單
2. 若無追蹤檔案，告知使用者：「沒有追蹤的檔案。請先編輯程式碼。」
3. 執行 `git diff --name-only` 確認變更檔案
4. 準備每個檔案的變更摘要

## Step 0: Task Context & TDD Gate

此步驟由你直接執行（不需 agent）：
1. 讀取目前 `plan.md`、track plan 或工作清單，找出當前 phase 的單一 task
2. 若變更混入多個 task，要求先拆分
3. 確認行為變更有對應測試變更：feature / bug fix 必須有測試；refactor 至少要有 characterization tests
4. 若缺少測試，先停在這裡，要求補上 RED/GREEN 後再跑完整 review

## Step 1: Combined Code Review（三個 agent **平行**派遣）

同時呼叫以下三個 agent（model: `gpt-5.4`）：
- `@code-simplifier` — 簡化程式碼（此 agent 有 edit 權限，會直接修改檔案）
- `@code-review` — 程式碼品質、結構性審查（SQL safety、race conditions、trust boundary、shared DbContext、async patterns）
- `@security-scanner` — OWASP Top 10、injection、XSS、auth bypass

三個 agent 全部完成後，標記 `codeReview` 步驟完成。

## Step 2: Linus Review（等 Step 1 完成後）

呼叫 `@linus-reviewer` agent（model: `gpt-5.4`）
- 等待 Step 1 全部完成後才派遣

## Step 3: Build Verification

1. 先跑目前 task 相關的測試，確認 review 目標維持綠燈
2. 執行：`dotnet build backend/HansOS.slnx`
3. 若有前端檔案修改：`cd frontend && pnpm check:type`
4. 執行：`dotnet test backend/HansOS.slnx`
5. 報告建置/測試結果

**不建立 git commit。** 使用 `commit this` 或 `@commit-workflow` 來提交。
