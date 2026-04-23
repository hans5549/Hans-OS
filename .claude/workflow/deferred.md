# Deferred Findings TODO

本檔案集中追蹤 Findings Ledger 中被標記為 `DEFERRED` 的發現，供未來 task 處理。

## 使用規則

- 每個 deferred finding 有**唯一 anchor**（`#entry-{N}`），供 Ledger 反向連結
- `Status` 欄位：`open` / `in-progress` / `done (commit {hash})` / `wontfix (reason)`
- 未來要 search 未處理項：`grep '^## entry-' deferred.md | grep -v done`
- Ledger 的 `DEFERRED` disposition 必須連結到此檔的某個 entry，否則 commit 被擋
- Close 後改 `Status` 不刪除 entry（保留歷史）

## Entry 格式範本

```markdown
## entry-{N}（YYYY-MM-DD）

- **Severity**: critical | high | medium | low
- **Source**: {Phase} / {agent-name} / ledger-{gate}-{agent}-{YYYYMMDD-HHmm}.md
- **Finding**: {file:line} — {short description}
- **Reason for deferring**: {why not fixing now — scope, complexity, dependency}
- **Target milestone**: {v1.2 | next-sprint | TBD}
- **Status**: open
- **Related**: {optional links to other entries, PRs, discussions}
```

## Pipeline hook 行為

- `pre-bash-check.mjs`（commit gate）會讀本檔驗證 DEFERRED 連結存在
- `pre-compact.mjs` 在每次 context 壓縮前顯示「N deferred findings awaiting」提醒
- `workflow status` 指令會列出當前 open entries 的數量與 severity 分佈

---

# Entries

<!-- 新 entry 加在下方，最新在最下 -->

<!-- 範例（未來實際使用時刪除此範例）：

## entry-1（2026-04-23）

- **Severity**: medium
- **Source**: Gate A / security-vuln-scanner / ledger-gateA-security-20260423-1530.md
- **Finding**: Features/Auth/RefreshTokenService.cs:88 — Potential race on rotation
- **Reason for deferring**: 需要跨 service lock 機制，超出當前 task scope
- **Target milestone**: v1.2
- **Status**: open

-->
