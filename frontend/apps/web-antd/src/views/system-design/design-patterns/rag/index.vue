<script setup lang="ts">
import SystemDesignDocPage from '../../_shared/SystemDesignDocPage.vue';
import { content } from './content';

defineOptions({ name: 'RagPage' });
</script>

<template>
  <SystemDesignDocPage
    :content="content"
    category="設計模式"
    category-key="design-patterns"
  />
</template>
