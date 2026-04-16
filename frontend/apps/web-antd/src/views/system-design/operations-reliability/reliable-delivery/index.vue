<script setup lang="ts">
import SystemDesignDocPage from '../../_shared/SystemDesignDocPage.vue';
import { content } from './content';

defineOptions({ name: 'ReliableDeliveryPage' });
</script>

<template>
  <SystemDesignDocPage
    :content="content"
    category="維運與可靠性"
    category-key="operations-reliability"
  />
</template>
