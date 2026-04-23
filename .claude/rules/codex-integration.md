# Codex 整合規則（Hans-OS）

Hans-OS 透過 OpenAI Codex plugin 整合跨模型審查。本檔彙整使用規則、故障排除與降級策略。

---

## 整合點總覽

| 位置 | Agent / Command | Phase | Trigger |
|------|----------------|-------|---------|
| Plan Phase 第 4 reviewer | `plan-codex-adversarial-reviewer`（wrapper subagent） | Plan mode 必跑 | 主 Claude 在 plan mode 單訊息內與其他 3 位 reviewer 並行 dispatch |
| Code Phase Gate X | `gatex-codex-reviewer`（wrapper subagent） | Code phase，Gate A-D 全完成後 | 主 Claude dispatch |
| Stop Hook | `stop-review-gate`（Codex plugin 原生 hook） | 每次 Stop | 自動 |
| Escape hatch | `/codex:adversarial-review`（手動 Bash） | Code phase 高風險變動選用 | 主 Claude 判斷 |

**不在其他地方使用**：Codex 只在上述位置出現。不要在 Gate A / B / C / D 額外呼叫 Codex（重複呼叫無意義且燒錢）。

---

## Wrapper Subagent 設計原則

兩個 wrapper（`plan-codex-adversarial-reviewer` 和 `gatex-codex-reviewer`）遵循相同模式：

1. **輕量 orchestration**：sonnet model，不做自己判斷
2. **忠實代理**：Codex JSON → 標準化格式，不改變結論
3. **保留 Raw Verdict**：供主 Claude 交叉驗證
4. **優雅降級**：Codex 不可用時回傳 `DEGRADED`
5. **統一 output format**：含 Machine-Readable Findings table

## Codex 無法使用時的降級策略

### 情境 1：Codex CLI 未安裝

**Wrapper 行為**：verdict = `DEGRADED`，reason = "Codex CLI not found"

**使用者行動**：
```bash
# 安裝（一次性）
npm install -g @openai/codex

# 驗證
/codex:setup
```

### 情境 2：Codex 未登入

**Wrapper 行為**：verdict = `DEGRADED`，reason = "Codex not authenticated"

**使用者行動**：
```
!codex login
```

然後重新 dispatch wrapper subagent（主 Claude 再呼叫一次）。

### 情境 3：Codex timeout 或 API 錯誤

**Wrapper 行為**：verdict = `DEGRADED`，reason = 具體錯誤訊息

**使用者行動**（擇一）：
1. 重試（暫時性問題）：主 Claude 再 dispatch 一次
2. 接受降級：`workflow override <target> <reason>`（target 為 `planCodex` 或 `gateX`，reason ≥ 20 字）
3. 等待服務恢復：暫緩當前 task

### 情境 4：OpenAI API quota exhausted

**Wrapper 行為**：verdict = `DEGRADED`，reason = "API quota exceeded"

**使用者行動**：
1. 等待 quota reset（通常月度）
2. `workflow override gateX quota exhausted, retry after billing cycle reset` 放行當前 commit
3. 檢查 OpenAI dashboard 監控每日消耗

---

## Stop-Review-Gate 使用警示

**官方警告**：此 gate 會引發 Claude ↔ Codex loop，可能快速耗用 OpenAI API quota。只建議在**主動監控的 session** 啟用。

### 啟用
```
/codex:setup --enable-review-gate
```

### 緊急關閉
```
/codex:setup --disable-review-gate
```

### 費用監控

每日 Hans-OS 開發估計 $1-5 USD 區間（標準 commit 頻率）。若超過 $10/day 有異常，檢查：

1. 是否 stop-gate 在 long-running session 產生 loop
2. 是否 Gate X findings 處理流程有 infinite rerun
3. 是否 plan-codex wrapper 被反覆重跑

**建議**：session 結束前主動 `/codex:setup --disable-review-gate`，下次 session 再啟用。

---

## Override 指令使用規則

Hans-OS 統一用 `workflow override <target> <reason>` 指令處理所有 Codex 相關的降級路徑：

| Target | 用途 | reason 範例 |
|--------|------|-----------|
| `gateX` | 跳過 Gate X Codex verdict=approve 要求 | `"Codex flagged a false positive about the logging pattern — confirmed against project convention in CLAUDE.md line 234"` |
| `planCodex` | 跳過 Plan Phase Codex reviewer | `"Codex CLI quota exhausted, monthly reset on 2026-05-01"` |
| `unblock-next` | 單次放行 pre-agent-gate-check | `"pre-agent-gate-check misread ledger-gateA as incomplete when finding #3 is actually dismissed"` |

