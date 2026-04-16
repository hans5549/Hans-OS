<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

import { preferences } from '@vben/preferences';
import { $t } from '#/locales';

import { Button, Card, Segmented, Tag, message } from 'ant-design-vue';

import {
  createQrCodeGeneratorPageContent,
  type ApiMethod,
  type FlowKey,
  type QrCodeGeneratorPageContent,
  type SectionId,
  type SectionMeta,
} from './content';
import FlowDiagram from './FlowDiagram.vue';
import FlowTimeline from './FlowTimeline.vue';
import GlossaryPanel from './GlossaryPanel.vue';

defineOptions({ name: 'QrCodeGeneratorDocumentTab' });

const methodClassMap: Record<ApiMethod, string> = {
  DELETE: 'method-badge method-badge--delete',
  GET: 'method-badge method-badge--get',
  POST: 'method-badge method-badge--post',
  PUT: 'method-badge method-badge--put',
};

const activeFlow = ref<FlowKey>('create');
const activeGlossaryId = ref('base62');
const activeSectionId = ref<null | SectionId>(null);
const pageContent = ref<QrCodeGeneratorPageContent>(createQrCodeGeneratorPageContent($t));

watch(
  () => preferences.app.locale,
  () => {
    pageContent.value = createQrCodeGeneratorPageContent($t);
  },
  { immediate: true },
);

const activeSectionIndex = computed(() => {
  if (!activeSectionId.value) {
    return -1;
  }

  return pageContent.value.sections.findIndex(
    (section) => section.id === activeSectionId.value,
  );
});
const currentFlowSteps = computed(() => pageContent.value.flowSteps[activeFlow.value]);
const currentSection = computed<SectionMeta | null>(() =>
  activeSectionIndex.value === -1
    ? null
    : pageContent.value.sections[activeSectionIndex.value] ?? pageContent.value.whyHardSection,
);
const currentSectionTitle = computed(
  () => currentSection.value?.title ?? pageContent.value.navigation.startReading,
);
const progressPercent = computed(() =>
  activeSectionIndex.value === -1
    ? 0
    : Math.round(
        ((activeSectionIndex.value + 1) / pageContent.value.sections.length) * 100,
      ),
);

let sectionObserver: IntersectionObserver | null = null;
const sectionVisibility = new Map<SectionId, number>();
const sectionObserverOptions: IntersectionObserverInit = {
  rootMargin: '-18% 0px -58% 0px',
  threshold: [0.2, 0.35, 0.5, 0.65],
};

async function handleCopy(snippet: string) {
  if (!navigator.clipboard) {
    message.error(pageContent.value.ui.copyUnsupported);
    return;
  }

  try {
    await navigator.clipboard.writeText(snippet);
    message.success(pageContent.value.ui.copySuccess);
  } catch {
    message.error(pageContent.value.ui.copyFailure);
  }
}

const getObservedSections = () =>
  pageContent.value.sections
    .map((section) => document.getElementById(section.id))
    .filter((element): element is HTMLElement => element !== null);

const getNextVisibleSection = () =>
  [...sectionVisibility.entries()]
    .filter(([, visibilityRatio]) => visibilityRatio > 0)
    .sort((left, right) => right[1] - left[1])[0]?.[0];

function updateActiveSection(entries: IntersectionObserverEntry[]) {
  for (const entry of entries) {
    const sectionId = entry.target.id as SectionId;
    sectionVisibility.set(sectionId, entry.isIntersecting ? entry.intersectionRatio : 0);
  }

  const nextSection = getNextVisibleSection();
  if (!nextSection) {
    return;
  }

  activeSectionId.value = nextSection;
}

function observeSections() {
  const elements = getObservedSections();

  if (elements.length === 0) {
    return;
  }

  if (typeof IntersectionObserver === 'undefined') {
    activeSectionId.value = pageContent.value.whyHardSection.id;
    return;
  }

  sectionObserver = new IntersectionObserver(updateActiveSection, sectionObserverOptions);

  elements.forEach((element) => sectionObserver?.observe(element));
}

function scrollToSection(sectionId: SectionId) {
  document.getElementById(sectionId)?.scrollIntoView({ behavior: 'smooth' });
}

const toggleGlossary = (termId: string) => {
  activeGlossaryId.value = activeGlossaryId.value === termId ? '' : termId;
};

