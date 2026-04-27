import type {
  TodoDifficulty,
  TodoPriority,
  TodoStatus,
} from '#/api/core/todos';

/**
 * Todo 模組的 enum → 顯示資訊對應表（label / 色彩 / Lucide icon path）。
 *
 * 集中於此避免每個元件各自定義；icon 一律使用 Lucide SVG path
 * （24x24 viewBox，stroke="currentColor"），符合 SKILL.md 禁用 emoji 規範。
 */

export interface MetaOption<TValue extends string> {
  value: TValue;
  label: string;
  color: string;
  /** Lucide SVG path（適用 24x24 viewBox） */
  iconPath?: string;
}

// ─── Priority ──────────────────────────────────────

export const priorityOptions: MetaOption<TodoPriority>[] = [
  { value: 'Urgent', label: 'P0 緊急', color: 'var(--todo-priority-p0)' },
  { value: 'High', label: 'P1 高', color: 'var(--todo-priority-p1)' },
  { value: 'Medium', label: 'P2 中', color: 'var(--todo-priority-p2)' },
  { value: 'Low', label: 'P3 低', color: 'var(--todo-priority-p3)' },
  { value: 'None', label: 'P4 無', color: 'var(--todo-priority-p4)' },
];

const priorityShortLabels: Record<TodoPriority, string> = {
  Urgent: 'P0',
  High: 'P1',
  Medium: 'P2',
  Low: 'P3',
  None: 'P4',
};

const priorityColors: Record<TodoPriority, string> = {
  Urgent: '#7C3AED',
  High: '#EF4444',
  Medium: '#F97316',
  Low: '#3B82F6',
  None: '#94A3B8',
};

export function getPriorityShortLabel(p: TodoPriority): string {
  return priorityShortLabels[p] ?? '';
}

export function getPriorityColor(p: TodoPriority): string {
  return priorityColors[p] ?? '#94A3B8';
}

// ─── Difficulty ────────────────────────────────────

export const difficultyOptions: MetaOption<TodoDifficulty>[] = [
  { value: 'Hard', label: '困難', color: 'var(--todo-difficulty-hard)' },
  { value: 'Medium', label: '中等', color: 'var(--todo-difficulty-medium)' },
  { value: 'Easy', label: '容易', color: 'var(--todo-difficulty-easy)' },
  { value: 'None', label: '無', color: 'var(--todo-difficulty-none)' },
];

const difficultyShortLabels: Record<TodoDifficulty, string> = {
  Hard: '難',
  Medium: '中',
  Easy: '易',
  None: '無',
};

const difficultyColors: Record<TodoDifficulty, string> = {
  Hard: '#EF4444',
  Medium: '#F97316',
  Easy: '#22C55E',
  None: '#94A3B8',
};

export function getDifficultyShortLabel(d: TodoDifficulty): string {
  return difficultyShortLabels[d] ?? '';
}

export function getDifficultyColor(d: TodoDifficulty): string {
  return difficultyColors[d] ?? '#94A3B8';
}

// ─── Status ────────────────────────────────────────

export const statusOptions: MetaOption<TodoStatus>[] = [
  { value: 'Pending', label: '待處理', color: 'var(--todo-status-pending)' },
  {
    value: 'InProgress',
    label: '進行中',
    color: 'var(--todo-status-in-progress)',
  },
  { value: 'Done', label: '已完成', color: 'var(--todo-status-done)' },
];

const statusColors: Record<TodoStatus, string> = {
  Pending: '#94A3B8',
  InProgress: '#3B82F6',
  Done: '#22C55E',
};

export function getStatusColor(s: TodoStatus): string {
  return statusColors[s] ?? '#94A3B8';
}

// ─── Lucide icon paths ─────────────────────────────

/**
 * Lucide icon path collection.
 * 用於 sidebar / button 等需要 SVG icon 的位置。
 * 全部 stroke-width=2，stroke="currentColor"，viewBox="0 0 24 24"。
 */
export const todoIcons = {
  inbox:
    'M22 12h-6l-2 3h-4l-2-3H2 M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z',
  today:
    'M8 2v4 M16 2v4 M3 10h18 M21 13V6a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h8',
  upcoming: 'M12 6v6l4 2 M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0z',
  week: 'M8 2v4 M16 2v4 M3 10h18 M21 18V8a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v10a4 4 0 0 0 4 4h10a4 4 0 0 0 4-4z M9 14h.01 M13 14h.01 M17 14h.01 M9 18h.01 M13 18h.01',
  all: 'M3 6h18 M3 12h18 M3 18h18',
  trash:
    'M3 6h18 M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2 M10 11v6 M14 11v6',
  search: 'M21 21l-4.35-4.35 M11 19a8 8 0 1 1 0-16 8 8 0 0 1 0 16z',
  plus: 'M12 5v14 M5 12h14',
  close: 'M18 6 6 18 M6 6l12 12',
  check: 'M20 6 9 17l-5-5',
  chevronDown: 'm6 9 6 6 6-6',
  chevronRight: 'm9 18 6-6-6-6',
  flag: 'M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z M4 22V15',
  archive: 'M21 8v13H3V8 M1 3h22v5H1z M10 12h4',
  restore:
    'M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8 M3 3v5h5',
  calendar: 'M8 2v4 M16 2v4 M3 10h18 M21 18V8a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v10a4 4 0 0 0 4 4h10a4 4 0 0 0 4-4z',
  folder: 'M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z',
  tag: 'M12.586 2.586A2 2 0 0 0 11.172 2H4a2 2 0 0 0-2 2v7.172a2 2 0 0 0 .586 1.414l8.704 8.704a2.426 2.426 0 0 0 3.42 0l6.58-6.58a2.426 2.426 0 0 0 0-3.42z M7 7h.01',
  gripVertical: 'M9 5h.01 M9 12h.01 M9 19h.01 M15 5h.01 M15 12h.01 M15 19h.01',
  trash2:
    'M14 11v6 M10 11v6 M21 6V4a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1v2 M5 6h14l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2z',
};
