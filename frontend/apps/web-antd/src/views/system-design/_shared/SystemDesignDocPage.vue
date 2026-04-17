<script setup lang="ts">
import { computed, toRef } from 'vue';

import { Card, Collapse, CollapsePanel, Tag } from 'ant-design-vue';

import type { ContentBlock, SystemDesignContent } from './types';
import { useDocumentScroll } from './useDocumentScroll';

defineOptions({ name: 'SystemDesignDocPage' });

const props = defineProps<{
  category: string;
  categoryKey: string;
  content: SystemDesignContent;
}>();

const sections = toRef(() => props.content.sections);

const { activeSectionId, progressPercent, scrollToSection } =
  useDocumentScroll(sections);

const currentSectionTitle = computed(() => {
  const active = props.content.sections.find(
    (s) => s.id === activeSectionId.value,
  );
  return active?.title ?? '開始閱讀';
});

/** 閱讀時間估算：中文 300 字/分鐘 */
const readingTime = computed(() => {
  let charCount = 0;
  for (const section of props.content.sections) {
    for (const block of section.blocks) {
      charCount += getBlockTextLength(block);
    }
  }
  if (props.content.selfTest) {
    for (const item of props.content.selfTest) {
      charCount += item.question.length + item.answer.length;
    }
  }
  return Math.max(1, Math.ceil(charCount / 300));
});

function getBlockTextLength(block: ContentBlock): number {
  switch (block.type) {
    case 'code': {
      return block.code.length;
    }
    case 'list': {
      return block.items.reduce((sum, item) => sum + item.length, 0);
    }
    case 'paragraph':
    case 'subheading': {
      return block.text.length;
    }
    case 'callout': {
      return block.text.length + (block.title?.length ?? 0);
    }
    case 'keyvalue': {
      return block.items.reduce(
        (sum, item) => sum + item.label.length + item.value.length,
        0,
      );
    }
    case 'table': {
      return (
        block.headers.reduce((sum, h) => sum + h.length, 0) +
        block.rows.reduce(
          (sum, row) =>
            sum + row.reduce((rSum, cell) => rSum + cell.length, 0),
          0,
        )
      );
    }
  }
}

