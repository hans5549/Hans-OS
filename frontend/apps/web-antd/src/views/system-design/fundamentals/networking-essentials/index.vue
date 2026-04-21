<script setup lang="ts">
import { computed, ref, toRef } from 'vue';

import { Card, Collapse, CollapsePanel, Tag } from 'ant-design-vue';

import type { ContentBlock } from '../../_shared/types';
import { useDocumentScroll } from '../../_shared/useDocumentScroll';
import { content } from './content';

defineOptions({ name: 'NetworkingEssentialsPage' });

const sections = toRef(() => content.sections);
const { activeSectionId, progressPercent, scrollToSection } =
  useDocumentScroll(sections);

/** 章節圖示對照 */
const sectionIcons: Record<string, string> = {
  '101': 'i-lucide-globe',
  'section-1': 'i-lucide-layers',
  'section-2': 'i-lucide-app-window',
  'section-3': 'i-lucide-scale',
  'section-4': 'i-lucide-search',
  'section-5': 'i-lucide-bookmark-check',
};

const currentSectionTitle = computed(() => {
  const active = content.sections.find(
    (s) => s.id === activeSectionId.value,
  );
  return active?.title ?? '開始閱讀';
});

/** 閱讀時間估算：中文 300 字/分鐘 */
const readingTime = computed(() => {
  let charCount = 0;
  for (const section of content.sections) {
    for (const block of section.blocks) {
      charCount += getBlockTextLength(block);
    }
  }
  if (content.selfTest) {
    for (const item of content.selfTest) {
      charCount += item.question.length + item.answer.length;
    }
  }
  return Math.max(1, Math.ceil(charCount / 300));
});

function getBlockTextLength(block: ContentBlock): number {
  switch (block.type) {
    case 'callout': {
      return block.text.length + (block.title?.length ?? 0);
    }
    case 'code': {
      return block.code.length;
    }
    case 'keyvalue': {
      return block.items.reduce(
        (sum, i) => sum + i.label.length + i.value.length,
        0,
      );
    }
    case 'list': {
      return block.items.reduce((sum, i) => sum + i.length, 0);
    }
    case 'paragraph':
    case 'subheading': {
      return block.text.length;
    }
    case 'table': {
      return (
        block.headers.reduce((sum, h) => sum + h.length, 0) +
        block.rows.reduce(
          (sum, r) => sum + r.reduce((rs, c) => rs + c.length, 0),
          0,
        )
      );
    }
  }
}

/** 第一段摘要（Hero 描述用） */
const heroDescription = computed(() => {
  const firstSection = content.sections[0];
  if (!firstSection) return '';
  const firstParagraph = firstSection.blocks.find(
    (b) => b.type === 'paragraph',
  );
  if (firstParagraph?.type === 'paragraph') {
    return firstParagraph.text.length > 200
      ? `${firstParagraph.text.slice(0, 200)}…`
      : firstParagraph.text;
  }
  return '';
});

const selfTestActiveKeys = ref<number[]>([]);
</script>