**硬性要求**：
- `reason` 必須 ≥ 20 字（hook 會擋）
- 所有 override 寫入 `.claude/workflow/skip-log.md` 留痕
- commit message 強制含 `Override-<target>-Reason:` 欄位

**使用時機判斷**：
- ✅ 外部服務異常（Codex 掛了）
- ✅ False positive 且使用者明確評估過
- ❌ 懶得處理 findings（繞過 review）
- ❌ 想省時間（這些 gate 存在是有原因的）

---

## Rerun 語意

Gate X wrapper 支援 rerun：

```
主 Claude dispatch gatex-codex-reviewer
  → verdict=needs-attention + findings 1-3
主 Claude 處理 findings（FIXED 或 DISMISSED/DEFERRED）
主 Claude 再 dispatch gatex-codex-reviewer
  → 新 verdict（可能 approve，可能 needs-attention + 新 findings）
```

每次 rerun 都是**完整獨立的 Codex call**——成本與第一次一樣。如果 rerun > 3 次還沒 approve，建議：
1. 檢查是否 Codex 反覆命中同類型 false positive → 改用 `workflow override gateX`
2. 檢查是否 FIXED 的修正實際沒效 → 更深入修正
3. 檢查 Codex 是否不理解 Hans-OS 特定慣例 → 可能是 Hans-OS 化某個 agent 沒做完

Plan phase wrapper 不支援 rerun loop——plan 審查原則上一次過，有問題就用 override 或修 plan 重跑。

---

## 與 stop-review-gate 的互動

stop-review-gate 跟 Gate X 是**兩個獨立層級**，不要混淆：

| 層級 | 作用時機 | 自動/手動 | 擋什麼 |
|------|--------|---------|-------|
| Gate X（wrapper subagent） | Commit 前明確 dispatch | 主 Claude 手動 | Commit |
| stop-review-gate（plugin 原生 hook） | 每次 Stop 自動觸發 | 自動 | Stop |

正常流程：
1. 所有 gate 完成 → commit
2. 每次 Stop 時 stop-gate 做 quick targeted review（對 Claude 前一輪編輯）
3. 若 stop-gate 發現 issue → 擋 Stop，要求處理

**為什麼兩層都要**：Gate X 保證 commit 時 code 整體合規；stop-gate 保證過程中 Claude 沒突然寫出問題程式。防止「commit 時 OK，但 commit 後 Claude 又改壞」的情境。

---

## 故障排除 FAQ

### Q: Wrapper subagent 一直回 DEGRADED 怎麼辦？

A: 檢查順序：
1. `codex --version` 確認安裝
2. `!codex login` 重新登入
3. 檢查網路連線
4. 檢查 OpenAI API quota
5. 若全部 OK 仍 DEGRADED → 查看 wrapper 回報的具體 error message，可能是 Codex plugin 本身有 bug

### Q: Codex findings 看起來明顯錯（false positive）

A: 兩種處理：
1. **單次性錯誤** → 在 ledger 標該 finding `DISMISSED`，reason 寫清楚為什麼是 false positive
2. **系統性錯誤**（Codex 不理解某個 Hans-OS 慣例） → 考慮補強對應 agent 的 checklist / CLAUDE.md 指引，讓下次 Codex 有更多 context

### Q: Codex 費用超預期

A: 
1. 暫時關閉 stop-gate：`/codex:setup --disable-review-gate`
2. 減少 Gate X rerun 次數：快速修大 finding 再一次 rerun，別每修一個小 finding 就 rerun
3. 長 session 中途主動 `/clear` 後再開 gate，避免 hook loop 累積

### Q: Plan phase 跑 4 個 reviewer 太慢

A: 
- 3 個 Claude reviewer 是本地並行（主 Claude 可 parallel dispatch）
- Codex wrapper 是遠端 API call，較慢但仍 < 3 分鐘
- 整體 plan review 時間 ~5-8 分鐘為正常範圍
- 若覺得太慢，可用 `workflow override planCodex <reason>` 跳過 Codex，但失去跨模型視角

---

## 版本與依賴

- OpenAI Codex plugin: `openai/codex-plugin-cc`（透過 `/plugin install` 安裝）
- 本地 `codex` CLI: `@openai/codex`（npm 全域安裝）
- 認證：ChatGPT 訂閱 或 OpenAI API key
- Node.js: ≥ 18.18

此文件搭配 `.claude/agents/plan-codex-adversarial-reviewer.md` 與 `.claude/agents/gatex-codex-reviewer.md` 一起使用。
