<script setup lang="ts">
defineProps<{
  iconPath: string;
  title: string;
  description: string;
}>();
</script>

<template>
  <div class="flex flex-col items-center justify-center py-16 text-center">
    <div class="mb-4 opacity-30">
      <svg class="mx-auto size-16 text-foreground" fill="none" stroke="currentColor" stroke-width="1" viewBox="0 0 24 24">
        <path :d="iconPath" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </div>
    <p class="text-base font-medium text-foreground">{{ title }}</p>
    <p class="mt-1 text-sm text-muted-foreground">{{ description }}</p>
  </div>
</template>
