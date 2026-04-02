<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';

import { Page } from '@vben/common-ui';
import {
  Button,
  Card,
  DatePicker,
  Drawer,
  Form,
  FormItem,
  Input,
  InputNumber,
  message,
  Modal,
  Select,
  SelectOption,
  Spin,
  Tabs,
  TabPane,
} from 'ant-design-vue';
import dayjs from 'dayjs';

import type {
  AccountResponse,
  CategoryResponse,
  CreateTransactionRequest,
  DailyTransactionGroup,
  MonthlySummaryResponse,
  TransactionResponse,
} from '#/api';

import {
  createTransactionApi,
  deleteTransactionApi,
  getAccountsApi,
  getCategoriesApi,
  getMonthlySummaryApi,
  getTransactionsApi,
  updateTransactionApi,
} from '#/api';

defineOptions({ name: 'FinanceTransactionsPage' });

// ── State ────────────────────────────────────────

const currentMonth = ref(dayjs());
const loading = ref(false);
const summary = ref<MonthlySummaryResponse | null>(null);
const dailyGroups = ref<DailyTransactionGroup[]>([]);

// Form / Drawer
const drawerOpen = ref(false);
const saving = ref(false);
const editingId = ref<string | null>(null);
const formType = ref<'Expense' | 'Income' | 'Transfer'>('Expense');
const formAmount = ref<number | undefined>(undefined);
const formCategoryId = ref<string | undefined>(undefined);
const formAccountId = ref<string | undefined>(undefined);
const formToAccountId = ref<string | undefined>(undefined);
const formDate = ref(dayjs());
const formNote = ref('');

// Lookup data
const accounts = ref<AccountResponse[]>([]);
const categories = ref<CategoryResponse[]>([]);

// ── Computed ──────────────────────────────────────

const displayYear = computed(() => currentMonth.value.year());
const displayMonth = computed(() => currentMonth.value.month() + 1);

const filteredCategories = computed(() =>
  categories.value.filter((c) => {
    if (formType.value === 'Expense') return c.categoryType === 'Expense';
    if (formType.value === 'Income') return c.categoryType === 'Income';
    return false;
  }),
);

const flatCategories = computed(() => {
  const result: CategoryResponse[] = [];
  function flatten(items: CategoryResponse[]) {
    for (const item of items) {
      result.push(item);
      if (item.children?.length) {
        flatten(item.children);
      }
    }
  }
  flatten(filteredCategories.value);
  return result;
});

const isTransfer = computed(() => formType.value === 'Transfer');
const drawerTitle = computed(() => (editingId.value ? '編輯交易' : '新增交易'));

// ── Helpers ──────────────────────────────────────

function formatAmount(amount: number): string {
  return amount.toLocaleString('zh-TW');
}

function formatDateHeader(dateStr: string): string {
  const d = dayjs(dateStr);
  const weekdays = [
    '星期日',
    '星期一',
    '星期二',
    '星期三',
    '星期四',
    '星期五',
    '星期六',
  ];
  return `${d.month() + 1}月${d.date()}日 ${weekdays[d.day()]}`;
}

function amountColor(type: string): string {
  if (type === 'Income') return 'text-green-600';
  if (type === 'Expense') return 'text-red-500';
  return 'text-blue-500';
}

function amountPrefix(type: string): string {
  if (type === 'Income') return '+';
  if (type === 'Expense') return '-';
  return '';
}

function transactionSubtext(tx: TransactionResponse): string {
  if (tx.transactionType === 'Transfer' && tx.toAccountName) {
    return `${tx.accountName} → ${tx.toAccountName}`;
  }
  return tx.accountName;
}

// ── API Operations ───────────────────────────────

async function fetchData() {
  loading.value = true;
  try {
    const [summaryData, groups] = await Promise.all([
      getMonthlySummaryApi(displayYear.value, displayMonth.value),
      getTransactionsApi(displayYear.value, displayMonth.value),
    ]);
    summary.value = summaryData;
    dailyGroups.value = groups;
  } finally {
    loading.value = false;
  }
}

