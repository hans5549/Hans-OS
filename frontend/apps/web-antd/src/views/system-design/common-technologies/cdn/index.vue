<script setup lang="ts">
import SystemDesignDocPage from '../../_shared/SystemDesignDocPage.vue';
import { content } from './content';

defineOptions({ name: 'CdnPage' });
</script>

<template>
  <SystemDesignDocPage
    :content="content"
    category="常用技術"
    category-key="common-technologies"
  />
</template>
