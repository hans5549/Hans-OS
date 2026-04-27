<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';

import {
  Button,
  Checkbox,
  Col,
  DatePicker,
  Form,
  FormItem,
  Input,
  InputNumber,
  message,
  Modal,
  Popconfirm,
  Radio,
  RadioGroup,
  Row,
  Segmented,
  Select,
  SelectOption,
  Spin,
  Table,
  Tag,
  Tooltip,
} from 'ant-design-vue';
import dayjs from 'dayjs';
import customParseFormat from 'dayjs/plugin/customParseFormat';

import type {
  ActivityDetailResponse,
  ActivitySummaryResponse,
  BudgetItemResponse,
  BankTransactionResponse,
  BankTransactionSummaryResponse,
  CreateBankTransactionRequest,
  DepartmentResponse,
} from '#/api';

import {
  batchUpdateDepartmentApi,
  createActivityApi,
  createBankTransactionApi,
  deleteActivityApi,
  deleteBankTransactionApi,
  exportBankTransactionsApi,
  getActivitiesApi,
  getBankTransactionsApi,
  getBankTransactionSummaryApi,
  getDepartmentBudgetItemsApi,
  getDepartmentsApi,
  updateBankTransactionApi,
} from '#/api';

import ActivityFormDrawer from '../activities/components/ActivityFormDrawer.vue';
import TsfGlassPage from './TsfGlassPage.vue';
import TsfMetricCard from './TsfMetricCard.vue';

type ActivityMode = 'existing' | 'new' | 'none';

dayjs.extend(customParseFormat);

const props = defineProps<{
  bankName: string;
}>();

// ── 時間篩選 ────────────────────────────────────

const currentYear = ref(dayjs().year());
const currentMonth = ref<number | undefined>(dayjs().month() + 1);
function parseTransactionDate(value: string) {
  const parsed = dayjs(value, 'YYYY-MM-DD', true);
  return parsed.isValid() ? parsed : null;
}

function getDefaultTransactionDate() {
  const today = dayjs();
  const targetMonth = currentMonth.value ?? (currentYear.value === today.year() ? today.month() + 1 : 1);
  const targetDay = currentYear.value === today.year() && targetMonth === today.month() + 1
    ? today.date()
    : 1;

  return dayjs(new Date(currentYear.value, targetMonth - 1, targetDay)).format('YYYY-MM-DD');
}

const defaultFormState = (): CreateBankTransactionRequest => ({
  transactionType: 0,
  transactionDate: getDefaultTransactionDate(),
  description: '',
  amount: 0,
  fee: 0,
  receiptCollected: false,
  receiptMailed: false,
});

const yearOptions = computed(() => {
  const startYear = 2019;
  const endYear = dayjs().year() + 1;
  return Array.from(
    { length: endYear - startYear + 1 },
    (_, index) => startYear + index,
  );
});

const monthOptions = computed(() => {
  const options: Array<{ label: string; value: number | undefined }> = [
    { label: '全年', value: undefined },
  ];
  for (let m = 1; m <= 12; m++) {
    options.push({ label: `${m}月`, value: m });
  }
  return options;
});

const monthSegmentedValue = computed({
  get: () => (currentMonth.value === undefined ? '全年' : `${currentMonth.value}月`),
  set: (val: string) => {
    currentMonth.value = val === '全年' ? undefined : Number.parseInt(val, 10);
  },
});

const segmentedOptions = computed(() =>
  monthOptions.value.map((option) => option.label),
);

// ── 資料 ────────────────────────────────────────

const loading = ref(false);
const transactions = ref<BankTransactionResponse[]>([]);
const summary = ref<BankTransactionSummaryResponse>({
  openingBalance: 0,
  totalIncome: 0,
  totalExpense: 0,
  closingBalance: 0,
});
const departments = ref<DepartmentResponse[]>([]);

async function fetchData() {
  loading.value = true;
  try {
    const [txList, txSummary, deptList] = await Promise.all([
      getBankTransactionsApi(props.bankName, currentYear.value, currentMonth.value),
      getBankTransactionSummaryApi(
        props.bankName,
        currentYear.value,
        currentMonth.value,
      ),
      getDepartmentsApi(),
    ]);
    transactions.value = txList;
    summary.value = txSummary;
    departments.value = deptList;
    selectedRowKeys.value = selectedRowKeys.value.filter((id) =>
      txList.some((transaction) => transaction.id === id),
    );
  } finally {
    loading.value = false;
  }
}

