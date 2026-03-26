<script lang="ts" setup>
import { onMounted, ref } from 'vue';

import {
  Button,
  Card,
  InputNumber,
  message,
  Spin,
  Statistic,
} from 'ant-design-vue';

import type { BankInitialBalanceResponse } from '#/api';

import { getBankBalancesApi, updateBankBalanceApi } from '#/api';

const loading = ref(false);
const banks = ref<BankInitialBalanceResponse[]>([]);
const editingId = ref<null | string>(null);
const editAmount = ref(0);
const saving = ref(false);

async function fetchBanks() {
  loading.value = true;
  try {
    banks.value = await getBankBalancesApi();
  } finally {
    loading.value = false;
  }
}

function startEdit(bank: BankInitialBalanceResponse) {
  editingId.value = bank.id;
  editAmount.value = bank.initialAmount;
}

function cancelEdit() {
  editingId.value = null;
}

async function saveEdit(id: string) {
  saving.value = true;
  try {
    await updateBankBalanceApi(id, { initialAmount: editAmount.value });
    message.success('起始金額已更新');
    editingId.value = null;
    await fetchBanks();
  } finally {
    saving.value = false;
  }
}

function getBankIcon(bankName: string): string {
  return bankName.includes('上海') ? '🏦' : '🏛️';
}

onMounted(fetchBanks);
</script>

<template>
  <div>
    <div class="mb-4">
      <h3 class="text-lg font-medium">收支表起始資料</h3>
      <p class="mt-1 text-sm text-gray-500">
        設定上海銀行與合作金庫收支表的起始金額
      </p>
    </div>

    <Spin :spinning="loading">
      <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
        <Card
          v-for="bank in banks"
          :key="bank.id"
          :hoverable="editingId !== bank.id"
          class="transition-shadow duration-200"
        >
          <template #title>
            <div class="flex items-center gap-2">
              <span class="text-xl">{{ getBankIcon(bank.bankName) }}</span>
              <span>{{ bank.bankName }}</span>
            </div>
          </template>

          <template #extra>
            <template v-if="editingId === bank.id">
              <div class="flex gap-1">
                <Button
                  :loading="saving"
                  size="small"
                  type="primary"
                  @click="saveEdit(bank.id)"
                >
                  儲存
                </Button>
                <Button size="small" @click="cancelEdit"> 取消 </Button>
              </div>
            </template>
            <template v-else>
              <Button size="small" type="link" @click="startEdit(bank)">
                編輯
              </Button>
            </template>
          </template>

          <div v-if="editingId === bank.id">
            <div class="mb-2 text-sm text-gray-500">起始金額</div>
            <InputNumber
              v-model:value="editAmount"
              class="w-full"
              :formatter="
                (val: string | number | undefined) =>
                  `$ ${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')
              "
              :parser="
                (val: string | undefined) =>
                  Number(val?.replace(/\$\s?|(,*)/g, '') ?? '0')
              "
              :precision="2"
              size="large"
            />
          </div>
          <div v-else>
            <Statistic
              :precision="2"
              prefix="$"
              title="起始金額"
              :value="bank.initialAmount"
            />
          </div>
        </Card>
      </div>

      <div v-if="!loading && banks.length === 0" class="py-8 text-center">
        <p class="text-gray-400">尚無銀行起始資料</p>
      </div>
    </Spin>
  </div>
</template>
