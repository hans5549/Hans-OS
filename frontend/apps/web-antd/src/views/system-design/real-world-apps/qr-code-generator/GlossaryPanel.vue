<script setup lang="ts">
import type { GlossaryTerm } from './content';

defineOptions({ name: 'GlossaryPanel' });

const props = defineProps<{
  activeTermId: string;
  getToggleAriaLabel: (label: string, expanded: boolean) => string;
  kicker: string;
  layout: 'desktop' | 'mobile';
  terms: GlossaryTerm[];
  title: string;
  titleClass: string;
}>();

const emit = defineEmits<{
  toggle: [termId: string];
}>();

const glossaryPanelId = (termId: string) =>
  `glossary-panel-${props.layout}-${termId}`;

const isExpanded = (termId: string) => props.activeTermId === termId;

function toggleGlossary(termId: string) {
  emit('toggle', termId);
}
</script>

<template>
  <div class="space-y-4">
    <div>
      <p class="text-xs font-semibold uppercase tracking-[0.16em] text-muted-foreground">
        {{ kicker }}
      </p>
      <h2 :class="['font-semibold text-foreground', titleClass]">
        {{ title }}
      </h2>
    </div>

    <div class="space-y-2">
      <div
        v-for="term in terms"
        :key="term.id"
        class="glossary-item"
      >
        <button
          :aria-controls="glossaryPanelId(term.id)"
          :aria-expanded="isExpanded(term.id)"
          :aria-label="getToggleAriaLabel(term.label, isExpanded(term.id))"
          :class="[
            'glossary-chip',
            isExpanded(term.id) ? 'glossary-chip--expanded' : '',
          ]"
          type="button"
          @click="toggleGlossary(term.id)"
        >
          <span>{{ term.label }}</span>
          <span>{{ isExpanded(term.id) ? '−' : '+' }}</span>
        </button>
        <p
          v-show="isExpanded(term.id)"
          :id="glossaryPanelId(term.id)"
          :aria-hidden="!isExpanded(term.id)"
          class="glossary-detail"
        >
          {{ term.description }}
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.glossary-item {
  border-radius: 1rem;
}

.glossary-chip {
  cursor: pointer;
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: space-between;
  border: 1px solid hsl(var(--border));
  border-radius: 999px;
  background-color: hsl(var(--background));
  padding: 0.5rem 0.875rem;
  color: hsl(var(--foreground));
  font-size: 0.8125rem;
  font-weight: 500;
  transition:
    border-color 200ms ease,
    color 200ms ease,
    background-color 200ms ease;
}

.glossary-chip--expanded {
  border-color: hsl(var(--primary) / 0.28);
  background-color: hsl(var(--primary) / 0.1);
  color: hsl(var(--primary));
}

.glossary-chip:hover,
.glossary-chip:focus-visible {
  border-color: hsl(var(--primary) / 0.28);
  color: hsl(var(--primary));
}

.glossary-detail {
  margin-top: 0.5rem;
  border-left: 2px solid hsl(var(--primary) / 0.28);
  padding-left: 0.875rem;
  color: hsl(var(--muted-foreground));
  font-size: 0.8125rem;
  line-height: 1.6;
}
</style>