/** 第一段摘要（Hero 描述用） */
const heroDescription = computed(() => {
  const firstSection = props.content.sections[0];
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

const selfTestActiveKeys = computed(() =>
  props.content.selfTest ? [] : undefined,
);
</script>

<template>
  <div class="doc-root">
    <!-- 頂部閱讀進度條 -->
    <div class="progress-bar" :style="{ width: `${progressPercent}%` }" />

    <div class="mx-auto flex w-full max-w-7xl flex-col gap-10 p-4 md:p-6 lg:p-8">
      <!-- ==================== HERO ==================== -->
      <section class="hero-section">
        <div class="flex flex-wrap gap-2">
          <Tag class="case-badge">{{ props.category }}</Tag>
          <Tag class="case-badge">{{ `約 ${readingTime} 分鐘` }}</Tag>
        </div>

        <div class="mt-6 space-y-3">
          <p class="case-kicker">現代系統設計</p>
          <h1
            class="text-3xl font-bold tracking-tight text-foreground lg:text-5xl"
          >
            {{ props.content.title }}
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
        <Card :bordered="false" class="case-panel">
          <div class="space-y-4">
            <details class="toc-details">
              <summary class="toc-details__summary">
                目錄 · {{ currentSectionTitle }}
              </summary>
              <nav class="mt-4 space-y-2" aria-label="行動版目錄">
                <a
                  v-for="section in props.content.sections"
                  :key="section.id"
                  :aria-current="
                    activeSectionId === section.id ? 'true' : undefined
                  "
                  :class="[
                    'toc-link',
                    activeSectionId === section.id ? 'toc-link--active' : '',
                  ]"
                  :href="`#${section.id}`"
                  @click.prevent="scrollToSection(section.id)"
                >
                  <span>{{ section.title }}</span>
                </a>
              </nav>
            </details>
          </div>
        </Card>
      </section>

      <!-- ==================== MAIN + SIDEBAR ==================== -->
      <div class="grid gap-8 xl:grid-cols-[minmax(0,1fr)_260px]">
        <main class="space-y-10">
          <!-- 各 Section -->
          <section
            v-for="section in props.content.sections"
            :id="section.id"
            :key="section.id"
            class="section-anchor"
          >
            <div class="section-header">
              <h2 class="section-title">{{ section.title }}</h2>
            </div>

            <div class="section-content">
              <template v-for="(block, bIdx) in section.blocks" :key="bIdx">
                <!-- 段落 -->
                <p
                  v-if="block.type === 'paragraph'"
                  class="content-paragraph"
                >
                  {{ block.text }}
                </p>

                <!-- 子標題 -->
                <h3
                  v-else-if="block.type === 'subheading'"
                  class="content-subheading"
                >
                  {{ block.text }}
                </h3>

                <!-- 清單 -->
                <component
                  :is="block.type === 'list' && block.ordered ? 'ol' : 'ul'"
                  v-else-if="block.type === 'list'"
                  :class="[
                    'content-list',
                    block.ordered ? 'content-list--ordered' : '',
                  ]"
                >
                  <li v-for="(item, lIdx) in block.items" :key="lIdx">
                    {{ item }}
                  </li>
                </component>

                <!-- 表格 -->
                <div
                  v-else-if="block.type === 'table'"
                  class="content-table-wrapper"
                >
                  <table class="content-table">
                    <thead>
                      <tr>
                        <th v-for="header in block.headers" :key="header">
                          {{ header }}
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="(row, rIdx) in block.rows" :key="rIdx">
                        <td v-for="(cell, cIdx) in row" :key="cIdx">
                          {{ cell }}
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>

                <!-- 程式碼區塊 -->
                <pre
                  v-else-if="block.type === 'code'"
                  class="content-code"
                  tabindex="0"
                ><code>{{ block.code }}</code></pre>
              </template>
            </div>
          </section>

          <!-- ==================== 自我測驗 ==================== -->
          <section
            v-if="props.content.selfTest && props.content.selfTest.length > 0"
            id="self-test"
            class="section-anchor"
          >
            <div class="section-header">
              <h2 class="section-title">自我測驗</h2>
            </div>

            <Collapse
              :default-active-key="selfTestActiveKeys"
              class="self-test-collapse"
            >
              <CollapsePanel
                v-for="(item, qIdx) in props.content.selfTest"
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
          <section class="footer-section">
            <div class="footer-source">
              <p class="text-xs leading-5 text-muted-foreground">
                內容來源：《現代系統設計》· {{ props.category }}
              </p>
            </div>
          </section>
        </main>

        <!-- ==================== SIDEBAR ==================== -->
        <aside class="hidden xl:block">
          <div class="sticky top-6 space-y-4">
            <!-- Metadata -->
            <Card :bordered="false" class="case-panel">
              <div class="space-y-4">
                <dl class="grid gap-2">
                  <div class="meta-row">
                    <dt>類別</dt>
                    <dd>{{ props.category }}</dd>
                  </div>
                  <div class="meta-row">
                    <dt>閱讀時間</dt>
                    <dd>{{ `約 ${readingTime} 分鐘` }}</dd>
                  </div>
                  <div class="meta-row">
                    <dt>當前章節</dt>
                    <dd>{{ currentSectionTitle }}</dd>
                  </div>
                </dl>
              </div>
            </Card>

            <!-- 目錄 -->
            <Card :bordered="false" class="case-panel">
              <div class="space-y-3">
                <div>
                  <p class="section-kicker">導覽</p>
                  <h2 class="text-base font-bold text-foreground">目錄</h2>
                </div>

                <nav class="space-y-1" aria-label="桌面版目錄">
                  <a
                    v-for="section in props.content.sections"
                    :key="section.id"
                    :aria-current="
                      activeSectionId === section.id ? 'true' : undefined
                    "
                    :class="[
                      'toc-link',
                      activeSectionId === section.id ? 'toc-link--active' : '',
                    ]"
                    :href="`#${section.id}`"
                    @click.prevent="scrollToSection(section.id)"
                  >
                    <span>{{ section.title }}</span>
                  </a>
                </nav>

                <!-- 自我測驗 TOC link -->
                <a
                  v-if="
                    props.content.selfTest &&
                    props.content.selfTest.length > 0
                  "
                  class="toc-link"
                  href="#self-test"
                  @click.prevent="scrollToSection('self-test')"
                >
                  <span>自我測驗</span>
                </a>
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
.doc-root {
  position: relative;
}

.progress-bar {
  position: fixed;
  top: 0;
  left: 0;
  z-index: 50;
  height: 2px;
  background: hsl(var(--primary));
  transition: width 300ms ease-out;
}