<template>
  <div class="ne-root">
    <!-- 頂部閱讀進度條 -->
    <div class="ne-progress" :style="{ width: `${progressPercent}%` }" />

    <div
      class="mx-auto flex w-full max-w-7xl flex-col gap-10 p-4 md:p-6 lg:p-8"
    >
      <!-- ==================== HERO ==================== -->
      <section class="ne-hero">
        <div class="flex flex-wrap items-center gap-2">
          <Tag class="ne-badge">
            <span class="i-lucide-book-open mr-1 inline-block align-[-2px]" />
            基本觀念
          </Tag>
          <Tag class="ne-badge">
            <span class="i-lucide-clock mr-1 inline-block align-[-2px]" />
            {{ `約 ${readingTime} 分鐘` }}
          </Tag>
        </div>

        <div class="mt-6 space-y-3">
          <p class="ne-kicker">現代系統設計</p>
          <h1
            class="text-3xl font-bold tracking-tight text-foreground lg:text-5xl"
          >
            {{ content.title }}
          </h1>
          <p
            v-if="heroDescription"
            class="max-w-3xl text-base leading-7 text-muted-foreground lg:text-lg"
          >
            {{ heroDescription }}
          </p>
        </div>
      </section>

      <!-- ==================== 行動版 TOC ==================== -->
      <section class="xl:hidden">
        <Card :bordered="false" class="ne-panel">
          <details class="ne-toc-details">
            <summary class="ne-toc-summary">
              目錄 · {{ currentSectionTitle }}
            </summary>
            <nav class="mt-4 space-y-2" aria-label="行動版目錄">
              <a
                v-for="s in content.sections"
                :key="s.id"
                :aria-current="
                  activeSectionId === s.id ? 'true' : undefined
                "
                :class="[
                  'ne-toc-link',
                  activeSectionId === s.id && 'ne-toc-link--active',
                ]"
                :href="`#${s.id}`"
                @click.prevent="scrollToSection(s.id)"
              >
                <span
                  v-if="sectionIcons[s.id]"
                  :class="[sectionIcons[s.id], 'ne-toc-icon']"
                />
                <span>{{ s.title }}</span>
              </a>
            </nav>
          </details>
        </Card>
      </section>

      <!-- ==================== MAIN + SIDEBAR ==================== -->
      <div class="grid gap-8 xl:grid-cols-[minmax(0,1fr)_260px]">
        <main class="space-y-12">
          <!-- 各 Section -->
          <section
            v-for="section in content.sections"
            :id="section.id"
            :key="section.id"
            class="ne-section"
          >
            <div class="ne-section-header">
              <span
                v-if="sectionIcons[section.id]"
                :class="[sectionIcons[section.id], 'ne-section-icon']"
              />
              <h2 class="ne-section-title">{{ section.title }}</h2>
            </div>

            <div class="ne-section-body">
              <template
                v-for="(block, bIdx) in section.blocks"
                :key="bIdx"
              >
                <!-- 段落 -->
                <p
                  v-if="block.type === 'paragraph'"
                  class="ne-paragraph"
                >
                  {{ block.text }}
                </p>

                <!-- 子標題 -->
                <h3
                  v-else-if="block.type === 'subheading'"
                  class="ne-subheading"
                >
                  {{ block.text }}
                </h3>

                <!-- 清單 -->
                <component
                  :is="
                    block.type === 'list' && block.ordered ? 'ol' : 'ul'
                  "
                  v-else-if="block.type === 'list'"
                  :class="[
                    'ne-list',
                    block.ordered && 'ne-list--ordered',
                  ]"
                >
                  <li
                    v-for="(item, lIdx) in block.items"
                    :key="lIdx"
                  >
                    {{ item }}
                  </li>
                </component>

                <!-- 表格 -->
                <div
                  v-else-if="block.type === 'table'"
                  class="ne-table-wrap"
                >
                  <table class="ne-table">
                    <thead>
                      <tr>
                        <th
                          v-for="header in block.headers"
                          :key="header"
                        >
                          {{ header }}
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr
                        v-for="(row, rIdx) in block.rows"
                        :key="rIdx"
                      >
                        <td
                          v-for="(cell, cIdx) in row"
                          :key="cIdx"
                        >
                          {{ cell }}
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>

                <!-- 程式碼區塊 -->
                <div
                  v-else-if="block.type === 'code'"
                  class="ne-code-wrap"
                >
                  <div class="ne-code-lang">{{ block.language }}</div>
                  <pre class="ne-code" tabindex="0"><code>{{ block.code }}</code></pre>
                </div>

                <!-- 提示 / 警告 / 資訊方塊 -->
                <div
                  v-else-if="block.type === 'callout'"
                  :class="[
                    'ne-callout',
                    `ne-callout--${block.variant}`,
                  ]"
                >
                  <div class="ne-callout__icon">
                    <span
                      v-if="block.variant === 'info'"
                      class="i-lucide-info"
                    />
                    <span
                      v-else-if="block.variant === 'tip'"
                      class="i-lucide-lightbulb"
                    />
                    <span
                      v-else-if="block.variant === 'warning'"
                      class="i-lucide-triangle-alert"
                    />
                  </div>
                  <div class="ne-callout__body">
                    <p
                      v-if="block.title"
                      class="ne-callout__title"
                    >
                      {{ block.title }}
                    </p>
                    <p class="ne-callout__text">
                      {{ block.text }}
                    </p>
                  </div>
                </div>

                <!-- 關鍵特性清單 (key-value) -->
                <div
                  v-else-if="block.type === 'keyvalue'"
                  class="ne-kv"
                >
                  <div
                    v-for="(kv, kvIdx) in block.items"
                    :key="kvIdx"
                    class="ne-kv__item"
                  >
                    <span
                      v-if="kv.icon"
                      :class="['ne-kv__icon', kv.icon]"
                    />
                    <span v-else class="ne-kv__dot" />
                    <div class="ne-kv__content">
                      <span class="ne-kv__label">{{ kv.label }}</span>
                      <span class="ne-kv__value">{{ kv.value }}</span>
                    </div>
                  </div>
                </div>
              </template>
            </div>
          </section>

          <!-- ==================== 自我測驗 ==================== -->
          <section
            v-if="content.selfTest && content.selfTest.length > 0"
            id="self-test"
            class="ne-section"
          >
            <div class="ne-section-header">
              <span class="i-lucide-brain ne-section-icon" />
              <h2 class="ne-section-title">自我測驗</h2>
            </div>

            <Collapse
              v-model:activeKey="selfTestActiveKeys"
              class="ne-collapse"
            >
              <CollapsePanel
                v-for="(item, qIdx) in content.selfTest"
                :key="qIdx"
                :header="item.question"
              >
                <p class="text-sm leading-7 text-muted-foreground">
                  {{ item.answer }}
                </p>
              </CollapsePanel>
            </Collapse>
          </section>

          <!-- ==================== FOOTER ==================== -->
          <section class="ne-footer">
            <div class="ne-footer__source">
              <p class="text-xs leading-5 text-muted-foreground">
                內容來源：《現代系統設計》· 基本觀念
              </p>
            </div>
          </section>
        </main>

        <!-- ==================== SIDEBAR ==================== -->
        <aside class="hidden xl:block">
          <div class="sticky top-6 space-y-4">
            <!-- Metadata -->
            <Card :bordered="false" class="ne-panel">
              <dl class="grid gap-2">
                <div class="ne-meta">
                  <dt>類別</dt>
                  <dd>基本觀念</dd>
                </div>
                <div class="ne-meta">
                  <dt>閱讀時間</dt>
                  <dd>{{ `約 ${readingTime} 分鐘` }}</dd>
                </div>
                <div class="ne-meta">
                  <dt>當前章節</dt>
                  <dd>{{ currentSectionTitle }}</dd>
                </div>
              </dl>
            </Card>

            <!-- 目錄 -->
            <Card :bordered="false" class="ne-panel">
              <div class="space-y-3">
                <div>
                  <p class="ne-sidebar-kicker">導覽</p>
                  <h2 class="text-base font-bold text-foreground">
                    目錄
                  </h2>
                </div>

                <nav class="space-y-1" aria-label="桌面版目錄">
                  <a
                    v-for="s in content.sections"
                    :key="s.id"
                    :aria-current="
                      activeSectionId === s.id ? 'true' : undefined
                    "
                    :class="[
                      'ne-toc-link',
                      activeSectionId === s.id && 'ne-toc-link--active',
                    ]"
                    :href="`#${s.id}`"
                    @click.prevent="scrollToSection(s.id)"
                  >
                    <span
                      v-if="sectionIcons[s.id]"
                      :class="[sectionIcons[s.id], 'ne-toc-icon']"
                    />
                    <span>{{ s.title }}</span>
                  </a>

                  <!-- 自我測驗 TOC link -->
                  <a
                    v-if="
                      content.selfTest && content.selfTest.length > 0
                    "
                    class="ne-toc-link"
                    href="#self-test"
                    @click.prevent="scrollToSection('self-test')"
                  >
                    <span class="i-lucide-brain ne-toc-icon" />
                    <span>自我測驗</span>
                  </a>
                </nav>
              </div>
            </Card>
          </div>
        </aside>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ── 全域 ── */
