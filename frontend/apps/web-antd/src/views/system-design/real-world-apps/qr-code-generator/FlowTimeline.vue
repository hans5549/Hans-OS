<script setup lang="ts">
defineOptions({ name: 'FlowTimeline' });

defineProps<{
  steps: Array<{ description: string; title: string }>;
}>();
</script>

<template>
  <div class="timeline">
    <div
      v-for="(step, index) in steps"
      :key="step.title"
      class="timeline-step"
    >
      <div class="timeline-indicator">
        <span class="timeline-dot">{{ index + 1 }}</span>
        <div v-if="index < steps.length - 1" class="timeline-line" aria-hidden="true" />
      </div>
      <div class="timeline-content">
        <p class="text-sm font-semibold text-foreground">{{ step.title }}</p>
        <p class="mt-1 text-sm leading-6 text-muted-foreground">{{ step.description }}</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.timeline {
  display: flex;
  flex-direction: column;
}

.timeline-step {
  display: flex;
  gap: 1rem;
  min-height: 4rem;
}

.timeline-step:last-child {
  min-height: auto;
}

.timeline-indicator {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex-shrink: 0;
  width: 2rem;
}

.timeline-dot {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 1.75rem;
  height: 1.75rem;
  border-radius: 999px;
  background-color: hsl(var(--primary) / 0.12);
  color: hsl(var(--primary));
  font-size: 0.75rem;
  font-weight: 700;
}

.timeline-line {
  flex: 1;
  width: 1px;
  margin: 0.375rem 0;
  background: linear-gradient(
    180deg,
    hsl(var(--primary) / 0.2) 0%,
    hsl(var(--border)) 100%
  );
}

.timeline-content {
  flex: 1;
  padding-bottom: 1.25rem;
}

.timeline-step:last-child .timeline-content {
  padding-bottom: 0;
}
</style>
