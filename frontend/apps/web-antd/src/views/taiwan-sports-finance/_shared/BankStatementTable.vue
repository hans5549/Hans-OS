<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';

import { Page } from '@vben/common-ui';

import {
  Button,
  Card,
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
  Statistic,
  Table,
  Tag,
  Tooltip,
} from 'ant-design-vue';
import dayjs from 'dayjs';

import type {
  BankTransactionResponse,
  BankTransactionSummaryResponse,
  CreateBankTransactionRequest,
  DepartmentResponse,
  TransactionType,
} from '#/api';

import {
  batchUpdateDepartmentApi,
  createBankTransactionApi,
  deleteBankTransactionApi,
  exportBankTransactionsApi,
  getBankTransactionsApi,
  getBankTransactionSummaryApi,
  getDepartmentsApi,
  updateBankTransactionApi,
} from '#/api';

const props = defineProps<{
  bankName: string;
}>();

// ── 時間篩選 ────────────────────────────────────

const currentYear = ref(dayjs().year());
const currentMonth = ref<number | undefined>(dayjs().month() + 1);
const defaultFormState = (): CreateBankTransactionRequest => ({
  transactionType: 0 as TransactionType,
  transactionDate: dayjs().format('YYYY-MM-DD'),
  description: '',
  amount: 0,
  fee: 0,
  receiptCollected: false,
  receiptMailed: false,
});

const yearOptions = computed(() => {
  const startYear = 2019;
  const endYear = dayjs().year() + 1;
  return Array.from({ length: endYear - startYear + 1 }, (_, index) => startYear + index);
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

const segmentedOptions = computed(() => monthOptions.value.map((o) => o.label));

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
      getBankTransactionSummaryApi(props.bankName, currentYear.value, currentMonth.value),
      getDepartmentsApi(),
    ]);
    transactions.value = txList;
    summary.value = txSummary;
    departments.value = deptList;
  } finally {
    loading.value = false;
  }
}

watch([currentYear, currentMonth], () => fetchData());
onMounted(fetchData);

// ── 表格欄位 ────────────────────────────────────

const columns = [
  { dataIndex: 'transactionDate', title: '日期', width: 110 },
  { dataIndex: 'description', title: '摘要', ellipsis: true, minWidth: 400 },
  { dataIndex: 'departmentName', title: '歸屬部門', width: 120 },
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
    .filter((t) => t.transactionType === 0)
    .reduce((sum, t) => sum + t.amount, 0),
);

const totalExpense = computed(() =>
  transactions.value
    .filter((t) => t.transactionType === 1)
    .reduce((sum, t) => sum + t.amount, 0),
);

const totalFee = computed(() =>
  transactions.value.reduce((sum, t) => sum + t.fee, 0),
);

// ── 金額格式化 ──────────────────────────────────

const formatCurrency = (val: number): string =>
  val.toLocaleString('zh-TW', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  });

// ── Modal ───────────────────────────────────────

const modalVisible = ref(false);
const modalTitle = ref('新增交易');
const editingId = ref<null | string>(null);
const submitting = ref(false);

const formState = ref<CreateBankTransactionRequest>({
  ...defaultFormState(),
});

function openCreateModal() {
  editingId.value = null;
  modalTitle.value = '新增交易';
  formState.value = defaultFormState();
  modalVisible.value = true;
}

function openEditModal(record: BankTransactionResponse) {
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
  };
  modalVisible.value = true;
}

