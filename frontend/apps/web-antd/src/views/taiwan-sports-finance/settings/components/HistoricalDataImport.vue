<script lang="ts" setup>
import { ref } from 'vue';

import {
  Alert,
  Button,
  Card,
  message,
  Modal,
  Result,
  Spin,
  Statistic,
} from 'ant-design-vue';

import type { ImportResultResponse } from '#/api';

import { importBankTransactionsApi } from '#/api';

const importing = ref(false);
const importResult = ref<ImportResultResponse | null>(null);

function confirmImport() {
  Modal.confirm({
    title: '確認匯入歷史資料',
    content:
      '此操作將清除所有現有的銀行交易記錄和起始餘額，並從 Excel 檔案重新匯入。此操作無法復原，確定要繼續嗎？',
    okText: '確認匯入',
    okType: 'danger',
    cancelText: '取消',
    async onOk() {
      await executeImport();
    },
  });
}

async function executeImport() {
  importing.value = true;
  importResult.value = null;
  try {
    importResult.value = await importBankTransactionsApi();
    message.success(
      `匯入完成，共匯入 ${importResult.value.totalTransactions} 筆交易`,
    );
  } catch {
    message.error('匯入失敗，請查看後端日誌');
  } finally {
    importing.value = false;
  }
}

function getBankIcon(bankName: string): string {
  return bankName.includes('上海') ? '🏦' : '🏛️';
}
</script>

<template>
  <div>
    <div class="mb-4">
      <h3 class="text-lg font-medium">歷史資料匯入</h3>
      <p class="mt-1 text-sm text-gray-500">
        從內建的 Excel 檔案匯入上海銀行與合作金庫的歷史收支資料
      </p>
    </div>

    <Alert
      class="mb-4"
      message="注意事項"
      show-icon
      type="warning"
    >
      <template #description>
        <ul class="list-disc pl-4">
          <li>匯入將<strong>清除所有現有交易記錄和起始餘額</strong>，以 Excel 檔案內容全量替換</li>
          <li>歷史銀行資料統一歸類：彰化銀行 → 上海銀行、第一銀行 → 合作金庫</li>
          <li>手續費會自動合併至對應交易的手續費欄位</li>
          <li>民國年自動轉換為西元年</li>
        </ul>
      </template>
    </Alert>

    <div class="mb-6">
      <Button
        :loading="importing"
        size="large"
        type="primary"
        danger
        @click="confirmImport"
      >
        {{ importing ? '匯入中...' : '初始化歷史資料' }}
      </Button>
    </div>

    <Spin :spinning="importing" tip="正在匯入歷史資料，請稍候...">
      <div v-if="importResult" class="mt-4">
        <Result status="success" title="匯入完成">
          <template #subTitle>
            共匯入 {{ importResult.totalTransactions }} 筆交易記錄
          </template>

          <template #extra>
            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <Card v-for="bank in importResult.banks" :key="bank.bankName">
                <template #title>
                  <div class="flex items-center gap-2">
                    <span class="text-xl">{{ getBankIcon(bank.bankName) }}</span>
                    <span>{{ bank.bankName }}</span>
                  </div>
                </template>
                <div class="grid grid-cols-2 gap-4">
                  <Statistic
                    title="匯入筆數"
                    :value="bank.transactionCount"
                    suffix="筆"
                  />
                  <Statistic
                    title="起始餘額"
                    :value="bank.initialBalance"
                    :precision="0"
                    prefix="$"
                  />
                </div>
              </Card>
            </div>
          </template>
        </Result>
      </div>
    </Spin>
  </div>
</template>