onMounted(observeSections);

onBeforeUnmount(() => {
  sectionObserver?.disconnect();
  sectionVisibility.clear();
});
</script>

<template>
  <div class="doc-root">
    <!-- 頂部閱讀進度條 -->
    <div class="progress-bar" :style="{ width: `${progressPercent}%` }" />

    <div class="mx-auto flex w-full max-w-7xl flex-col gap-10 p-4 md:p-6 lg:p-8">

      <!-- ==================== HERO ==================== -->
      <section class="hero-section">
        <div class="flex flex-wrap gap-2">
          <Tag class="case-badge">{{ pageContent.badges.caseStudy }}</Tag>
          <Tag class="case-badge">{{ pageContent.badges.frontendOnly }}</Tag>
          <Tag class="case-badge">{{ pageContent.badges.basedOnPdf }}</Tag>
          <Tag class="case-badge">{{ pageContent.badges.readingTime }}</Tag>
        </div>

        <div class="mt-6 space-y-3">
          <p class="case-kicker">{{ pageContent.hero.kicker }}</p>
          <h1 class="text-3xl font-bold tracking-tight text-foreground lg:text-5xl">
            {{ pageContent.hero.title }}
          </h1>
          <p class="max-w-3xl text-base leading-7 text-muted-foreground lg:text-lg">
            {{ pageContent.hero.description }}
          </p>
        </div>

        <!-- 水平流程圖（inline） -->
        <div class="mt-8">
          <FlowDiagram
            :chips="pageContent.hero.visual.chips"
            :description="pageContent.hero.visual.description"
            :kicker="pageContent.hero.visual.kicker"
            :nodes="pageContent.hero.visual.nodes"
            :title="pageContent.hero.visual.title"
          />
        </div>

        <!-- 你會學到的重點 — 無邊框水平排列 -->
        <div class="mt-8 space-y-4">
          <p class="section-kicker">{{ pageContent.hero.learnKicker }}</p>
          <div class="grid gap-6 md:grid-cols-3">
            <div v-for="item in pageContent.hero.highlights" :key="item.title" class="highlight-item">
              <h3 class="text-sm font-bold text-foreground">{{ item.title }}</h3>
              <p class="mt-1 text-sm leading-6 text-muted-foreground">{{ item.description }}</p>
            </div>
          </div>
        </div>

        <!-- 設計目標 — 水平 stat strip -->
        <div class="stat-strip">
          <div class="flex items-center justify-between gap-3">
            <h2 class="text-base font-bold text-foreground">
              {{ pageContent.hero.targetsTitle }}
            </h2>
            <span class="text-xs text-muted-foreground">
              {{ pageContent.hero.targetsNote }}
            </span>
          </div>
          <div class="grid grid-cols-2 gap-4 sm:grid-cols-4">
            <div v-for="target in pageContent.designTargets" :key="target.label" class="stat-item">
              <p class="stat-item__value">{{ target.value }}</p>
              <p class="stat-item__label">{{ target.label }}</p>
              <p class="stat-item__note">{{ target.note }}</p>
            </div>
          </div>
        </div>
      </section>

      <!-- ==================== 行動版 TOC ==================== -->
      <section class="xl:hidden">
        <Card :bordered="false" class="case-panel">
          <div class="space-y-4">
            <details class="toc-details">
              <summary class="toc-details__summary">
                {{ pageContent.navigation.inPageLabel }} · {{ currentSectionTitle }}
              </summary>
              <nav
                class="mt-4 space-y-2"
                :aria-label="pageContent.navigation.mobileTocAriaLabel"
              >
                <a
                  v-for="section in pageContent.sections"
                  :key="section.id"
                  :aria-current="activeSectionId === section.id ? 'true' : undefined"
                  :class="[
                    'toc-link',
                    activeSectionId === section.id ? 'toc-link--active' : '',
                  ]"
                  :href="`#${section.id}`"
                  @click.prevent="scrollToSection(section.id)"
                >
                  <span class="toc-link__eyebrow">{{ section.eyebrow }}</span>
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

          <!-- 章節 1: 為什麼這題困難 — accent border 卡片 -->
          <section :id="pageContent.whyHardSection.id" class="section-anchor">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.whyHardSection.eyebrow }}</p>
                <h2 class="section-title">{{ pageContent.whyHardSection.title }}</h2>
              </div>
              <p class="section-takeaway">{{ pageContent.whyHardSection.takeaway }}</p>
            </div>

            <div class="grid gap-4 md:grid-cols-3">
              <div
                v-for="(item, index) in pageContent.tensionCards"
                :key="item.title"
                :class="['tension-card', `tension-card--${index}`]"
              >
                <h3 class="text-sm font-bold text-foreground">{{ item.title }}</h3>
                <p class="mt-2 text-sm leading-6 text-muted-foreground">{{ item.description }}</p>
              </div>
            </div>
          </section>

          <!-- 章節 2: 需求分析 — 雙欄 numbered list -->
          <section :id="pageContent.requirementsSection.id" class="section-anchor">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.requirementsSection.eyebrow }}</p>
                <h2 class="section-title">{{ pageContent.requirementsSection.title }}</h2>
              </div>
              <p class="section-takeaway">{{ pageContent.requirementsSection.takeaway }}</p>
            </div>

            <div class="grid gap-6 lg:grid-cols-2">
              <Card
                v-for="group in pageContent.requirementGroups"
                :key="group.title"
                :bordered="false"
                class="case-subcard h-full"
              >
                <div class="space-y-4">
                  <h3 class="text-base font-bold text-foreground">{{ group.title }}</h3>
                  <ol class="space-y-3 text-sm leading-6 text-muted-foreground">
                    <li
                      v-for="(item, idx) in group.items"
                      :key="item"
                      class="req-item"
                    >
                      <span class="req-number">{{ idx + 1 }}</span>
                      <span>{{ item }}</span>
                    </li>
                  </ol>
                </div>
              </Card>
            </div>
          </section>

          <!-- 章節 3: API Surface — 保留卡片但改善 code block -->
          <section :id="pageContent.apiSurfaceSection.id" class="section-anchor">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.apiSurfaceSection.eyebrow }}</p>
                <h2 class="section-title">{{ pageContent.apiSurfaceSection.title }}</h2>
              </div>
              <p class="section-takeaway">{{ pageContent.apiSurfaceSection.takeaway }}</p>
            </div>

            <div class="grid gap-4 xl:grid-cols-2">
              <Card
                v-for="card in pageContent.apiCards"
                :key="`${card.method}-${card.path}`"
                :bordered="false"
                class="api-card h-full"
              >
                <div class="flex items-start justify-between gap-4">
                  <div class="space-y-2">
                    <span :class="methodClassMap[card.method]">{{ card.method }}</span>
                    <div class="space-y-1">
                      <h3 class="text-base font-bold text-foreground">{{ card.title }}</h3>
                      <p class="break-all font-mono text-sm text-muted-foreground">{{ card.path }}</p>
                    </div>
                  </div>
                  <Button
                    :aria-label="pageContent.ui.getCopyApiAriaLabel(card.title)"
                    size="small"
                    type="text"
                    @click="handleCopy(card.snippet)"
                  >
                    {{ pageContent.ui.copyButton }}
                  </Button>
                </div>

                <p class="mt-3 text-sm leading-6 text-muted-foreground">{{ card.description }}</p>

                <div class="mt-3 flex flex-wrap gap-2">
                  <span
                    v-for="item in card.request"
                    :key="`${card.path}-request-${item}`"
                    class="pill-chip"
                  >
                    {{ pageContent.ui.requestLabel }} · {{ item }}
                  </span>
                  <span
                    v-for="item in card.response"
                    :key="`${card.path}-response-${item}`"
                    class="pill-chip"
                  >
                    {{ pageContent.ui.responseLabel }} · {{ item }}
                  </span>
                </div>

                <pre class="api-snippet" tabindex="0"><code>{{ card.snippet }}</code></pre>
              </Card>
            </div>
          </section>

          <!-- 章節 4: Request Lifecycle — Timeline -->
          <section :id="pageContent.lifecycleSection.id" class="section-anchor">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.lifecycleSection.eyebrow }}</p>
                <h2 class="section-title">{{ pageContent.lifecycleSection.title }}</h2>
              </div>
              <p class="section-takeaway">{{ pageContent.lifecycleSection.takeaway }}</p>
            </div>

            <div class="space-y-5">
              <Segmented
                v-model:value="activeFlow"
                :options="pageContent.flowOptions"
                block
              />
              <FlowTimeline :steps="currentFlowSteps" />
            </div>
          </section>

          <!-- 章節 5: Scaling 策略 — Bento Grid -->
          <section :id="pageContent.scalingSection.id" class="section-anchor">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.scalingSection.eyebrow }}</p>
                <h2 class="section-title">{{ pageContent.scalingSection.title }}</h2>
              </div>
              <p class="section-takeaway">{{ pageContent.scalingSection.takeaway }}</p>
            </div>

            <div class="bento-grid">
              <Card
                v-for="(item, index) in pageContent.scalingCards"
                :key="item.title"
                :bordered="false"
                :class="['case-subcard h-full', index === 0 ? 'bento-featured' : '']"
              >
                <div class="space-y-4">
                  <h3 class="text-base font-bold text-foreground">{{ item.title }}</h3>
                  <ul class="space-y-3 text-sm leading-6 text-muted-foreground">
                    <li
                      v-for="bullet in item.bullets"
                      :key="bullet"
                      class="list-disc pl-2 marker:text-primary"
                    >
                      {{ bullet }}
                    </li>
                  </ul>
                </div>
              </Card>
            </div>
          </section>

          <!-- 章節 6: Trade-offs — Decision Table -->
          <section :id="pageContent.tradeOffsSection.id" class="section-anchor">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.tradeOffsSection.eyebrow }}</p>
                <h2 class="section-title">{{ pageContent.tradeOffsSection.title }}</h2>
              </div>
              <p class="section-takeaway">{{ pageContent.tradeOffsSection.takeaway }}</p>
            </div>

            <div class="decision-table">
              <div
                v-for="item in pageContent.tradeOffs"
                :key="item.decision"
                class="decision-row"
              >
                <div class="decision-row__decision">
                  <p class="text-sm font-bold text-foreground">{{ item.decision }}</p>
                </div>
                <div class="decision-row__reason">
                  <p class="text-sm leading-6 text-muted-foreground">{{ item.reason }}</p>
                </div>
              </div>
            </div>
          </section>

          <!-- ==================== FOOTER ==================== -->
          <section class="footer-section">
            <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_280px]">
              <div class="footer-takeaways">
                <p class="section-kicker">{{ pageContent.footer.takeawaysKicker }}</p>
                <h3 class="mt-1 text-lg font-bold text-foreground">
                  {{ pageContent.footer.takeawaysTitle }}
                </h3>
                <ul class="mt-4 space-y-3 text-sm leading-6 text-muted-foreground">
                  <li
                    v-for="item in pageContent.footer.takeaways"
                    :key="item"
                    class="list-disc pl-2 marker:text-primary"
                  >
                    {{ item }}
                  </li>
                </ul>
              </div>

              <div class="footer-related">
                <p class="section-kicker">{{ pageContent.footer.relatedKicker }}</p>
                <h3 class="mt-1 text-base font-bold text-foreground">
                  {{ pageContent.footer.relatedTitle }}
                </h3>
                <div class="mt-4 space-y-2">
                  <div
                    v-for="item in pageContent.footer.relatedCases"
                    :key="item"
                    class="related-case"
                  >
                    <span class="text-sm font-medium text-foreground">{{ item }}</span>
                    <span class="text-xs text-muted-foreground">
                      {{ pageContent.footer.relatedComingSoon }}
                    </span>
                  </div>
                </div>
                <p class="mt-4 text-xs leading-5 text-muted-foreground">
                  {{ pageContent.footer.sourceNote }}
                </p>
              </div>
            </div>
          </section>
        </main>

        <!-- ==================== SIDEBAR ==================== -->
        <aside class="hidden xl:block">
          <div class="sticky top-6 space-y-4">
            <!-- TOC + Metadata -->
            <Card :bordered="false" class="case-panel">
              <div class="space-y-4">
                <dl class="grid gap-2">
                  <div class="meta-row">
                    <dt>{{ pageContent.sidebar.currentSectionLabel }}</dt>
                    <dd>{{ currentSectionTitle }}</dd>
                  </div>
                  <div class="meta-row">
                    <dt>{{ pageContent.sidebar.difficultyLabel }}</dt>
                    <dd>{{ pageContent.sidebar.difficultyValue }}</dd>
                  </div>
                  <div class="meta-row">
                    <dt>{{ pageContent.sidebar.scopeLabel }}</dt>
                    <dd>{{ pageContent.sidebar.scopeValue }}</dd>
                  </div>
                </dl>
              </div>
            </Card>

            <!-- 目錄 -->
            <Card :bordered="false" class="case-panel">
              <div class="space-y-3">
                <div>
                  <p class="section-kicker">{{ pageContent.sidebar.tocKicker }}</p>
                  <h2 class="text-base font-bold text-foreground">
                    {{ pageContent.sidebar.tocTitle }}
                  </h2>
                </div>

                <nav
                  class="space-y-1"
                  :aria-label="pageContent.navigation.desktopTocAriaLabel"
                >
                  <a
                    v-for="section in pageContent.sections"
                    :key="section.id"
                    :aria-current="activeSectionId === section.id ? 'true' : undefined"
                    :class="[
                      'toc-link',
                      activeSectionId === section.id ? 'toc-link--active' : '',
                    ]"
                    :href="`#${section.id}`"
                    @click.prevent="scrollToSection(section.id)"
                  >
                    <span class="toc-link__eyebrow">{{ section.eyebrow }}</span>
                    <span>{{ section.title }}</span>
                  </a>
                </nav>
              </div>
            </Card>

            <!-- 術語表 -->
            <Card :bordered="false" class="case-panel">
              <GlossaryPanel
                :active-term-id="activeGlossaryId"
                :get-toggle-aria-label="pageContent.ui.getGlossaryToggleAriaLabel"
                :kicker="pageContent.glossary.kicker"
                layout="desktop"
                :terms="pageContent.glossary.terms"
                :title="pageContent.glossary.title"
                title-class="text-base"
                @toggle="toggleGlossary"
              />
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
  background:
    linear-gradient(180deg, hsl(var(--card)) 0%, hsl(var(--background)) 100%);
  padding: 2rem;
  box-shadow: 0 24px 64px -56px hsl(var(--foreground) / 0.35);
}