.ne-root {
  position: relative;
}

.ne-progress {
  position: fixed;
  top: 0;
  left: 0;
  z-index: 50;
  height: 2px;
  background: hsl(var(--primary));
  transition: width 300ms ease-out;
}

/* ── Hero ── */
.ne-hero {
  border: 1px solid hsl(var(--border));
  border-radius: 1rem;
  background: linear-gradient(
    180deg,
    hsl(var(--card)) 0%,
    hsl(var(--background)) 100%
  );
  padding: 2rem;
  box-shadow: 0 24px 64px -56px hsl(var(--foreground) / 0.35);
}

/* ── 卡片共用 ── */
.ne-panel {
  border: 1px solid hsl(var(--border));
  background: linear-gradient(
    180deg,
    hsl(var(--card)) 0%,
    hsl(var(--background)) 100%
  );
  box-shadow: 0 24px 64px -56px hsl(var(--foreground) / 0.35);
}

/* ── Kickers / Labels ── */
.ne-kicker {
  font-size: 0.6875rem;
  font-weight: 600;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: hsl(var(--primary));
}

.ne-sidebar-kicker {
  font-size: 0.6875rem;
  font-weight: 600;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: hsl(var(--muted-foreground));
}

.ne-badge {
  border-color: hsl(var(--border));
  background-color: hsl(var(--muted));
  color: hsl(var(--muted-foreground));
  font-size: 0.6875rem;
  font-weight: 600;
}