async function handleSubmit() {
  const description = formState.value.description.trim();
  if (!description) {
    message.warning('請輸入摘要');
    return;
  }

  if (!formState.value.amount || formState.value.amount <= 0) {
    message.warning('請輸入有效金額');
    return;
  }

  const payload = {
    ...formState.value,
    description,
  };

  submitting.value = true;
  try {
    if (editingId.value) {
      await updateBankTransactionApi(editingId.value, payload);
      message.success('交易已更新');
    } else {
      await createBankTransactionApi(props.bankName, payload);
      message.success('交易已新增');
    }
    modalVisible.value = false;
    await fetchData();
  } finally {
    submitting.value = false;
  }
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
  onChange: (keys: (number | string)[]) => {
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
      ids: selectedRowKeys.value,
      departmentId: batchDepartmentId.value ?? null,
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
  <Page content-class="p-0" :title="`${bankName}收支表`">
    <Card :body-style="{ padding: '16px 24px' }">
      <!-- 頂部控制列 -->
      <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div class="flex items-center gap-3">
          <span class="text-2xl">{{ bankName.includes('上海') ? '🏦' : '🏛️' }}</span>
          <span class="text-lg font-medium">{{ bankName }}收支表</span>
        </div>
        <div class="flex items-center gap-2">
          <Select
            v-model:value="currentYear"
            :options="yearOptions.map((y) => ({ label: `${y}年`, value: y }))"
            style="width: 100px"
          />
          <Button
            :loading="exporting"
            type="default"
            @click="handleExport"
          >
            <template #icon><span class="i-lucide-download" /></template>
            匯出 Excel
          </Button>
        </div>
      </div>

      <!-- 月份切換器 -->
      <div class="mb-4">
        <Segmented
          v-model:value="monthSegmentedValue"
          :options="segmentedOptions"
          block
        />
      </div>

      <!-- 摘要卡片 -->
      <Spin :spinning="loading">
        <Row :gutter="16" class="mb-4">
          <Col :md="6" :xs="12">
            <Card size="small">
              <Statistic
                :precision="0"
                prefix="$"
                title="期初餘額"
                :value="summary.openingBalance"
              />
            </Card>
          </Col>
          <Col :md="6" :xs="12">
            <Card size="small">
              <Statistic
                :precision="0"
                prefix="$"
                title="期間收入"
                :value="summary.totalIncome"
                :value-style="{ color: '#3f8600' }"
              />
            </Card>
          </Col>
          <Col :md="6" :xs="12">
            <Card size="small">
              <Statistic
                :precision="0"
                prefix="$"
                title="期間支出"
                :value="summary.totalExpense"
                :value-style="{ color: '#cf1322' }"
              />
            </Card>
          </Col>
          <Col :md="6" :xs="12">
            <Card size="small">
              <Statistic
                :precision="0"
                prefix="$"
                title="期末餘額"
                :value="summary.closingBalance"
                :value-style="{ color: '#1677ff' }"
              />
            </Card>
          </Col>
        </Row>

        <!-- 操作列 -->
        <div class="mb-3 flex flex-wrap items-center justify-between gap-2">
          <!-- 批次操作 -->
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

          <Button type="primary" @click="openCreateModal">
            <template #icon><span class="i-lucide-plus" /></template>
            新增交易
          </Button>
        </div>

        <!-- 交易表格 -->
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
            <!-- 日期 -->
            <template v-if="column.dataIndex === 'transactionDate'">
              {{ (record as BankTransactionResponse).transactionDate }}
            </template>

            <!-- 收入 -->
            <template v-if="column.key === 'income'">
              <span
                v-if="(record as BankTransactionResponse).transactionType === 0"
                class="text-green-600"
              >
                {{ formatCurrency((record as BankTransactionResponse).amount) }}
              </span>
            </template>

            <!-- 支出 -->
            <template v-if="column.key === 'expense'">
              <span
                v-if="(record as BankTransactionResponse).transactionType === 1"
                class="text-red-600"
              >
                {{ formatCurrency((record as BankTransactionResponse).amount) }}
              </span>
            </template>

            <!-- 手續費 -->
            <template v-if="column.dataIndex === 'fee'">
              <span v-if="(record as BankTransactionResponse).fee > 0" class="text-orange-500">
                {{ formatCurrency((record as BankTransactionResponse).fee) }}
              </span>
            </template>

            <!-- 餘額 -->
            <template v-if="column.dataIndex === 'runningBalance'">
              <span class="font-medium">
                {{ formatCurrency((record as BankTransactionResponse).runningBalance) }}
              </span>
            </template>

            <!-- 部門 -->
            <template v-if="column.dataIndex === 'departmentName'">
              <Tag v-if="(record as BankTransactionResponse).departmentName" color="blue">
                {{ (record as BankTransactionResponse).departmentName }}
              </Tag>
            </template>

            <!-- 收據狀態（支出才顯示） -->
            <template v-if="column.key === 'receiptStatus'">
              <div v-if="(record as BankTransactionResponse).transactionType === 1" class="flex items-center justify-center gap-2">
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

            <!-- 操作 -->
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

          <!-- 合計列 -->
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

          <!-- 空狀態 -->
          <template #emptyText>
            <div class="py-8 text-center text-gray-400">
              本期間尚無交易記錄，請點擊「新增交易」按鈕建立
            </div>
          </template>
        </Table>
      </Spin>
    </Card>

    <!-- 新增/編輯 Modal -->
    <Modal
      :confirm-loading="submitting"
      destroy-on-close
      ok-text="儲存"
      :open="modalVisible"
      :title="modalTitle"
      :width="520"
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
            @change="(_: unknown, dateStr: string | string[]) => { formState.transactionDate = typeof dateStr === 'string' ? dateStr : dateStr[0] ?? ''; }"
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

        <Row :gutter="16">
          <Col :span="12">
            <FormItem label="金額" required>
              <InputNumber
                v-model:value="formState.amount"
                class="w-full"
                :formatter="(val: string | number | undefined) => `$ ${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')"
                :min="1"
                :parser="(val: string | undefined) => Number(val?.replace(/\$\s?|(,*)/g, '') ?? '0')"
                :precision="0"
              />
            </FormItem>
          </Col>
          <Col :span="12">
            <FormItem label="手續費">
              <InputNumber
                v-model:value="formState.fee"
                class="w-full"
                :formatter="(val: string | number | undefined) => `$ ${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')"
                :min="0"
                :parser="(val: string | undefined) => Number(val?.replace(/\$\s?|(,*)/g, '') ?? '0')"
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
  </Page>
</template>