watch([() => props.bankName, currentYear, currentMonth], () => {
  clearBatchSelection();
  void fetchData();
});
onMounted(fetchData);

// ── 表格欄位 ────────────────────────────────────

const columns = [
  { dataIndex: 'transactionDate', title: '日期', width: 110 },
  { dataIndex: 'description', title: '摘要', ellipsis: true, minWidth: 350 },
  { dataIndex: 'departmentName', title: '歸屬部門', width: 120 },
  { key: 'activitySource', title: '來源活動', width: 130 },
  { key: 'income', title: '收入', width: 120, align: 'right' as const },
  { key: 'expense', title: '支出', width: 120, align: 'right' as const },
  { dataIndex: 'fee', title: '手續費', width: 100, align: 'right' as const },
  { dataIndex: 'runningBalance', title: '餘額', width: 130, align: 'right' as const },
  { key: 'receiptStatus', title: '收據狀態', width: 130, align: 'center' as const },
  { key: 'action', title: '操作', width: 100, fixed: 'right' as const },
];

// ── 表格合計 ────────────────────────────────────

const totalIncome = computed(() =>
  transactions.value
    .filter((transaction) => transaction.transactionType === 0)
    .reduce((sum, transaction) => sum + transaction.amount, 0),
);

const totalExpense = computed(() =>
  transactions.value
    .filter((transaction) => transaction.transactionType === 1)
    .reduce((sum, transaction) => sum + transaction.amount, 0),
);

const totalFee = computed(() =>
  transactions.value.reduce((sum, transaction) => sum + transaction.fee, 0),
);

// ── 金額格式化 ──────────────────────────────────

const formatCurrency = (val: number): string =>
  val.toLocaleString('zh-TW', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  });

