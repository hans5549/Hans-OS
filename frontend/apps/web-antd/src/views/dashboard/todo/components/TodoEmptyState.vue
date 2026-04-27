<script setup lang="ts">
import { computed } from 'vue';

import { todoIcons } from '../composables/useTodoMeta';

const props = defineProps<{
  iconPath?: string;
  title: string;
  description?: string;
}>();

const path = computed(() => props.iconPath ?? todoIcons.inbox);
</script>

<template>
  <div class="flex flex-col items-center justify-center py-20 text-center">
    <div
      class="mb-5 flex size-20 items-center justify-center rounded-full bg-muted/60 text-muted-foreground"
    >
      <svg
        class="size-9"
        fill="none"
        stroke="currentColor"
        stroke-linecap="round"
        stroke-linejoin="round"
        stroke-width="1.6"
        viewBox="0 0 24 24"
      >
        <path :d="path" />
      </svg>
    </div>
    <p class="text-base font-medium text-foreground">{{ title }}</p>
    <p
      v-if="description"
      class="mt-1.5 max-w-xs text-sm text-muted-foreground"
    >
      {{ description }}
    </p>
  </div>
</template>
