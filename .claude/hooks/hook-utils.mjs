// ============================================================================
// hook-utils.mjs - Shared utilities for Claude Code hooks
// ============================================================================
// DRY: readStdin + JSON parse + stderr logging + file helpers
// ============================================================================

import { readFileSync, existsSync } from 'fs';

/**
 * Read all data from stdin as a string.
 * @returns {Promise<string>}
 */
export function readStdin() {
  return new Promise((resolve) => {
    let data = '';
    process.stdin.setEncoding('utf-8');
    process.stdin.on('data', (chunk) => { data += chunk; });
    process.stdin.on('end', () => resolve(data));
  });
}

/**
 * Read stdin and parse as JSON. Returns parsed object or null on failure.
 * Logs parse errors to stderr (non-blocking).
 * @returns {Promise<object|null>}
 */
export async function parseHookInput() {
  try {
    const raw = await readStdin();
    return JSON.parse(raw);
  } catch (err) {
    process.stderr.write(`[Hook] Parse error: ${err.message}\n`);
    return null;
  }
}

/**
 * Write a message to stderr (visible to Claude, not blocking).
 * @param {string} msg
 */
export const log = (msg) => process.stderr.write(msg + '\n');

/**
 * Read first N lines of a file. Returns empty string if file doesn't exist.
 * @param {string} filePath
 * @param {number} n
 * @returns {string}
 */
export function readFirstNLines(filePath, n) {
  if (!existsSync(filePath)) return '';
  try {
    const content = readFileSync(filePath, 'utf-8');
    return content.split('\n').slice(0, n).join('\n');
  } catch {
    return '';
  }
}