/* ── Section ── */
.ne-section {
  scroll-margin-top: 6rem;
}

.ne-section-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1.5rem;
  border-bottom: 1px solid hsl(var(--border));
}

.ne-section-icon {
  flex-shrink: 0;
  font-size: 1.375rem;
  color: hsl(var(--primary));
}

.ne-section-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: hsl(var(--foreground));
  line-height: 1.3;
}

/* ── Section Content ── */
.ne-section-body {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.ne-paragraph {
  font-size: 0.9375rem;
  line-height: 1.8;
  color: hsl(var(--foreground));
}

.ne-subheading {
  font-size: 1.125rem;
  font-weight: 600;
  color: hsl(var(--foreground));
  margin-top: 0.75rem;
  padding-left: 0.75rem;
  border-left: 3px solid hsl(var(--primary));
}

/* ── 清單 ── */
.ne-list {
  padding-left: 1.5rem;
  font-size: 0.9375rem;
  line-height: 1.8;
  color: hsl(var(--foreground));
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.ne-list li {
  list-style-type: disc;
}

.ne-list--ordered li {
  list-style-type: decimal;
}

.ne-list li::marker {
  color: hsl(var(--primary));
}

/* ── 表格 ── */
.ne-table-wrap {
  overflow-x: auto;
  border-radius: 0.75rem;
  border: 1px solid hsl(var(--border));
}

.ne-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;
}

.ne-table th {
  background-color: hsl(var(--muted) / 0.5);
  padding: 0.75rem 1rem;
  text-align: left;
  font-weight: 600;
  color: hsl(var(--foreground));
  border-bottom: 1px solid hsl(var(--border));
}

.ne-table td {
  padding: 0.75rem 1rem;
  color: hsl(var(--foreground));
  border-bottom: 1px solid hsl(var(--border));
}

.ne-table tbody tr:last-child td {
  border-bottom: none;
}

.ne-table tbody tr:hover {
  background-color: hsl(var(--muted) / 0.3);
}

/* ── 程式碼 ── */
.ne-code-wrap {
  position: relative;
  border-radius: 0.75rem;
  border: 1px solid hsl(var(--border));
  overflow: hidden;
}

.ne-code-lang {
  position: absolute;
  top: 0;
  right: 0;
  padding: 0.25rem 0.625rem;
  font-size: 0.6875rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: hsl(var(--muted-foreground));
  background-color: hsl(var(--muted));
  border-bottom-left-radius: 0.5rem;
}

.ne-code {
  overflow-x: auto;
  background-color: hsl(var(--muted));
  padding: 1rem;
  color: hsl(var(--foreground));
  font-family: ui-monospace, SFMono-Regular, 'SF Mono', Menlo, monospace;
  font-size: 0.8125rem;
  line-height: 1.6;
  margin: 0;
}

/* ── Callout（提示 / 警告 / 資訊方塊） ── */
.ne-callout {
  display: flex;
  gap: 0.75rem;
  border-radius: 0.75rem;
  border: 1px solid;
  padding: 1rem 1.25rem;
}

.ne-callout--info {
  border-color: hsl(210 100% 50% / 0.25);
  background-color: hsl(210 100% 50% / 0.06);
}

.ne-callout--tip {
  border-color: hsl(150 60% 40% / 0.25);
  background-color: hsl(150 60% 40% / 0.06);
}

.ne-callout--warning {
  border-color: hsl(38 92% 50% / 0.3);
  background-color: hsl(38 92% 50% / 0.06);
}

.ne-callout__icon {
  flex-shrink: 0;
  font-size: 1.125rem;
  margin-top: 0.125rem;
}

.ne-callout--info .ne-callout__icon {
  color: hsl(210 100% 55%);
}

.ne-callout--tip .ne-callout__icon {
  color: hsl(150 60% 40%);
}

.ne-callout--warning .ne-callout__icon {
  color: hsl(38 92% 50%);
}

.ne-callout__body {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 0;
}

.ne-callout__title {
  font-size: 0.875rem;
  font-weight: 700;
  color: hsl(var(--foreground));
}

.ne-callout__text {
  font-size: 0.875rem;
  line-height: 1.7;
  color: hsl(var(--foreground) / 0.85);
}

/* ── KeyValue（關鍵特性清單） ── */
.ne-kv {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
  border-radius: 0.75rem;
  border: 1px solid hsl(var(--border));
  background-color: hsl(var(--card));
  padding: 1.25rem 1.5rem;
}

.ne-kv__item {
  display: flex;
  align-items: flex-start;
  gap: 0.625rem;
  font-size: 0.9375rem;
  line-height: 1.7;
}

.ne-kv__icon {
  flex-shrink: 0;
  font-size: 0.875rem;
  color: hsl(var(--primary));
  margin-top: 0.3rem;
}

.ne-kv__dot {
  flex-shrink: 0;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background-color: hsl(var(--primary));
  margin-top: 0.6rem;
}

.ne-kv__content {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
}

.ne-kv__label {
  font-weight: 600;
  color: hsl(var(--foreground));
}

.ne-kv__value {
  color: hsl(var(--muted-foreground));
  font-size: 0.875rem;
}

/* ── 自我測驗 ── */
.ne-collapse :deep(.ant-collapse-item) {
  border-color: hsl(var(--border));
}

.ne-collapse :deep(.ant-collapse-header) {
  font-weight: 600;
  color: hsl(var(--foreground)) !important;
}

.ne-collapse :deep(.ant-collapse-content) {
  border-color: hsl(var(--border));
}

/* ── TOC ── */
.ne-toc-details {
  /* 行動版摺疊 */
}

.ne-toc-summary {
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 600;
  color: hsl(var(--foreground));
}

.ne-toc-link {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  border-radius: 0.5rem;
  border: 1px solid transparent;
  padding: 0.625rem 0.75rem;
  color: hsl(var(--foreground));
  text-decoration: none;
  font-size: 0.8125rem;
  transition:
    border-color 200ms ease,
    color 200ms ease,
    background-color 200ms ease;
}

.ne-toc-link:hover,
.ne-toc-link:focus-visible {
  border-color: hsl(var(--primary) / 0.2);
  color: hsl(var(--primary));
}

.ne-toc-link--active {
  border-color: hsl(var(--primary) / 0.25);
  background-color: hsl(var(--primary) / 0.05);
  color: hsl(var(--primary));
  font-weight: 600;
}

.ne-toc-icon {
  flex-shrink: 0;
  font-size: 0.875rem;
}

/* ── Footer ── */
.ne-footer {
  border-top: 1px solid hsl(var(--border));
  padding-top: 2rem;
}

.ne-footer__source {
  padding: 1.5rem;
  border-radius: 0.75rem;
  border: 1px solid hsl(var(--border));
  background-color: hsl(var(--card));
}

/* ── Sidebar meta ── */
.ne-meta {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  font-size: 0.8125rem;
  line-height: 1.5;
}

.ne-meta dt {
  color: hsl(var(--muted-foreground));
}

.ne-meta dd {
  color: hsl(var(--foreground));
  font-weight: 600;
  text-align: right;
}
</style>
