<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

import { preferences } from '@vben/preferences';
import { $t } from '#/locales';

import { Button, Card, Progress, Segmented, Tag, message } from 'ant-design-vue';

import {
  createQrCodeGeneratorPageContent,
  type ApiMethod,
  type FlowKey,
  type QrCodeGeneratorPageContent,
  type SectionId,
  type SectionMeta,
} from './content';
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
  <div class="mx-auto flex w-full max-w-7xl flex-col gap-6 p-4 md:p-6 lg:p-8">
    <section class="case-panel overflow-hidden">
      <div class="grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_360px]">
        <div class="space-y-6">
          <div class="flex flex-wrap gap-2">
            <Tag class="case-badge">{{ pageContent.badges.caseStudy }}</Tag>
            <Tag class="case-badge">{{ pageContent.badges.frontendOnly }}</Tag>
            <Tag class="case-badge">{{ pageContent.badges.basedOnPdf }}</Tag>
            <Tag class="case-badge">{{ pageContent.badges.readingTime }}</Tag>
          </div>

          <div class="space-y-4">
            <p class="case-kicker">{{ pageContent.hero.kicker }}</p>
            <div class="space-y-3">
              <h1 class="text-3xl font-semibold tracking-tight text-foreground lg:text-5xl">
                {{ pageContent.hero.title }}
              </h1>
              <p class="max-w-3xl text-base leading-7 text-muted-foreground lg:text-lg">
                {{ pageContent.hero.description }}
              </p>
            </div>
          </div>

          <div class="grid gap-4 md:grid-cols-3">
            <Card
              v-for="item in pageContent.hero.highlights"
              :key="item.title"
              :bordered="false"
              class="case-subcard"
            >
              <div class="space-y-2">
                <h2 class="text-base font-semibold text-foreground">
                  {{ item.title }}
                </h2>
                <p class="text-sm leading-6 text-muted-foreground">
                  {{ item.description }}
                </p>
              </div>
            </Card>
          </div>

          <div class="space-y-3">
            <div class="flex items-center justify-between gap-3">
              <div>
                <p class="section-kicker">{{ pageContent.hero.learnKicker }}</p>
                <h2 class="text-lg font-semibold text-foreground">
                  {{ pageContent.hero.targetsTitle }}
                </h2>
              </div>
              <span class="text-xs text-muted-foreground">
                {{ pageContent.hero.targetsNote }}
              </span>
            </div>
            <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <Card
                v-for="target in pageContent.designTargets"
                :key="target.label"
                :bordered="false"
                class="target-card"
                size="small"
              >
                <div class="space-y-1">
                  <p class="text-xs font-medium uppercase tracking-[0.18em] text-muted-foreground">
                    {{ target.label }}
                  </p>
                  <p class="text-2xl font-semibold text-foreground">
                    {{ target.value }}
                  </p>
                  <p class="text-xs leading-5 text-muted-foreground">
                    {{ target.note }}
                  </p>
                </div>
              </Card>
            </div>
          </div>
        </div>

        <Card :bordered="false" class="hero-artifact">
          <div class="space-y-5">
            <div class="space-y-2">
              <p class="section-kicker">{{ pageContent.hero.visual.kicker }}</p>
              <h2 class="text-lg font-semibold text-foreground">
                {{ pageContent.hero.visual.title }}
              </h2>
              <p class="text-sm leading-6 text-muted-foreground">
                {{ pageContent.hero.visual.description }}
              </p>
            </div>

            <div class="space-y-3">
              <template
                v-for="(node, index) in pageContent.hero.visual.nodes"
                :key="node.title"
              >
                <div :class="['diagram-node', index === 0 ? 'diagram-node--accent' : '']">
                  <span class="diagram-label">{{ node.label }}</span>
                  <strong class="diagram-title">{{ node.title }}</strong>
                </div>
                <div
                  v-if="index < pageContent.hero.visual.nodes.length - 1"
                  class="diagram-arrow"
                  aria-hidden="true"
                >
                  ↓
                </div>
              </template>
            </div>

            <div class="grid gap-3 sm:grid-cols-3">
              <div
                v-for="chip in pageContent.hero.visual.chips"
                :key="chip.label"
                class="artifact-chip"
              >
                <span class="artifact-chip__label">{{ chip.label }}</span>
                <span class="artifact-chip__value">{{ chip.value }}</span>
              </div>
            </div>
          </div>
        </Card>
      </div>
    </section>

    <section class="xl:hidden">
      <Card :bordered="false" class="case-panel">
        <div class="space-y-4">
          <div class="space-y-2">
            <div class="flex items-center justify-between gap-3">
              <span class="text-sm font-medium text-foreground">
                {{ pageContent.navigation.progressLabel }}
              </span>
              <span class="text-sm text-muted-foreground">
                {{ progressPercent }}%
              </span>
            </div>
            <Progress :percent="progressPercent" :show-info="false" size="small" />
          </div>
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
              >
                <span class="toc-link__eyebrow">{{ section.eyebrow }}</span>
                <span>{{ section.title }}</span>
              </a>
            </nav>
          </details>
          <GlossaryPanel
            :active-term-id="activeGlossaryId"
            :get-toggle-aria-label="pageContent.ui.getGlossaryToggleAriaLabel"
            :kicker="pageContent.glossary.kicker"
            layout="mobile"
            :terms="pageContent.glossary.terms"
            :title="pageContent.glossary.title"
            title-class="text-base"
            @toggle="toggleGlossary"
          />
        </div>
      </Card>
    </section>

    <div class="grid gap-6 xl:grid-cols-[minmax(0,1fr)_280px]">
      <main class="space-y-6">
        <section :id="pageContent.whyHardSection.id" class="section-anchor">
          <Card :bordered="false" class="case-panel">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.whyHardSection.eyebrow }}</p>
                <h2 class="text-2xl font-semibold text-foreground">
                  {{ pageContent.whyHardSection.title }}
                </h2>
              </div>
              <p class="section-takeaway">
                {{ pageContent.whyHardSection.takeaway }}
              </p>
            </div>

            <div class="grid gap-4 md:grid-cols-3">
              <Card
                v-for="item in pageContent.tensionCards"
                :key="item.title"
                :bordered="false"
                class="case-subcard h-full"
              >
                <div class="space-y-3">
                  <h3 class="text-base font-semibold text-foreground">
                    {{ item.title }}
                  </h3>
                  <p class="text-sm leading-6 text-muted-foreground">
                    {{ item.description }}
                  </p>
                </div>
              </Card>
            </div>
          </Card>
        </section>

        <section :id="pageContent.requirementsSection.id" class="section-anchor">
          <Card :bordered="false" class="case-panel">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.requirementsSection.eyebrow }}</p>
                <h2 class="text-2xl font-semibold text-foreground">
                  {{ pageContent.requirementsSection.title }}
                </h2>
              </div>
              <p class="section-takeaway">
                {{ pageContent.requirementsSection.takeaway }}
              </p>
            </div>

            <div class="grid gap-4 lg:grid-cols-2">
              <Card
                v-for="group in pageContent.requirementGroups"
                :key="group.title"
                :bordered="false"
                class="case-subcard h-full"
              >
                <div class="space-y-4">
                  <h3 class="text-lg font-semibold text-foreground">
                    {{ group.title }}
                  </h3>
                  <ul class="space-y-3 text-sm leading-6 text-muted-foreground">
                    <li
                      v-for="item in group.items"
                      :key="item"
                      class="list-disc pl-2 marker:text-primary"
                    >
                      {{ item }}
                    </li>
                  </ul>
                </div>
              </Card>
            </div>
          </Card>
        </section>

        <section :id="pageContent.apiSurfaceSection.id" class="section-anchor">
          <Card :bordered="false" class="case-panel">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.apiSurfaceSection.eyebrow }}</p>
                <h2 class="text-2xl font-semibold text-foreground">
                  {{ pageContent.apiSurfaceSection.title }}
                </h2>
              </div>
              <p class="section-takeaway">
                {{ pageContent.apiSurfaceSection.takeaway }}
              </p>
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
                      <h3 class="text-lg font-semibold text-foreground">
                        {{ card.title }}
                      </h3>
                      <p class="break-all text-sm font-medium text-foreground">
                        {{ card.path }}
                      </p>
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

                <p class="mt-4 text-sm leading-6 text-muted-foreground">
                  {{ card.description }}
                </p>

                <div class="mt-4 flex flex-wrap gap-2">
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
          </Card>
        </section>

        <section :id="pageContent.lifecycleSection.id" class="section-anchor">
          <Card :bordered="false" class="case-panel">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.lifecycleSection.eyebrow }}</p>
                <h2 class="text-2xl font-semibold text-foreground">
                  {{ pageContent.lifecycleSection.title }}
                </h2>
              </div>
              <p class="section-takeaway">
                {{ pageContent.lifecycleSection.takeaway }}
              </p>
            </div>

            <div class="space-y-5">
              <Segmented
                v-model:value="activeFlow"
                :options="pageContent.flowOptions"
                block
              />

              <div class="grid gap-4">
                <Card
                  v-for="step in currentFlowSteps"
                  :key="step.title"
                  :bordered="false"
                  class="case-subcard"
                >
                  <div class="grid gap-3 lg:grid-cols-[180px_minmax(0,1fr)]">
                    <p class="text-sm font-semibold text-foreground">
                      {{ step.title }}
                    </p>
                    <p class="text-sm leading-6 text-muted-foreground">
                      {{ step.description }}
                    </p>
                  </div>
                </Card>
              </div>
            </div>
          </Card>
        </section>

        <section :id="pageContent.scalingSection.id" class="section-anchor">
          <Card :bordered="false" class="case-panel">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.scalingSection.eyebrow }}</p>
                <h2 class="text-2xl font-semibold text-foreground">
                  {{ pageContent.scalingSection.title }}
                </h2>
              </div>
              <p class="section-takeaway">
                {{ pageContent.scalingSection.takeaway }}
              </p>
            </div>

            <div class="grid gap-4 md:grid-cols-2">
              <Card
                v-for="item in pageContent.scalingCards"
                :key="item.title"
                :bordered="false"
                class="case-subcard h-full"
              >
                <div class="space-y-4">
                  <h3 class="text-lg font-semibold text-foreground">
                    {{ item.title }}
                  </h3>
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
          </Card>
        </section>

        <section :id="pageContent.tradeOffsSection.id" class="section-anchor">
          <Card :bordered="false" class="case-panel">
            <div class="section-header">
              <div>
                <p class="section-kicker">{{ pageContent.tradeOffsSection.eyebrow }}</p>
                <h2 class="text-2xl font-semibold text-foreground">
                  {{ pageContent.tradeOffsSection.title }}
                </h2>
              </div>
              <p class="section-takeaway">
                {{ pageContent.tradeOffsSection.takeaway }}
              </p>
            </div>

            <div class="grid gap-4 lg:grid-cols-2">
              <Card
                v-for="item in pageContent.tradeOffs"
                :key="item.decision"
                :bordered="false"
                class="case-subcard h-full"
              >
                <div class="space-y-3">
                  <h3 class="text-base font-semibold text-foreground">
                    {{ item.decision }}
                  </h3>
                  <p class="text-sm leading-6 text-muted-foreground">
                    {{ item.reason }}
                  </p>
                </div>
              </Card>
            </div>

            <div class="mt-6 grid gap-4 lg:grid-cols-[minmax(0,1fr)_280px]">
              <Card :bordered="false" class="case-subcard">
                <div class="space-y-4">
                  <div>
                    <p class="section-kicker">{{ pageContent.footer.takeawaysKicker }}</p>
                    <h3 class="text-lg font-semibold text-foreground">
                      {{ pageContent.footer.takeawaysTitle }}
                    </h3>
                  </div>
                  <ul class="space-y-3 text-sm leading-6 text-muted-foreground">
                    <li
                      v-for="item in pageContent.footer.takeaways"
                      :key="item"
                      class="list-disc pl-2 marker:text-primary"
                    >
                      {{ item }}
                    </li>
                  </ul>
                </div>
              </Card>

              <Card :bordered="false" class="case-subcard">
                <div class="space-y-4">
                  <div>
                    <p class="section-kicker">{{ pageContent.footer.relatedKicker }}</p>
                    <h3 class="text-lg font-semibold text-foreground">
                      {{ pageContent.footer.relatedTitle }}
                    </h3>
                  </div>
                  <div class="space-y-3">
                    <div
                      v-for="item in pageContent.footer.relatedCases"
                      :key="item"
                      class="related-case"
                    >
                      <span class="text-sm font-medium text-foreground">
                        {{ item }}
                      </span>
                      <span class="text-xs text-muted-foreground">
                        {{ pageContent.footer.relatedComingSoon }}
                      </span>
                    </div>
                  </div>
                  <p class="text-xs leading-5 text-muted-foreground">
                    {{ pageContent.footer.sourceNote }}
                  </p>
                </div>
              </Card>
            </div>
          </Card>
        </section>
      </main>

      <aside class="hidden xl:block">
        <div class="sticky top-6 space-y-4">
          <Card :bordered="false" class="case-panel">
            <div class="space-y-4">
              <div class="space-y-2">
                <div class="flex items-center justify-between gap-3">
                  <span class="text-sm font-medium text-foreground">
                    {{ pageContent.navigation.progressLabel }}
                  </span>
                  <span class="text-sm text-muted-foreground">
                    {{ progressPercent }}%
                  </span>
                </div>
                <Progress :percent="progressPercent" :show-info="false" size="small" />
              </div>

              <dl class="grid gap-3">
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

          <Card :bordered="false" class="case-panel">
            <div class="space-y-4">
              <div>
                <p class="section-kicker">{{ pageContent.sidebar.tocKicker }}</p>
                <h2 class="text-lg font-semibold text-foreground">
                  {{ pageContent.sidebar.tocTitle }}
                </h2>
              </div>

              <nav
                class="space-y-2"
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
                >
                  <span class="toc-link__eyebrow">{{ section.eyebrow }}</span>
                  <span>{{ section.title }}</span>
                </a>
              </nav>
            </div>
          </Card>

          <Card :bordered="false" class="case-panel">
            <GlossaryPanel
              :active-term-id="activeGlossaryId"
              :get-toggle-aria-label="pageContent.ui.getGlossaryToggleAriaLabel"
              :kicker="pageContent.glossary.kicker"
              layout="desktop"
              :terms="pageContent.glossary.terms"
              :title="pageContent.glossary.title"
              title-class="text-lg"
              @toggle="toggleGlossary"
            />
          </Card>
        </div>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.api-card,