async function loadLookups() {
  const [accts, cats] = await Promise.all([
    getAccountsApi(),
    getCategoriesApi(),
  ]);
  accounts.value = accts;
  categories.value = cats;
}

// ── Month Navigation ─────────────────────────────

function prevMonth() {
  currentMonth.value = currentMonth.value.subtract(1, 'month');
}

function nextMonth() {
  currentMonth.value = currentMonth.value.add(1, 'month');
}

function onMonthChange(value: dayjs.Dayjs | string) {
  currentMonth.value = dayjs(value);
}

// ── Drawer / Form ────────────────────────────────

function resetForm() {
  editingId.value = null;
  formType.value = 'Expense';
  formAmount.value = undefined;
  formCategoryId.value = undefined;
  formAccountId.value = undefined;
  formToAccountId.value = undefined;
  formDate.value = dayjs();
  formNote.value = '';
}

function openAddDrawer() {
  resetForm();
  drawerOpen.value = true;
}

function openEditDrawer(tx: TransactionResponse) {
  editingId.value = tx.id;
  formType.value = tx.transactionType as 'Expense' | 'Income' | 'Transfer';
  formAmount.value = tx.amount;
  formCategoryId.value = tx.categoryId ?? undefined;
  formAccountId.value = tx.accountId;
  formToAccountId.value = tx.toAccountId ?? undefined;
  formDate.value = dayjs(tx.transactionDate);
  formNote.value = tx.note ?? '';
  drawerOpen.value = true;
}

function closeDrawer() {
  drawerOpen.value = false;
}

function onTypeChange(key: string) {
  formType.value = key as 'Expense' | 'Income' | 'Transfer';
  formCategoryId.value = undefined;
  formToAccountId.value = undefined;
}

async function handleSave() {
  if (!formAmount.value || formAmount.value <= 0) {
    message.warning('請輸入有效金額');
    return;
  }
  if (!formAccountId.value) {
    message.warning('請選擇帳戶');
    return;
  }
  if (!isTransfer.value && !formCategoryId.value) {
    message.warning('請選擇分類');
    return;
  }
  if (isTransfer.value && !formToAccountId.value) {
    message.warning('請選擇轉入帳戶');
    return;
  }
  if (isTransfer.value && formAccountId.value === formToAccountId.value) {
    message.warning('轉出與轉入帳戶不可相同');
    return;
  }

  const payload: CreateTransactionRequest = {
    transactionType: formType.value,
    amount: formAmount.value,
    transactionDate: formDate.value.format('YYYY-MM-DD'),
    categoryId: isTransfer.value ? undefined : formCategoryId.value,
    accountId: formAccountId.value,
    toAccountId: isTransfer.value ? formToAccountId.value : undefined,
    note: formNote.value.trim() || undefined,
  };

  saving.value = true;
  try {
    if (editingId.value) {
      await updateTransactionApi(editingId.value, payload);
      message.success('交易已更新');
    } else {
      await createTransactionApi(payload);
      message.success('交易已新增');
    }
    closeDrawer();
    await fetchData();
  } catch {
    message.error('儲存失敗，請稍後再試');
  } finally {
    saving.value = false;
  }
}

function confirmDelete(tx: TransactionResponse) {
  Modal.confirm({
    title: '確認刪除',
    content: `確定要刪除此筆${tx.transactionType === 'Income' ? '收入' : tx.transactionType === 'Expense' ? '支出' : '轉帳'}紀錄嗎？`,
    okText: '刪除',
    okType: 'danger',
    cancelText: '取消',
    async onOk() {
      try {
        await deleteTransactionApi(tx.id);
        message.success('交易已刪除');
        await fetchData();
      } catch {
        message.error('刪除失敗');
      }
    },
  });
}