.stat-strip {
  margin-top: 2rem;
  border-top: 1px solid hsl(var(--border));
  padding-top: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.stat-item {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
}

.stat-item__value {
  font-size: 1.75rem;
  font-weight: 800;
  letter-spacing: -0.02em;
  color: hsl(var(--foreground));
  line-height: 1.2;
}

.stat-item__label {
  font-size: 0.6875rem;
  font-weight: 600;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: hsl(var(--muted-foreground));
}

.stat-item__note {
  font-size: 0.75rem;
  color: hsl(var(--muted-foreground));
  line-height: 1.5;
}

.highlight-item {
  border-left: 2px solid hsl(var(--primary) / 0.3);
  padding-left: 1rem;
}

/* ── 卡片共用 ── */
.api-card,
.case-panel,
.case-subcard {
  border: 1px solid hsl(var(--border));
  background:
    linear-gradient(180deg, hsl(var(--card)) 0%, hsl(var(--background)) 100%);
  box-shadow: 0 24px 64px -56px hsl(var(--foreground) / 0.35);
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

.section-takeaway {
  max-width: 36rem;
  color: hsl(var(--muted-foreground));
  font-size: 0.9375rem;
  line-height: 1.7;
}

/* ── Kickers / Labels ── */
.case-kicker,
.section-kicker,
.toc-link__eyebrow {
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

/* ── Tension 卡片（accent border） ── */
.tension-card {
  border-radius: 0.75rem;
  border: 1px solid hsl(var(--border));
  border-left: 3px solid hsl(var(--primary) / 0.4);
  background-color: hsl(var(--card));
  padding: 1.25rem;
  transition: border-color 200ms ease;
}

.tension-card--0 {
  border-left-color: hsl(var(--primary) / 0.5);
}

.tension-card--1 {
  border-left-color: hsl(var(--warning) / 0.5);
}

.tension-card--2 {
  border-left-color: hsl(var(--destructive) / 0.5);
}

/* ── Requirement numbered list ── */
.req-item {
  display: flex;
  align-items: baseline;
  gap: 0.75rem;
}

.req-number {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 1.375rem;
  height: 1.375rem;
  border-radius: 999px;
  background-color: hsl(var(--primary) / 0.1);
  color: hsl(var(--primary));
  font-size: 0.6875rem;
  font-weight: 700;
}

/* ── API 卡片 ── */
.api-snippet {
  overflow-x: auto;
  margin-top: 0.75rem;
  border-radius: 0.75rem;
  border: 1px solid hsl(var(--border));
  background-color: hsl(var(--muted));
  padding: 1rem;
  color: hsl(var(--foreground));
  font-family: ui-monospace, SFMono-Regular, 'SF Mono', Menlo, monospace;
  font-size: 0.8125rem;
  line-height: 1.6;
}

.method-badge {
  display: inline-flex;
  align-items: center;
  border-radius: 999px;
  border: 1px solid transparent;
  padding: 0.25rem 0.6rem;
  font-size: 0.6875rem;
  font-weight: 700;
  letter-spacing: 0.08em;
}

.method-badge--delete {
  border-color: hsl(var(--destructive) / 0.18);
  background-color: hsl(var(--destructive) / 0.1);
  color: hsl(var(--destructive));
}

.method-badge--get {
  border-color: hsl(var(--success) / 0.18);
  background-color: hsl(var(--success) / 0.1);
  color: hsl(var(--success));
}

.method-badge--post {
  border-color: hsl(var(--primary) / 0.18);
  background-color: hsl(var(--primary) / 0.1);
  color: hsl(var(--primary));
}

.method-badge--put {
  border-color: hsl(var(--warning) / 0.18);
  background-color: hsl(var(--warning) / 0.1);
  color: hsl(var(--warning));
}

.pill-chip {
  display: inline-flex;
  align-items: center;
  border-radius: 999px;
  border: 1px solid hsl(var(--border));
  background-color: hsl(var(--background));
  padding: 0.3rem 0.7rem;
  font-size: 0.6875rem;
  color: hsl(var(--muted-foreground));
}

/* ── Bento Grid（Scaling） ── */
.bento-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: 1fr;
}

@media (min-width: 768px) {
  .bento-grid {
    grid-template-columns: repeat(2, 1fr);
    grid-template-rows: auto auto;
  }

  .bento-featured {
    grid-row: 1 / 3;
  }
}

/* ── Decision Table（Trade-offs） ── */
.decision-table {
  display: flex;
  flex-direction: column;
  border: 1px solid hsl(var(--border));
  border-radius: 0.75rem;
  overflow: hidden;
}

.decision-row {
  display: grid;
  gap: 0;
  grid-template-columns: 1fr;
  border-bottom: 1px solid hsl(var(--border));
}

.decision-row:last-child {
  border-bottom: none;
}

@media (min-width: 768px) {
  .decision-row {
    grid-template-columns: minmax(200px, 0.4fr) 1fr;
  }
}

.decision-row__decision {
  background-color: hsl(var(--muted) / 0.5);
  padding: 1rem 1.25rem;
}

.decision-row__reason {
  padding: 1rem 1.25rem;
}

/* ── Footer ── */
.footer-section {
  border-top: 1px solid hsl(var(--border));
  padding-top: 2rem;
}

.footer-takeaways {
  padding: 1.5rem;
  border-radius: 0.75rem;
  background: linear-gradient(
    135deg,
    hsl(var(--primary) / 0.04) 0%,
    hsl(var(--background)) 100%
  );
  border: 1px solid hsl(var(--primary) / 0.12);
}

.footer-related {
  padding: 1.5rem;
  border-radius: 0.75rem;
  border: 1px solid hsl(var(--border));
  background-color: hsl(var(--card));
}

.related-case {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-radius: 0.5rem;
  border: 1px solid hsl(var(--border));
  background-color: hsl(var(--background));
  padding: 0.75rem 1rem;
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
  border-color: hsl(var(--primary) / 0.28);
  background-color: hsl(var(--primary) / 0.08);
  color: hsl(var(--primary));
}

.toc-details {
  width: 100%;
}

.toc-details__summary {
  cursor: pointer;
  list-style: none;
  font-size: 0.9375rem;
  font-weight: 600;
  color: hsl(var(--foreground));
}

.toc-details__summary::-webkit-details-marker {
  display: none;
}

/* ── Responsive ── */
@media (min-width: 1024px) {
  .section-header {
    flex-direction: row;
    align-items: end;
    justify-content: space-between;
  }
}

/* ── 動態偏好 ── */
@media (prefers-reduced-motion: reduce) {
  .progress-bar {
    transition: none;
  }

  .toc-link,
  .tension-card {
    transition: none;
  }
}
</style>
