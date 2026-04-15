<script setup lang="ts">
import { ref } from 'vue';

import { Page } from '@vben/common-ui';

import { Tabs, TabPane } from 'ant-design-vue';

import DocumentTab from './DocumentTab.vue';
import PracticeTab from './PracticeTab.vue';

defineOptions({ name: 'QrCodeGeneratorPage' });

const activeTab = ref('document');
</script>

<template>
  <Page auto-content-height content-class="p-0">
    <Tabs
      v-model:activeKey="activeTab"
      class="qr-tabs"
      size="large"
    >
      <TabPane key="document" tab="文件說明">
        <DocumentTab />
      </TabPane>
      <TabPane key="practice" tab="實作練習">
        <PracticeTab />
      </TabPane>
    </Tabs>
  </Page>
</template>

<style scoped>
.qr-tabs {
  padding: 0 1rem;
}

.qr-tabs :deep(.ant-tabs-nav) {
  margin-bottom: 0;
}
</style>