// ── Watchers & Lifecycle ─────────────────────────

watch(currentMonth, fetchData);

onMounted(async () => {
  await Promise.all([fetchData(), loadLookups()]);
});
</script>

<template>
  <Page auto-content-height>
    <!-- ── Month Selector + Summary ─────────────── -->
    <Card class="mb-4">
      <div class="mb-3 flex items-center justify-center gap-3">
        <Button shape="circle" size="small" @click="prevMonth">
          <template #icon>
            <span class="i-lucide-chevron-left" />
          </template>
        </Button>
        <DatePicker
          :allow-clear="false"
          :value="currentMonth"
          picker="month"
          style="width: 160px"
          @change="onMonthChange"
        />
        <Button shape="circle" size="small" @click="nextMonth">
          <template #icon>
            <span class="i-lucide-chevron-right" />
          </template>
        </Button>
      </div>

      <div v-if="summary" class="flex justify-around text-center">
        <div>
          <div class="text-sm text-gray-500">收入</div>
          <div class="text-lg font-semibold text-green-600">
            {{ formatAmount(summary.totalIncome) }}
          </div>
        </div>
        <div>
          <div class="text-sm text-gray-500">支出</div>
          <div class="text-lg font-semibold text-red-500">
            {{ formatAmount(summary.totalExpense) }}
          </div>
        </div>
        <div>
          <div class="text-sm text-gray-500">結餘</div>
          <div
            :class="[
              'text-lg font-semibold',
              summary.balance >= 0 ? 'text-green-600' : 'text-red-500',
            ]"
          >
            {{ formatAmount(summary.balance) }}
          </div>
        </div>
      </div>
    </Card>

    <!-- ── Daily Transaction Groups ─────────────── -->
    <Spin :spinning="loading">
      <div v-if="dailyGroups.length > 0" class="flex flex-col gap-3">
        <Card
          v-for="group in dailyGroups"
          :key="group.date"
          :body-style="{ padding: '0' }"
          size="small"
        >
          <!-- Date Header -->
          <template #title>
            <div class="flex items-center justify-between text-sm">
              <span class="font-medium">
                {{ formatDateHeader(group.date) }}
              </span>
              <span class="flex gap-4 text-xs">
                <span v-if="group.dayIncome > 0" class="text-green-600">
                  +{{ formatAmount(group.dayIncome) }}
                </span>
                <span v-if="group.dayExpense > 0" class="text-red-500">
                  -{{ formatAmount(group.dayExpense) }}
                </span>
              </span>
            </div>
          </template>

          <!-- Transaction Items -->
          <div class="divide-y">
            <div
              v-for="tx in group.transactions"
              :key="tx.id"
              class="flex cursor-pointer items-center px-4 py-3 transition-colors hover:bg-gray-50 dark:hover:bg-gray-800"
              @click="openEditDrawer(tx)"
            >
              <!-- Icon + Category -->
              <div class="flex min-w-0 flex-1 items-center gap-3">
                <span
                  v-if="tx.categoryIcon"
                  :class="tx.categoryIcon"
                  class="text-lg text-gray-500"
                />
                <span
                  v-else
                  class="i-lucide-arrow-left-right text-lg text-gray-400"
                />
                <div class="min-w-0 flex-1">
                  <div class="truncate text-sm font-medium">
                    {{
                      tx.transactionType === 'Transfer'
                        ? '轉帳'
                        : (tx.categoryName ?? '未分類')
                    }}
                  </div>
                  <div class="truncate text-xs text-gray-400">
                    {{ transactionSubtext(tx) }}
                    <template v-if="tx.note">
                      · {{ tx.note }}
                    </template>
                  </div>
                </div>
              </div>

              <!-- Amount + Delete -->
              <div class="flex items-center gap-2">
                <span
                  :class="amountColor(tx.transactionType)"
                  class="whitespace-nowrap text-sm font-semibold"
                >
                  {{ amountPrefix(tx.transactionType)
                  }}{{ formatAmount(tx.amount) }}
                </span>
                <Button
                  danger
                  shape="circle"
                  size="small"
                  type="text"
                  @click.stop="confirmDelete(tx)"
                >
                  <template #icon>
                    <span class="i-lucide-trash-2 text-xs" />
                  </template>
                </Button>
              </div>
            </div>
          </div>
        </Card>
      </div>

      <div
        v-else-if="!loading"
        class="py-16 text-center text-gray-400"
      >
        本月尚無交易紀錄
      </div>
    </Spin>

    <!-- ── FAB: Add Transaction ─────────────────── -->
    <Button
      class="!fixed bottom-8 right-8 !h-14 !w-14 shadow-lg"
      shape="circle"
      size="large"
      type="primary"
      @click="openAddDrawer"
    >
      <template #icon>
        <span class="i-lucide-plus text-2xl" />
      </template>
    </Button>

    <!-- ── Transaction Form Drawer ──────────────── -->
    <Drawer
      :destroy-on-close="false"
      :open="drawerOpen"
      placement="right"
      :title="drawerTitle"
      :width="420"
      @close="closeDrawer"
    >
      <Form layout="vertical">
        <!-- Transaction Type Tabs -->
        <Tabs
          :active-key="formType"
          class="mb-2"
          @change="(k: string | number) => onTypeChange(String(k))"
        >
          <TabPane key="Expense" tab="支出" />
          <TabPane key="Income" tab="收入" />
          <TabPane key="Transfer" tab="轉帳" />
        </Tabs>

        <!-- Amount -->
        <FormItem label="金額" required>
          <InputNumber
            v-model:value="formAmount"
            :min="0.01"
            placeholder="請輸入金額"
            :precision="0"
            :step="100"
            style="width: 100%; font-size: 24px"
          />
        </FormItem>

        <!-- Category (not for Transfer) -->
        <FormItem v-if="!isTransfer" label="分類" required>
          <Select
            v-model:value="formCategoryId"
            :options="
              flatCategories.map((c) => ({
                label: c.name,
                value: c.id,
              }))
            "
            placeholder="請選擇分類"
            show-search
            :filter-option="
              (input: string, option: Record<string, unknown> | undefined) =>
                (String(option?.label ?? '')).includes(input)
            "
          />
        </FormItem>

        <!-- Account -->
        <FormItem label="帳戶" required>
          <Select
            v-model:value="formAccountId"
            placeholder="請選擇帳戶"
          >
            <SelectOption
              v-for="acct in accounts"
              :key="acct.id"
              :value="acct.id"
            >
              {{ acct.name }}
            </SelectOption>
          </Select>
        </FormItem>

        <!-- To Account (Transfer only) -->
        <FormItem v-if="isTransfer" label="轉入帳戶" required>
          <Select
            v-model:value="formToAccountId"
            placeholder="請選擇轉入帳戶"
          >
            <SelectOption
              v-for="acct in accounts"
              :key="acct.id"
              :disabled="acct.id === formAccountId"
              :value="acct.id"
            >
              {{ acct.name }}
            </SelectOption>
          </Select>
        </FormItem>

        <!-- Date -->
        <FormItem label="日期">
          <DatePicker
            v-model:value="formDate"
            :allow-clear="false"
            style="width: 100%"
          />
        </FormItem>

        <!-- Note -->
        <FormItem label="備註">
          <Input
            v-model:value="formNote"
            :maxlength="200"
            placeholder="選填"
          />
        </FormItem>
      </Form>

      <template #footer>
        <div class="flex justify-end gap-2">
          <Button @click="closeDrawer">取消</Button>
          <Button :loading="saving" type="primary" @click="handleSave">
            儲存
          </Button>
        </div>
      </template>
    </Drawer>
  </Page>
</template>
