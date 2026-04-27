<script setup lang="ts">
import { Page } from '@vben/common-ui';

import './tsf-glass.css';

withDefaults(
  defineProps<{
    icon?: string;
    subtitle?: string;
    title: string;
  }>(),
  {
    icon: 'i-lucide-landmark',
    subtitle: '',
  },
);
</script>

<template>
  <Page content-class="p-0" :title="title">
    <div class="tsf-glass-page">
      <div class="tsf-glass-shell">
        <header class="tsf-glass-hero">
          <div class="tsf-glass-title-group">
            <span class="tsf-glass-icon" :class="icon" aria-hidden="true" />
            <div>
              <h2 class="tsf-glass-title">{{ title }}</h2>
              <p v-if="subtitle" class="tsf-glass-subtitle">
                {{ subtitle }}
              </p>
            </div>
          </div>
          <div v-if="$slots.actions" class="tsf-glass-actions">
            <slot name="actions" />
          </div>
        </header>

        <section v-if="$slots.filters" class="tsf-toolbar">
          <slot name="filters" />
        </section>

        <slot />
      </div>
    </div>
  </Page>
</template>