/* ── Hero ── */
.hero-section {
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
.case-panel {
  border: 1px solid hsl(var(--border));
  background: linear-gradient(
    180deg,
    hsl(var(--card)) 0%,
    hsl(var(--background)) 100%
  );
  box-shadow: 0 24px 64px -56px hsl(var(--foreground) / 0.35);
}

/* ── Kickers / Labels ── */
.case-kicker,
.section-kicker {
  font-size: 0.6875rem;
  font-weight: 600;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: hsl(var(--muted-foreground));
}

.case-badge {
  border-color: hsl(var(--border));
  background-color: hsl(var(--muted));
  color: hsl(var(--muted-foreground));
  font-size: 0.6875rem;
  font-weight: 600;
}

/* ── Section 共用 ── */
.section-anchor {
  scroll-margin-top: 6rem;
}

.section-header {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1.5rem;
  border-bottom: 1px solid hsl(var(--border));
}

.section-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: hsl(var(--foreground));
  line-height: 1.3;
}

/* ── Section Content ── */
.section-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.content-paragraph {
  font-size: 0.9375rem;
  line-height: 1.8;
  color: hsl(var(--foreground));
}

.content-subheading {
  font-size: 1.125rem;
  font-weight: 600;
  color: hsl(var(--foreground));
  margin-top: 0.5rem;
}

.content-list {
  padding-left: 1.5rem;
  font-size: 0.9375rem;
  line-height: 1.8;
  color: hsl(var(--foreground));
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.content-list li {
  list-style-type: disc;
}

.content-list--ordered li {
  list-style-type: decimal;
}

.content-list li::marker {
  color: hsl(var(--primary));
}

/* ── 表格 ── */
.content-table-wrapper {
  overflow-x: auto;
  border-radius: 0.75rem;
  border: 1px solid hsl(var(--border));
}

.content-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;
}

.content-table th {
  background-color: hsl(var(--muted) / 0.5);
  padding: 0.75rem 1rem;
  text-align: left;
  font-weight: 600;
  color: hsl(var(--foreground));
  border-bottom: 1px solid hsl(var(--border));
}

.content-table td {
  padding: 0.75rem 1rem;
  color: hsl(var(--foreground));
  border-bottom: 1px solid hsl(var(--border));
}

.content-table tbody tr:last-child td {
  border-bottom: none;
}

.content-table tbody tr:hover {
  background-color: hsl(var(--muted) / 0.3);
}

/* ── 程式碼 ── */
.content-code {
  overflow-x: auto;
  border-radius: 0.75rem;
  border: 1px solid hsl(var(--border));
  background-color: hsl(var(--muted));
  padding: 1rem;
  color: hsl(var(--foreground));
  font-family: ui-monospace, SFMono-Regular, 'SF Mono', Menlo, monospace;
  font-size: 0.8125rem;
  line-height: 1.6;
}

/* ── 自我測驗 ── */
.self-test-collapse :deep(.ant-collapse-item) {
  border-color: hsl(var(--border));
}

.self-test-collapse :deep(.ant-collapse-header) {
  font-weight: 600;
  color: hsl(var(--foreground)) !important;
}

.self-test-collapse :deep(.ant-collapse-content) {
  border-color: hsl(var(--border));
}

/* ── TOC Details ── */
.toc-details__summary {
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 600;
  color: hsl(var(--foreground));
}

/* ── Footer ── */
.footer-section {
  border-top: 1px solid hsl(var(--border));
  padding-top: 2rem;
}

.footer-source {
  padding: 1.5rem;
  border-radius: 0.75rem;
  border: 1px solid hsl(var(--border));
  background-color: hsl(var(--card));
}

/* ── Sidebar ── */
.meta-row {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  font-size: 0.8125rem;
  line-height: 1.5;
}

.meta-row dt {
  color: hsl(var(--muted-foreground));
}

.meta-row dd {
  color: hsl(var(--foreground));
  font-weight: 600;
  text-align: right;
}

/* ── TOC Links ── */
.toc-link {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
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

.toc-link:hover,
.toc-link:focus-visible {
  border-color: hsl(var(--primary) / 0.2);
  color: hsl(var(--primary));
}

.toc-link--active {
  border-color: hsl(var(--primary) / 0.25);
  background-color: hsl(var(--primary) / 0.05);
  color: hsl(var(--primary));
  font-weight: 600;
}
</style>
