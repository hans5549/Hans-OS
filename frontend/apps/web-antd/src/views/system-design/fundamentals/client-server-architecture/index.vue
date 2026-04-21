<script setup lang="ts">
import SystemDesignDocPage from '../../_shared/SystemDesignDocPage.vue';
import { content } from './content';

defineOptions({ name: 'ClientServerArchitecturePage' });
</script>

<template>
  <SystemDesignDocPage
    :content="content"
    category="基本觀念"
    category-key="fundamentals"
  />
</template>
