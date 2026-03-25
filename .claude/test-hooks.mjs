// ============================================================================
// test-hooks.mjs — Hook 冒煙測試
// ============================================================================
// 修改 hook 後執行一次，驗證 BLOCK 行為正確。
// Run: node .claude/test-hooks.mjs
// ============================================================================

import { spawnSync, execSync } from 'child_process';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const HOOKS_DIR = resolve(__dirname, 'hooks');

let passed = 0;
let failed = 0;

function test(name, hookScript, input, expectedExitCode) {
  const hookPath = resolve(HOOKS_DIR, hookScript);
  const inputJson = JSON.stringify(input);

  const result = spawnSync('node', [hookPath], {
    input: inputJson,
    encoding: 'utf-8',
    timeout: 15000,
  });

  const actualCode = result.status;

  if (actualCode === expectedExitCode) {
    console.log(`  PASS: ${name} (exit ${actualCode})`);
    passed++;
  } else {
    console.error(`  FAIL: ${name}`);
    console.error(`    Expected exit code ${expectedExitCode}, got ${actualCode}`);
    if (result.stderr) console.error(`    stderr: ${result.stderr.substring(0, 200)}`);
    failed++;
  }
}

console.log('=== Hook Smoke Test ===\n');

// Detect current branch for conditional tests
let currentBranch = '(unknown)';
try {
  currentBranch = execSync('git branch --show-current', { encoding: 'utf-8', stdio: ['pipe', 'pipe', 'pipe'] }).trim();
} catch { /* ignore */ }
console.log(`Current branch: ${currentBranch}\n`);

// ── pre-edit-check.mjs tests ─────────────────────────────────────────────

console.log('pre-edit-check.mjs:');

if (currentBranch === 'main' || currentBranch === 'master') {
  // T1: Edit .cs on main → BLOCKED (exit 2)
  test(
    'T1: Edit .cs on main → BLOCKED',
    'pre-edit-check.mjs',
    { tool_name: 'Edit', tool_input: { file_path: 'backend/src/HansOS.Api/test.cs', old_string: 'a', new_string: 'b' } },
    2,
  );

  // T2: Write plan file on main → ALLOWED (exit 0)
  test(
    'T2: Write .claude/plans/test.md on main → ALLOWED',
    'pre-edit-check.mjs',
    { tool_name: 'Write', tool_input: { file_path: '.claude/plans/test-plan.md', content: 'test' } },
    0,
  );

  // T3: Write review file on main → ALLOWED (exit 0)
  test(
    'T3: Write .claude/reviews/test.md on main → ALLOWED',
    'pre-edit-check.mjs',
    { tool_name: 'Write', tool_input: { file_path: '.claude/reviews/test-review.md', content: 'test' } },
    0,
  );
} else {
  // Not on main — edit should be allowed
  test(
    'T1-alt: Edit .cs on non-main branch → ALLOWED',
    'pre-edit-check.mjs',
    { tool_name: 'Edit', tool_input: { file_path: 'backend/src/HansOS.Api/test.cs', old_string: 'a', new_string: 'b' } },
    0,
  );

  console.log('  SKIP: T2, T3 (main-only tests, current branch: ' + currentBranch + ')');
}

// T4: Edit protected hook file → BLOCKED (exit 2) regardless of branch
test(
  'T4: Edit .claude/hooks/test.mjs → BLOCKED (protected)',
  'pre-edit-check.mjs',
  { tool_name: 'Edit', tool_input: { file_path: '.claude/hooks/test.mjs', old_string: 'a', new_string: 'b' } },
  2,
);

// T5: Edit workflow state → BLOCKED (exit 2)
test(
  'T5: Edit .claude/workflow/state.json → BLOCKED (protected)',
  'pre-edit-check.mjs',
  { tool_name: 'Write', tool_input: { file_path: '.claude/workflow/state.json', content: '{}' } },
  2,
);

console.log('');

// ── pre-bash-check.mjs tests ─────────────────────────────────────────────

console.log('pre-bash-check.mjs:');

// T6: git commit with no tracked code files → ALLOWED (exit 0, skips workflow checks)
// Note: Commit gating only activates when workflow state tracks code files.
// Without code files, the hook warns about format but allows the commit.
test(
  'T6: git commit (no tracked code files) → ALLOWED',
  'pre-bash-check.mjs',
  { tool_name: 'Bash', tool_input: { command: 'git commit -m "test"' } },
  0,
);

// T7: Non-commit bash → ALLOWED (exit 0)
test(
  'T7: git status command → ALLOWED',
  'pre-bash-check.mjs',
  { tool_name: 'Bash', tool_input: { command: 'git status' } },
  0,
);

// T8: Bash referencing protected file → BLOCKED
test(
  'T8: Bash with .claude/hooks/ path → BLOCKED (protected)',
  'pre-bash-check.mjs',
  { tool_name: 'Bash', tool_input: { command: 'sed -i "s/a/b/" .claude/hooks/test.mjs' } },
  2,
);

console.log('');

// ── Summary ──────────────────────────────────────────────────────────────

console.log('─────────────────────────────');
console.log(`Results: ${passed} passed, ${failed} failed`);
console.log('─────────────────────────────');

if (failed > 0) {
  console.error('\nSome tests FAILED. Do NOT commit hook changes until all tests pass.');
  process.exit(1);
} else {
  console.log('\nAll tests passed. Hook changes are safe to commit.');
  process.exit(0);
}
