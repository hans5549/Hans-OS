<script lang="ts" setup>
import { ref } from 'vue';

import { Tabs, TabPane } from 'ant-design-vue';

import TsfGlassPage from '../_shared/TsfGlassPage.vue';

import BankInitialBalance from './components/BankInitialBalance.vue';
import DepartmentManagement from './components/DepartmentManagement.vue';
import HistoricalDataImport from './components/HistoricalDataImport.vue';

defineOptions({ name: 'TsfSettingsPage' });

const activeTab = ref('departments');
</script>

<template>
  <TsfGlassPage
    icon="i-lucide-settings"
    subtitle="管理體育部門、銀行起始資料與歷史交易初始化。"
    title="設定"
  >
    <section class="tsf-panel">
      <Tabs v-model:activeKey="activeTab" tab-position="left">
        <TabPane key="departments" tab="體育部門管理">
          <div class="pl-4">
            <DepartmentManagement />
          </div>
        </TabPane>
        <TabPane key="bank-balance" tab="收支表起始資料">
          <div class="pl-4">
            <BankInitialBalance />
          </div>
        </TabPane>
        <TabPane key="import" tab="歷史資料匯入">
          <div class="pl-4">
            <HistoricalDataImport />
          </div>
        </TabPane>
      </Tabs>
    </section>
  </TsfGlassPage>
</template>
