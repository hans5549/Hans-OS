# settings.local.json Registration Snippet

Append / merge these hook registrations into `.claude/settings.local.json`。

## 新增的 hook registration（Phase 1.75 + Phase 0）

`.claude/settings.local.json` 的 `hooks` 物件裡，確認 / 加入下列：

### PreToolUse 區段

找到 `hooks.PreToolUse`（陣列）。加入這兩條（若不存在）：

```json
{
  "matcher": "Agent",
  "hooks": [
    {
      "type": "command",
      "command": "node .claude/hooks/pre-agent-gate-check.mjs",
      "timeout": 10
    }
  ]
},
{
  "matcher": "ExitPlanMode",
  "hooks": [
    {
      "type": "command",
      "command": "node .claude/hooks/pre-exit-plan-check.mjs",
      "timeout": 10
    }
  ]
}
```

### 既有項目：無需新增

以下已存在於 settings.local.json，**無需**重複新增：

- `PreToolUse` matcher `Edit|MultiEdit|Write|mcp__filesystem__...` → pre-edit-check.mjs
- `PreToolUse` matcher `Bash` → pre-bash-check.mjs
- `PostToolUse` matcher `Agent` → post-agent-verify.mjs
- `PostToolUse` matcher `Edit|MultiEdit|Write|mcp__filesystem__...` → post-edit-build.mjs
- `SessionStart` → session-start.mjs
- `Stop` → on-stop.mjs
- `UserPromptSubmit` → workflow-orchestrator.mjs
- `PreCompact` → pre-compact.mjs

### 要移除的：無

所有既有 hook 仍需保留（內容會被新 drafts 覆寫，但 registration 不變）。

## 驗證

編輯後執行：

```bash
# 1. JSON 合法性
python3 -c "import json; json.load(open('.claude/settings.local.json'))" && echo "valid JSON"

# 2. 確認兩個新 hook 有註冊
python3 -c "
import json
s = json.load(open('.claude/settings.local.json'))
pre = s.get('hooks', {}).get('PreToolUse', [])
has_agent = any(g.get('matcher') == 'Agent' and any('pre-agent-gate-check' in h.get('command', '') for h in g.get('hooks', [])) for g in pre)
has_exit = any(g.get('matcher') == 'ExitPlanMode' and any('pre-exit-plan-check' in h.get('command', '') for h in g.get('hooks', [])) for g in pre)
print('pre-agent-gate-check registered:', has_agent)
print('pre-exit-plan-check registered:', has_exit)
"
```