.case-panel,
.case-subcard,
.hero-artifact,
.target-card {
  border: 1px solid hsl(var(--border));
  background:
    linear-gradient(180deg, hsl(var(--card)) 0%, hsl(var(--background)) 100%);
  box-shadow: 0 24px 64px -56px hsl(var(--foreground) / 0.35);
}

.api-snippet {
  overflow-x: auto;
  margin-top: 1rem;
  border-radius: 1rem;
  border: 1px solid hsl(var(--border));
  background-color: hsl(var(--muted));
  padding: 1rem;
  color: hsl(var(--foreground));
  font-size: 0.8125rem;
  line-height: 1.6;
}

.artifact-chip,
.diagram-node,
.pill-chip,
.related-case,
.toc-link {
  border: 1px solid hsl(var(--border));
  background-color: hsl(var(--background));
}

.artifact-chip {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  border-radius: 1rem;
  padding: 0.875rem 1rem;
}

.artifact-chip__label {
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: hsl(var(--muted-foreground));
}

.artifact-chip__value {
  font-size: 0.95rem;
  font-weight: 600;
  color: hsl(var(--foreground));
}

.case-badge {
  border-color: hsl(var(--border));
  background-color: hsl(var(--muted));
  color: hsl(var(--muted-foreground));
  font-size: 0.75rem;
  font-weight: 600;
}