const formatMoneyInput = (val: number | string | undefined) =>
  `$ ${val ?? ''}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',');

const parseMoneyInput = (val: string | undefined) =>
  Number(val?.replace(/\$\s?|(,*)/g, '') ?? '0');

const resolveTransactionYear = (date?: string) => {
  const parsed = date ? parseTransactionDate(date) : dayjs();
  return parsed?.isValid() ? parsed.year() : dayjs().year();
};

const resolveTransactionMonth = (date?: string) => {
  const parsed = date ? parseTransactionDate(date) : dayjs();
  return parsed?.isValid() ? parsed.month() + 1 : dayjs().month() + 1;
};

// ── Modal ───────────────────────────────────────

const modalVisible = ref(false);
const modalTitle = ref('新增交易');
const editingId = ref<null | string>(null);
const submitting = ref(false);
const isSyncingFormState = ref(false);

const formState = ref<CreateBankTransactionRequest>({
  ...defaultFormState(),
});

const activities = ref<ActivitySummaryResponse[]>([]);
const activitiesLoading = ref(false);
const budgetItems = ref<BudgetItemResponse[]>([]);
const budgetItemsLoading = ref(false);
const activityMode = ref<ActivityMode>('none');
const selectedBudgetItemId = ref<string | undefined>();
const newActivityName = ref('');
const newActivityMonth = ref(resolveTransactionMonth(defaultFormState().transactionDate));

const activityDrawerOpen = ref(false);
const activityDrawerActivity = ref<ActivityDetailResponse | null>(null);
let linkageRequestId = 0;

const departmentOptions = computed(() =>
  departments.value.map((department) => ({
    id: department.id,
    name: department.name,
  })),
);

const selectedBudgetItem = computed(() =>
  budgetItems.value.find((item) => item.id === selectedBudgetItemId.value),
);

const showActivityLinkSection = computed(
  () => formState.value.transactionType === 1 && Boolean(formState.value.departmentId),
);

const showExistingActivitySelector = computed(
  () =>
    formState.value.transactionType === 1 &&
    Boolean(formState.value.departmentId) &&
    (Boolean(editingId.value) || activityMode.value === 'existing'),
);

const showNewActivitySection = computed(
  () =>
    !editingId.value &&
    formState.value.transactionType === 1 &&
    Boolean(formState.value.departmentId) &&
    activityMode.value === 'new',
);

function resetNewActivityDraft() {
  selectedBudgetItemId.value = undefined;
  newActivityName.value = '';
  newActivityMonth.value = resolveTransactionMonth(formState.value.transactionDate);
}

function invalidateLinkageRequests() {
  linkageRequestId += 1;
  activitiesLoading.value = false;
  budgetItemsLoading.value = false;
}

function resetActivityLinkState() {
  invalidateLinkageRequests();
  activities.value = [];
  budgetItems.value = [];
  activityMode.value = 'none';
  resetNewActivityDraft();
}

async function loadLinkageOptions(departmentId?: string) {
  if (!modalVisible.value || !departmentId || formState.value.transactionType !== 1) {
    invalidateLinkageRequests();
    activities.value = [];
    budgetItems.value = [];
    return;
  }

  const requestId = ++linkageRequestId;
  const transactionYear = resolveTransactionYear(formState.value.transactionDate);
  const previousBudgetItem = budgetItems.value.find((item) => item.id === selectedBudgetItemId.value);

  activitiesLoading.value = true;
  budgetItemsLoading.value = true;

  try {
    const [departmentActivities, departmentBudgetItems] = await Promise.all([
      getActivitiesApi(transactionYear, undefined, departmentId).catch(() => []),
      getDepartmentBudgetItemsApi(transactionYear, departmentId).catch(() => []),
    ]);

    if (
      requestId !== linkageRequestId
      || !modalVisible.value
      || formState.value.transactionType !== 1
      || formState.value.departmentId !== departmentId
      || resolveTransactionYear(formState.value.transactionDate) !== transactionYear
    ) {
      return;
    }

    activities.value = departmentActivities;
    budgetItems.value = departmentBudgetItems;

    if (
      formState.value.activityId
      && !departmentActivities.some((activity) => activity.id === formState.value.activityId)
    ) {
      formState.value.activityId = undefined;
    }

    if (
      selectedBudgetItemId.value
      && !departmentBudgetItems.some((item) => item.id === selectedBudgetItemId.value)
    ) {
      const shouldClearAutoFilledName =
        activityMode.value === 'new'
        && previousBudgetItem
        && newActivityName.value.trim() === previousBudgetItem.activityName;

      selectedBudgetItemId.value = undefined;
      if (shouldClearAutoFilledName) {
        newActivityName.value = '';
      }
    }
  } finally {
    if (requestId === linkageRequestId) {
      activitiesLoading.value = false;
      budgetItemsLoading.value = false;
    }
  }
}

function handleActivityModeChange(mode: ActivityMode) {
  activityMode.value = mode;

  if (mode !== 'existing') {
    formState.value.activityId = undefined;
  }

  if (mode !== 'new') {
    resetNewActivityDraft();
  }
}

function openCreateModal() {
  isSyncingFormState.value = true;
  editingId.value = null;
  modalTitle.value = '新增交易';
  formState.value = defaultFormState();
  resetActivityLinkState();
  isSyncingFormState.value = false;
  modalVisible.value = true;
}

async function openEditModal(record: BankTransactionResponse) {
  isSyncingFormState.value = true;
  editingId.value = record.id;
  modalTitle.value = '編輯交易';
  formState.value = {
    transactionType: record.transactionType,
    transactionDate: record.transactionDate,
    description: record.description,
    departmentId: record.departmentId ?? undefined,
    amount: record.amount,
    fee: record.fee,
    receiptCollected: record.receiptCollected,
    receiptMailed: record.receiptMailed,
    activityId: record.transactionType === 1 ? record.activityId ?? undefined : undefined,
  };
  resetActivityLinkState();
  activityMode.value = 'existing';
  modalVisible.value = true;
  await loadLinkageOptions(record.departmentId ?? undefined);
  isSyncingFormState.value = false;
}

watch(
  () => formState.value.departmentId,
  async (departmentId, previousDepartmentId) => {
    if (isSyncingFormState.value) {
      return;
    }

    if (departmentId !== previousDepartmentId) {
      formState.value.activityId = undefined;
      selectedBudgetItemId.value = undefined;
      if (activityMode.value === 'new') {
        newActivityName.value = '';
      }
    }

    if (!departmentId) {
      resetActivityLinkState();
      return;
    }

    await loadLinkageOptions(departmentId);
  },
);

watch(
  () => modalVisible.value,
  (open) => {
    if (!open) {
      invalidateLinkageRequests();
    }
  },
);

watch(
  () => formState.value.transactionType,
  async (transactionType) => {
    if (isSyncingFormState.value) {
      return;
    }

    if (transactionType !== 1) {
      formState.value.activityId = undefined;
      resetActivityLinkState();
      return;
    }

    if (formState.value.departmentId) {
      await loadLinkageOptions(formState.value.departmentId);
    }
  },
);

watch(
  () => formState.value.transactionDate,
  async (transactionDate) => {
    if (activityMode.value === 'new') {
      newActivityMonth.value = resolveTransactionMonth(transactionDate);
    }

    if (!modalVisible.value || !formState.value.departmentId || formState.value.transactionType !== 1) {
      return;
    }

    await loadLinkageOptions(formState.value.departmentId);
  },
);

watch(selectedBudgetItemId, (budgetItemId) => {
  if (activityMode.value !== 'new' || !budgetItemId) {
    return;
  }

  const budgetItem = budgetItems.value.find((item) => item.id === budgetItemId);
  if (budgetItem && !newActivityName.value.trim()) {
    newActivityName.value = budgetItem.activityName;
  }
});

watch(currentYear, async () => {
  if (!modalVisible.value || !formState.value.departmentId || formState.value.transactionType !== 1) {
    return;
  }

  await loadLinkageOptions(formState.value.departmentId);
});

async function handleSubmit() {
  const parsedDate = parseTransactionDate(formState.value.transactionDate);
  const description = formState.value.description.trim();
  if (!parsedDate) {
    message.warning('請選擇日期');
    return;
  }

  if (!description) {
    message.warning('請輸入摘要');
    return;
  }

  if (!formState.value.amount || formState.value.amount <= 0) {
    message.warning('請輸入有效金額');
    return;
  }

  if (showExistingActivitySelector.value && !editingId.value && activityMode.value === 'existing' && !formState.value.activityId) {
    message.warning('請選擇要關聯的活動');
    return;
  }

  let createdActivity: ActivityDetailResponse | null = null;
  let transactionSaved = false;

  submitting.value = true;
  try {
    if (!editingId.value && activityMode.value === 'new') {
      if (!formState.value.departmentId) {
        message.warning('建立活動前請先選擇部門');
        return;
      }

      const activityName =
        newActivityName.value.trim() || selectedBudgetItem.value?.activityName?.trim();

      if (!activityName) {
        message.warning('請輸入活動名稱');
        return;
      }

      createdActivity = await createActivityApi({
        departmentId: formState.value.departmentId,
        year: resolveTransactionYear(formState.value.transactionDate),
        month: newActivityMonth.value,
        name: activityName,
        expenses: selectedBudgetItem.value
          ? [{
              description: selectedBudgetItem.value.contentItem,
              amount: formState.value.amount,
              sequence: 1,
              budgetItemId: selectedBudgetItem.value.id,
            }]
          : undefined,
      });
    }

    const activityId = editingId.value
      ? formState.value.activityId
      : createdActivity?.id ?? formState.value.activityId;
    const payload: CreateBankTransactionRequest = {
      ...formState.value,
      description,
      activityId: formState.value.transactionType === 1 ? activityId : undefined,
    };

    if (editingId.value) {
      await updateBankTransactionApi(editingId.value, payload);
      transactionSaved = true;
      message.success('交易已更新');
    } else {
      await createBankTransactionApi(props.bankName, payload);
      transactionSaved = true;
      message.success('交易已新增');
    }

    modalVisible.value = false;
    try {
      await fetchData();
    } catch {
      message.warning('交易已儲存，但列表重新整理失敗');
    }

    if (createdActivity) {
      Modal.confirm({
        title: '是否立即補填活動細項？',
        content: '新活動已建立完成，是否現在開啟活動細項編輯？',
        okText: '立即補填',
        cancelText: '稍後再填',
        onOk: () => {
          activityDrawerActivity.value = createdActivity;
          activityDrawerOpen.value = true;
        },
      });
    }
  } catch (error: any) {
    if (createdActivity && !transactionSaved) {
      try {
        await deleteActivityApi(createdActivity.id);
      } catch {
        // Ignore cleanup failure and surface the original error.
      }
    }
    message.error(error?.response?.data?.error ?? '儲存失敗');
  } finally {
    submitting.value = false;
  }
}

function handleActivityDrawerClose() {
  activityDrawerOpen.value = false;
  activityDrawerActivity.value = null;
}

async function handleActivityDrawerSaved() {
  activityDrawerOpen.value = false;
  activityDrawerActivity.value = null;
  await fetchData();
}

// ── 刪除 ────────────────────────────────────────

async function handleDelete(id: string) {
  try {
    await deleteBankTransactionApi(id);
    message.success('交易已刪除');
    await fetchData();
  } catch {
    message.error('刪除失敗');
  }
}

// ── 匯出 ────────────────────────────────────────

const exporting = ref(false);

async function handleExport() {
  exporting.value = true;
  try {
    await exportBankTransactionsApi(
      props.bankName,
      currentYear.value,
      currentMonth.value,
    );
    message.success('匯出成功');
  } catch {
    message.error('匯出失敗');
  } finally {
    exporting.value = false;
  }
}

// ── 批次更新部門 ────────────────────────────────

const selectedRowKeys = ref<string[]>([]);
const batchDepartmentId = ref<string | undefined>(undefined);
const batchSubmitting = ref(false);

const rowSelection = computed(() => ({
  selectedRowKeys: selectedRowKeys.value,
  onChange: (keys: Array<number | string>) => {
    selectedRowKeys.value = keys as string[];
  },
}));

function clearBatchSelection() {
  selectedRowKeys.value = [];
  batchDepartmentId.value = undefined;
}

async function handleBatchUpdateDepartment() {
  if (selectedRowKeys.value.length === 0) {
    return;
  }

  batchSubmitting.value = true;
  try {
    await batchUpdateDepartmentApi({
      bankName: props.bankName,
      ids: selectedRowKeys.value,
      month: currentMonth.value,
      departmentId: batchDepartmentId.value ?? null,
      year: currentYear.value,
    });
    message.success(`已更新 ${selectedRowKeys.value.length} 筆交易的歸屬部門`);
    clearBatchSelection();
    await fetchData();
  } catch (error: any) {
    message.error(error?.response?.data?.error ?? '批次更新失敗');
  } finally {
    batchSubmitting.value = false;
  }
}
</script>

<template>
  <TsfGlassPage
    :icon="bankName.includes('上海') ? 'i-lucide-building-2' : 'i-lucide-landmark'"
    subtitle="銀行交易、活動來源、收據狀態與月度餘額檢視。"
    :title="`${bankName}收支表`"
  >
    <template #actions>
      <Select
        v-model:value="currentYear"
        :options="yearOptions.map((year) => ({ label: `${year}年`, value: year }))"
        style="width: 108px"
      />
      <Button
        :loading="exporting"
        type="default"
        @click="handleExport"
      >
        <template #icon><span class="i-lucide-download" /></template>
        匯出 Excel
      </Button>
      <Button type="primary" @click="openCreateModal">
        <template #icon><span class="i-lucide-plus" /></template>
        新增交易
      </Button>
    </template>

    <template #filters>
      <div class="tsf-filter-group">
        <span class="tsf-filter-label">月份</span>
        <Segmented
          v-model:value="monthSegmentedValue"
          :options="segmentedOptions"
        />
      </div>
    </template>

    <Spin :spinning="loading">
      <div class="tsf-kpi-grid">
        <TsfMetricCard
          icon="i-lucide-wallet-cards"
          label="期初餘額"
          prefix="$"
          tone="neutral"
          :value="formatCurrency(summary.openingBalance)"
        />
        <TsfMetricCard
          icon="i-lucide-trending-up"
          label="期間收入"
          prefix="$"
          tone="success"
          :value="formatCurrency(summary.totalIncome)"
        />
        <TsfMetricCard
          icon="i-lucide-trending-down"
          label="期間支出"
          prefix="$"
          tone="danger"
          :value="formatCurrency(summary.totalExpense)"
        />
        <TsfMetricCard
          icon="i-lucide-badge-dollar-sign"
          label="期末餘額"
          prefix="$"
          tone="info"
          :value="formatCurrency(summary.closingBalance)"
        />
      </div>

      <section class="tsf-table-panel mt-4">
        <div class="tsf-section-header">
          <div class="tsf-section-title-group">
            <span class="tsf-section-kicker">交易台帳</span>
            <h3 class="tsf-section-title">銀行交易明細</h3>
            <p class="tsf-section-description">
              以日期、歸屬部門、收支與收據狀態檢視本期交易紀錄。
            </p>
          </div>
          <div class="tsf-section-meta">
            <span class="tsf-meta-pill">
              {{ currentMonth === undefined ? '全年' : `${currentMonth} 月` }}
            </span>
            <span class="tsf-meta-pill">共 {{ transactions.length }} 筆</span>
          </div>
        </div>

        <div class="tsf-table-body">
          <div class="mb-3 flex flex-wrap items-center justify-between gap-2">
            <div
              v-if="selectedRowKeys.length > 0"
              class="flex items-center gap-2"
            >
              <Tag color="blue">已選取 {{ selectedRowKeys.length }} 筆</Tag>
              <Select
                v-model:value="batchDepartmentId"
                allow-clear
                placeholder="選擇部門"
                style="width: 160px"
              >
                <SelectOption
                  v-for="dept in departments"
                  :key="dept.id"
                  :value="dept.id"
                >
                  {{ dept.name }}
                </SelectOption>
              </Select>
              <Popconfirm
                :title="`確定要將 ${selectedRowKeys.length} 筆交易${batchDepartmentId ? '指定' : '清除'}歸屬部門？`"
                ok-text="確認"
                cancel-text="取消"
                @confirm="handleBatchUpdateDepartment"
              >
                <Button :loading="batchSubmitting" type="primary">
                  套用
                </Button>
              </Popconfirm>
              <Button @click="clearBatchSelection">取消選取</Button>
            </div>
            <div v-else />
          </div>

          <Table
            :columns="columns"
            :data-source="transactions"
            :loading="loading"
            :pagination="{ pageSize: 50, showTotal: (total: number) => `共 ${total} 筆` }"
            row-key="id"
            :row-selection="rowSelection"
            :scroll="{ x: 1100 }"
            size="middle"
          >
          <template #bodyCell="{ column, record }">
            <template v-if="column.dataIndex === 'transactionDate'">
              {{ (record as BankTransactionResponse).transactionDate }}
            </template>

            <template v-if="column.key === 'income'">
              <span
                v-if="(record as BankTransactionResponse).transactionType === 0"
                class="text-green-600"
              >
                {{ formatCurrency((record as BankTransactionResponse).amount) }}
              </span>
            </template>

            <template v-if="column.key === 'expense'">
              <span
                v-if="(record as BankTransactionResponse).transactionType === 1"
                class="text-red-600"
              >
                {{ formatCurrency((record as BankTransactionResponse).amount) }}
              </span>
            </template>

            <template v-if="column.dataIndex === 'fee'">
              <span v-if="(record as BankTransactionResponse).fee > 0" class="text-orange-500">
                {{ formatCurrency((record as BankTransactionResponse).fee) }}
              </span>
            </template>

            <template v-if="column.dataIndex === 'runningBalance'">
              <span class="font-medium">
                {{ formatCurrency((record as BankTransactionResponse).runningBalance) }}
              </span>
            </template>

            <template v-if="column.dataIndex === 'departmentName'">
              <Tag v-if="(record as BankTransactionResponse).departmentName" color="blue">
                {{ (record as BankTransactionResponse).departmentName }}
              </Tag>
            </template>

            <template v-if="column.key === 'activitySource'">
              <Tag
                v-if="(record as BankTransactionResponse).transactionType === 1 && (record as BankTransactionResponse).activityName"
                color="purple"
                class="max-w-[120px] truncate"
              >
                {{ (record as BankTransactionResponse).activityName }}
              </Tag>
              <span v-else class="text-gray-300">—</span>
            </template>

            <template v-if="column.key === 'receiptStatus'">
              <div
                v-if="(record as BankTransactionResponse).transactionType === 1"
                class="flex items-center justify-center gap-2"
              >
                <Tooltip :title="(record as BankTransactionResponse).receiptCollected ? '已回收' : '未回收'">
                  <span
                    class="i-lucide-hand text-base"
                    :class="(record as BankTransactionResponse).receiptCollected ? 'text-green-600' : 'text-red-400'"
                  />
                </Tooltip>
                <Tooltip :title="(record as BankTransactionResponse).receiptMailed ? '已寄送' : '未寄送'">
                  <span
                    class="i-lucide-mail-check text-base"
                    :class="(record as BankTransactionResponse).receiptMailed ? 'text-green-600' : 'text-orange-400'"
                  />
                </Tooltip>
              </div>
              <span v-else class="text-gray-300">—</span>
            </template>

            <template v-if="column.key === 'action'">
              <div class="flex gap-1">
                <Button
                  size="small"
                  type="link"
                  @click="openEditModal(record as unknown as BankTransactionResponse)"
                >
                  編輯
                </Button>
                <Popconfirm
                  cancel-text="取消"
                  ok-text="確認刪除"
                  ok-type="danger"
                  :title="`確定要刪除「${(record as BankTransactionResponse).description}」嗎？`"
                  @confirm="handleDelete((record as BankTransactionResponse).id)"
                >
                  <Button danger size="small" type="link">刪除</Button>
                </Popconfirm>
              </div>
            </template>
          </template>

          <template #summary>
            <Table.Summary.Row>
              <Table.Summary.Cell :index="0" :col-span="3">
                <span class="font-bold">合計</span>
              </Table.Summary.Cell>
              <Table.Summary.Cell :index="3" align="right">
                <span class="font-bold text-green-600">{{ formatCurrency(totalIncome) }}</span>
              </Table.Summary.Cell>
              <Table.Summary.Cell :index="4" align="right">
                <span class="font-bold text-red-600">{{ formatCurrency(totalExpense) }}</span>
              </Table.Summary.Cell>
              <Table.Summary.Cell :index="5" align="right">
                <span class="font-bold text-orange-500">{{ formatCurrency(totalFee) }}</span>
              </Table.Summary.Cell>
              <Table.Summary.Cell :index="6" :col-span="3" />
            </Table.Summary.Row>
          </template>

          <template #emptyText>
            <div class="py-8 text-center text-gray-400">
              本期間尚無交易記錄，請點擊「新增交易」按鈕建立
            </div>
          </template>
          </Table>
        </div>
      </section>
    </Spin>

    <Modal
      :confirm-loading="submitting"
      destroy-on-close
      ok-text="儲存"
      :open="modalVisible"
      :title="modalTitle"
      :width="620"
      @cancel="modalVisible = false"
      @ok="handleSubmit"
    >
      <Form :model="formState" class="mt-4" layout="vertical">
        <FormItem label="交易類型" required>
          <RadioGroup v-model:value="formState.transactionType">
            <Radio :value="0">收入</Radio>
            <Radio :value="1">支出</Radio>
          </RadioGroup>
        </FormItem>

        <FormItem label="日期" required>
          <DatePicker
            :value="formState.transactionDate ? dayjs(formState.transactionDate) : undefined"
            class="w-full"
            format="YYYY/MM/DD"
            placeholder="選擇日期"
            @change="(_: unknown, dateStr: string | string[]) => {
              formState.transactionDate =
                typeof dateStr === 'string' ? dateStr : (dateStr[0] ?? '');
            }"
          />
        </FormItem>

        <FormItem label="摘要" required>
          <Input
            v-model:value="formState.description"
            :maxlength="200"
            placeholder="請輸入交易說明"
            show-count
          />
        </FormItem>

        <FormItem label="歸屬部門">
          <Select
            v-model:value="formState.departmentId"
            allow-clear
            placeholder="請選擇部門（選填）"
          >
            <SelectOption
              v-for="dept in departments"
              :key="dept.id"
              :value="dept.id"
            >
              {{ dept.name }}
            </SelectOption>
          </Select>
        </FormItem>

        <FormItem v-if="!editingId && showActivityLinkSection" label="活動處理">
          <RadioGroup
            :value="activityMode"
            @update:value="handleActivityModeChange"
          >
            <Radio value="none">不關聯活動</Radio>
            <Radio value="existing">選擇既有活動</Radio>
            <Radio value="new">建立新活動</Radio>
          </RadioGroup>
        </FormItem>

        <FormItem v-if="showExistingActivitySelector" label="來源活動">
          <Select
            v-model:value="formState.activityId"
            allow-clear
            :loading="activitiesLoading"
            placeholder="選擇來源活動（選填）"
          >
            <SelectOption
              v-for="act in activities"
              :key="act.id"
              :value="act.id"
            >
              {{ act.name }}
            </SelectOption>
          </Select>
          <div
            v-if="!activitiesLoading && activities.length === 0"
            class="mt-1 text-gray-400 text-xs"
          >
            此部門在 {{ resolveTransactionYear(formState.transactionDate) }} 年尚無可關聯的活動
          </div>
        </FormItem>

        <template v-if="showNewActivitySection">
          <FormItem label="年度預算項目">
            <Select
              v-model:value="selectedBudgetItemId"
              allow-clear
              :loading="budgetItemsLoading"
              placeholder="選擇年度預算項目（選填）"
            >
              <SelectOption
                v-for="item in budgetItems"
                :key="item.id"
                :value="item.id"
              >
                {{ item.activityName }} - {{ item.contentItem }}
              </SelectOption>
            </Select>
            <div
              v-if="!budgetItemsLoading && budgetItems.length === 0"
              class="mt-1 text-gray-400 text-xs"
            >
              此年度尚無可用預算項目，仍可建立活動，但不會自動帶入活動細項
            </div>
          </FormItem>

          <Row :gutter="16">
            <Col :span="14">
              <FormItem label="新活動名稱" required>
                <Input
                  v-model:value="newActivityName"
                  :maxlength="200"
                  placeholder="請輸入活動名稱"
                />
              </FormItem>
            </Col>
            <Col :span="10">
              <FormItem label="活動月份">
                <Select v-model:value="newActivityMonth">
                  <SelectOption v-for="month in 12" :key="month" :value="month">
                    {{ month }} 月
                  </SelectOption>
                </Select>
              </FormItem>
            </Col>
          </Row>
        </template>

        <Row :gutter="16">
          <Col :span="12">
            <FormItem label="金額" required>
              <InputNumber
                v-model:value="formState.amount"
                class="w-full"
                :formatter="formatMoneyInput"
                :min="1"
                :parser="parseMoneyInput"
                :precision="0"
              />
            </FormItem>
          </Col>
          <Col :span="12">
            <FormItem label="手續費">
              <InputNumber
                v-model:value="formState.fee"
                class="w-full"
                :formatter="formatMoneyInput"
                :min="0"
                :parser="parseMoneyInput"
                :precision="0"
                placeholder="選填"
              />
            </FormItem>
          </Col>
        </Row>

        <FormItem v-if="formState.transactionType === 1">
          <div class="flex gap-6">
            <Checkbox v-model:checked="formState.receiptCollected">收據已回收</Checkbox>
            <Checkbox v-model:checked="formState.receiptMailed">收據已寄送</Checkbox>
          </div>
        </FormItem>
      </Form>
    </Modal>

    <ActivityFormDrawer
      :departments="departmentOptions"
      :editing-activity="activityDrawerActivity"
      :month="activityDrawerActivity?.month ?? resolveTransactionMonth(formState.transactionDate)"
      :open="activityDrawerOpen"
      :year="activityDrawerActivity?.year ?? resolveTransactionYear(formState.transactionDate)"
      @close="handleActivityDrawerClose"
      @saved="handleActivityDrawerSaved"
    />
  </TsfGlassPage>
</template>
