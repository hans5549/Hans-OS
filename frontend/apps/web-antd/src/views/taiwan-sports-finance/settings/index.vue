<script lang="ts" setup>
import { ref } from 'vue';

import { Page } from '@vben/common-ui';

import { Card, Tabs, TabPane } from 'ant-design-vue';

import BankInitialBalance from './components/BankInitialBalance.vue';
import DepartmentManagement from './components/DepartmentManagement.vue';

defineOptions({ name: 'TsfSettingsPage' });

const activeTab = ref('departments');
</script>

<template>
  <Page content-class="p-0" title="設定">
    <Card :body-style="{ padding: '16px 24px' }">
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
      </Tabs>
    </Card>
  </Page>
</template>