.case-kicker,
.section-kicker,
.toc-link__eyebrow {
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: hsl(var(--muted-foreground));
}

.diagram-arrow {
  text-align: center;
  font-size: 1.125rem;
  color: hsl(var(--muted-foreground));
}

.diagram-label {
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: hsl(var(--muted-foreground));
}

.diagram-node {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  border-radius: 1rem;
  padding: 1rem 1.125rem;
}

.diagram-node--accent {
  border-color: hsl(var(--primary) / 0.28);
  background:
    linear-gradient(
      135deg,
      hsl(var(--primary) / 0.12) 0%,
      hsl(var(--background)) 100%
    );
}

.diagram-title {
  color: hsl(var(--foreground));
  font-size: 1rem;
  font-weight: 600;
}
.toc-link:hover,
.toc-link:focus-visible {
  border-color: hsl(var(--primary) / 0.28);
  color: hsl(var(--primary));
}

.hero-artifact {
  position: relative;
  overflow: hidden;
}

.hero-artifact::before {
  position: absolute;
  inset: -30% auto auto 40%;
  height: 14rem;
  width: 14rem;
  border-radius: 999px;
  background: hsl(var(--primary) / 0.08);
  content: '';
  filter: blur(12px);
}

.meta-row {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  font-size: 0.875rem;
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

.method-badge {
  display: inline-flex;
  align-items: center;
  border-radius: 999px;
  border: 1px solid transparent;
  padding: 0.3rem 0.65rem;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.08em;
}

.method-badge--delete {
  border-color: hsl(var(--destructive) / 0.18);
  background-color: hsl(var(--destructive) / 0.12);
  color: hsl(var(--destructive));
}

.method-badge--get {
  border-color: hsl(var(--success) / 0.18);
  background-color: hsl(var(--success) / 0.12);
  color: hsl(var(--success));
}

.method-badge--post {
  border-color: hsl(var(--primary) / 0.18);
  background-color: hsl(var(--primary) / 0.12);
  color: hsl(var(--primary));
}

.method-badge--put {
  border-color: hsl(var(--warning) / 0.18);
  background-color: hsl(var(--warning) / 0.12);
  color: hsl(var(--warning));
}

.pill-chip {
  display: inline-flex;
  align-items: center;
  border-radius: 999px;
  padding: 0.35rem 0.75rem;
  font-size: 0.75rem;
  color: hsl(var(--muted-foreground));
}

.related-case {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-radius: 1rem;
  padding: 0.875rem 1rem;
}

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

.section-takeaway {
  max-width: 36rem;
  color: hsl(var(--muted-foreground));
  font-size: 0.95rem;
  line-height: 1.7;
}

.toc-details {
  width: 100%;
}

.toc-details__summary {
  cursor: pointer;
  list-style: none;
  font-size: 0.95rem;
  font-weight: 600;
  color: hsl(var(--foreground));
}

.toc-details__summary::-webkit-details-marker {
  display: none;
}

.toc-link {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  border-radius: 1rem;
  padding: 0.875rem 1rem;
  color: hsl(var(--foreground));
  text-decoration: none;
  transition:
    border-color 200ms ease,
    color 200ms ease,
    background-color 200ms ease;
}

.toc-link--active {
  border-color: hsl(var(--primary) / 0.28);
  background-color: hsl(var(--primary) / 0.12);
  color: hsl(var(--primary));
}

@media (min-width: 1024px) {
  .section-header {
    flex-direction: row;
    align-items: end;
    justify-content: space-between;
  }
}
</style>
