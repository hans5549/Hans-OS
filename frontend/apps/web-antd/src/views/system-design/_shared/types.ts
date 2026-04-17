/** 結構化內容型別 — 每個系統設計頁面的 content.ts 匯出此型別 */

export interface SystemDesignContent {
  title: string;
  sections: DocSection[];
  selfTest?: SelfTestItem[];
}

export interface DocSection {
  id: string;
  title: string;
  blocks: ContentBlock[];
}

export type ContentBlock =
  | CalloutBlock
  | CodeBlock
  | KeyValueBlock
  | ListBlock
  | ParagraphBlock
  | SubHeadingBlock
  | TableBlock;

export interface ParagraphBlock {
  type: 'paragraph';
  text: string;
}

export interface SubHeadingBlock {
  type: 'subheading';
  text: string;
}

export interface ListBlock {
  type: 'list';
  ordered: boolean;
  items: string[];
}

export interface TableBlock {
  type: 'table';
  headers: string[];
  rows: string[][];
}

export interface CodeBlock {
  type: 'code';
  language: string;
  code: string;
}

export interface CalloutBlock {
  type: 'callout';
  variant: 'info' | 'tip' | 'warning';
  title?: string;
  text: string;
}

export interface KeyValueItem {
  icon?: string;
  label: string;
  value: string;
}

export interface KeyValueBlock {
  type: 'keyvalue';
  items: KeyValueItem[];
}

export interface SelfTestItem {
  question: string;
  answer: string;
}

/** 頁面 metadata — 供 index.vue 傳遞給共享元件 */
export interface SystemDesignPageMeta {
  category: string;
  categoryKey: string;
}
