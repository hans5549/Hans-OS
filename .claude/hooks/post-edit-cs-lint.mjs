// ============================================================================
// post-edit-cs-lint.mjs - PostToolUse Hook: C# Style Instant Feedback
// ============================================================================
// After .cs file edits, provides style suggestions via stderr (non-blocking).
// Checks: == null, "", missing Async suffix, namespace { } syntax
// ============================================================================

import { readFileSync } from 'fs';
import { extname } from 'path';
import { parseHookInput, log } from './hook-utils.mjs';

// ── Main ───────────────────────────────────────────────────────────────────

const parsed = await parseHookInput();
if (!parsed) process.exit(0);

const input = parsed.tool_input || {};
const filePath = input.file_path || input.path || '';

if (!filePath || extname(filePath).toLowerCase() !== '.cs') {
  process.exit(0);
}

// ── Read edited file content ───────────────────────────────────────────────

let content;
try {
  content = readFileSync(filePath, 'utf-8');
} catch {
  process.exit(0);
}

const lines = content.split('\n');
const hints = [];

for (let i = 0; i < lines.length; i++) {
  const line = lines[i];
  const lineNum = i + 1;

  // == null / != null (but not "is null" / "is not null")
  if (/[^!<>=]=\s*null\b/.test(line) && !/is\s+(not\s+)?null/.test(line)) {
    hints.push(`line ~${lineNum}: prefer 'is null' over '== null'`);
  }
  if (/!=\s*null\b/.test(line) && !/is\s+not\s+null/.test(line)) {
    hints.push(`line ~${lineNum}: prefer 'is not null' over '!= null'`);
  }

  // "" instead of string.Empty (skip attributes, string comparisons with "")
  if (/""/.test(line) && !/string\.Empty/.test(line) && !/\[.*"".*\]/.test(line) && !/\/\//.test(line.substring(0, line.indexOf('""')))) {
    // Only flag assignment-like patterns: = "", return "", ??  ""
    if (/(?:=|return|=>|\?\?)\s*""/.test(line)) {
      hints.push(`line ~${lineNum}: prefer 'string.Empty' over '""'`);
    }
  }

  // async method missing Async suffix
  if (/\basync\s+\S+\s+(\w+)\s*[<(]/.test(line)) {
    const match = line.match(/\basync\s+\S+\s+(\w+)\s*[<(]/);
    if (match && !match[1].endsWith('Async') && !match[1].startsWith('On') && match[1] !== 'Main') {
      hints.push(`line ~${lineNum}: async method '${match[1]}' should have 'Async' suffix`);
    }
  }

  // namespace X { } instead of namespace X;
  if (/^\s*namespace\s+\S+\s*\{/.test(line)) {
    hints.push(`line ~${lineNum}: prefer file-scoped namespace (namespace X;)`);
  }
}

// ── Output hints (max 8 to avoid noise) ────────────────────────────────────

if (hints.length > 0) {
  const fileName = filePath.split(/[/\\]/).pop();
  log('');
  for (const hint of hints.slice(0, 8)) {
    log(`[Style] ${fileName}: ${hint}`);
  }
  if (hints.length > 8) {
    log(`[Style] ... and ${hints.length - 8} more suggestions`);
  }
  log('');
}

process.exit(0);
