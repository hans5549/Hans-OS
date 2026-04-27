<script setup lang="ts">
type MetricTone = 'danger' | 'info' | 'neutral' | 'success' | 'warning';

withDefaults(
  defineProps<{
    icon?: string;
    label: string;
    prefix?: string;
    suffix?: string;
    tone?: MetricTone;
    value: number | string;
  }>(),
  {
    icon: '',
    prefix: '',
    suffix: '',
    tone: 'neutral',
  },
);
</script>

<template>
  <article class="tsf-metric-card" :class="`tsf-metric-card--${tone}`">
    <div>
      <div class="tsf-metric-card__label">{{ label }}</div>
      <div class="tsf-metric-card__value">
        <span v-if="prefix" class="tsf-metric-card__prefix">{{ prefix }}</span>
        <span>{{ value }}</span>
        <span v-if="suffix" class="tsf-metric-card__suffix">{{ suffix }}</span>
      </div>
    </div>
    <span
      v-if="icon"
      class="tsf-metric-card__icon"
      :class="icon"
      aria-hidden="true"
    />
  </article>
</template>
