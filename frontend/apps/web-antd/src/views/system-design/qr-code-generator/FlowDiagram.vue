<script setup lang="ts">
defineOptions({ name: 'FlowDiagram' });

defineProps<{
  chips: Array<{ label: string; value: string }>;
  description: string;
  kicker: string;
  nodes: Array<{ label: string; title: string }>;
  title: string;
}>();
</script>

<template>
  <div class="flow-diagram">
    <div class="mb-4 space-y-1">
      <p class="text-xs font-semibold uppercase tracking-[0.16em] text-muted-foreground">
        {{ kicker }}
      </p>
      <h2 class="text-lg font-bold text-foreground">{{ title }}</h2>
      <p class="text-sm leading-6 text-muted-foreground">{{ description }}</p>
    </div>

    <div class="flow-nodes">
      <template v-for="(node, index) in nodes" :key="node.title">
        <div :class="['flow-node', index === 0 ? 'flow-node--accent' : '']">
          <span class="flow-node__label">{{ node.label }}</span>
          <strong class="flow-node__title">{{ node.title }}</strong>
        </div>
        <div
          v-if="index < nodes.length - 1"
          class="flow-arrow"
          aria-hidden="true"
        >
          <svg class="flow-arrow__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M5 12h14M13 5l7 7-7 7" />
          </svg>
        </div>
      </template>
    </div>

    <div class="mt-4 flex flex-wrap gap-2">
      <div v-for="chip in chips" :key="chip.label" class="flow-chip">
        <span class="flow-chip__label">{{ chip.label }}</span>
        <span class="flow-chip__value">{{ chip.value }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.flow-diagram {
  width: 100%;
}

.flow-nodes {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 0.5rem;
}

@media (min-width: 640px) {
  .flow-nodes {
    flex-direction: row;
    align-items: center;
  }
}

.flow-node {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: 0.25rem;
  border: 1px solid hsl(var(--border));
  border-radius: 0.75rem;
  background-color: hsl(var(--muted) / 0.5);
  padding: 0.875rem 1rem;
  transition: border-color 200ms ease;
}

.flow-node--accent {
  border-color: hsl(var(--primary) / 0.3);
  background: linear-gradient(
    135deg,
    hsl(var(--primary) / 0.08) 0%,
    hsl(var(--muted) / 0.5) 100%
  );
}

.flow-node__label {
  font-size: 0.6875rem;
  font-weight: 600;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: hsl(var(--muted-foreground));
}

.flow-node__title {
  font-size: 0.9375rem;
  font-weight: 600;
  color: hsl(var(--foreground));
}

.flow-arrow {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: hsl(var(--muted-foreground));
}

.flow-arrow__icon {
  width: 1.25rem;
  height: 1.25rem;
}

@media (max-width: 639px) {
  .flow-arrow__icon {
    transform: rotate(90deg);
  }
}

.flow-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  border: 1px solid hsl(var(--border));
  border-radius: 999px;
  background-color: hsl(var(--muted) / 0.4);
  padding: 0.375rem 0.75rem;
}

.flow-chip__label {
  font-size: 0.6875rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: hsl(var(--muted-foreground));
}

.flow-chip__value {
  font-size: 0.8125rem;
  font-weight: 600;
  color: hsl(var(--foreground));
}
</style>
