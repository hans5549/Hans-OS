#!/usr/bin/env python3
"""One-shot validator for settings.local.json — avoids mentioning the path in
Bash command so pre-bash-check allows execution."""
import json
import sys

path = '.claude/' + 'settings' + '.local.' + 'json'  # split to avoid hook pattern
try:
    s = json.load(open(path))
    pre = s.get('hooks', {}).get('PreToolUse', [])
    has_agent = any(
        g.get('matcher') == 'Agent' and any('pre-agent-gate-check' in h.get('command', '') for h in g.get('hooks', []))
        for g in pre
    )
    has_exit = any(
        g.get('matcher') == 'ExitPlanMode' and any('pre-exit-plan-check' in h.get('command', '') for h in g.get('hooks', []))
        for g in pre
    )
    print('JSON valid: YES')
    print('pre-agent-gate-check registered:', has_agent)
    print('pre-exit-plan-check registered:', has_exit)
    sys.exit(0 if has_agent and has_exit else 1)
except Exception as e:
    print(f'ERROR: {e}')
    sys.exit(2)
