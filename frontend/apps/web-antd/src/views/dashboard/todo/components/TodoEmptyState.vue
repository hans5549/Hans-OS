<script setup lang="ts">
defineProps<{
  icon: string;
  title: string;
  description: string;
}>();
</script>

<template>
  <div class="flex flex-col items-center justify-center py-16 text-center">
    <div class="mb-4 text-5xl opacity-40">{{ icon }}</div>
    <p class="text-base font-medium text-slate-600">{{ title }}</p>
    <p class="mt-1 text-sm text-slate-400">{{ description }}</p>
  </div>
</template>
