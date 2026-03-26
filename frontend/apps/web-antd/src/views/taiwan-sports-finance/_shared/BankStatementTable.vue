<script lang="ts" setup>
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

const yearOptions = computed(() => {
  const startYear = 2019;
  const endYear = dayjs().year() + 1;
  const years: number[] = [];
  for (let y = startYear; y <= endYear; y++) {
    years.push(y);
  }
  return years;
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
  { dataIndex: 'description', title: '摘要', ellipsis: true },
  { dataIndex: 'departmentName', title: '歸屬部門', width: 120 },
  { dataIndex: 'requestingUnit', title: '需求單位', width: 120 },
  { key: 'income', title: '收入', width: 120, align: 'right' as const },
  { key: 'expense', title: '支出', width: 120, align: 'right' as const },
  { dataIndex: 'fee', title: '手續費', width: 100, align: 'right' as const },
  { dataIndex: 'runningBalance', title: '餘額', width: 130, align: 'right' as const },
  { key: 'receipt', title: '收據', width: 70, align: 'center' as const },
  { key: 'mailed', title: '已寄送', width: 70, align: 'center' as const },
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

function formatCurrency(val: number): string {
  return val.toLocaleString('zh-TW', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

// ── Modal ───────────────────────────────────────

const modalVisible = ref(false);
const modalTitle = ref('新增交易');
const editingId = ref<null | string>(null);
const submitting = ref(false);

const formState = ref<CreateBankTransactionRequest>({
  transactionType: 0 as TransactionType,
  transactionDate: dayjs().format('YYYY-MM-DD'),
  description: '',
  amount: 0,
  fee: 0,
  hasReceipt: false,
  receiptMailed: false,
});

function openCreateModal() {
  editingId.value = null;
  modalTitle.value = '新增交易';
  formState.value = {
    transactionType: 0,
    transactionDate: dayjs().format('YYYY-MM-DD'),
    description: '',
    amount: 0,
    fee: 0,
    hasReceipt: false,
    receiptMailed: false,
  };
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
    requestingUnit: record.requestingUnit ?? undefined,
    amount: record.amount,
    fee: record.fee,
    hasReceipt: record.hasReceipt,
    receiptMailed: record.receiptMailed,
  };
  modalVisible.value = true;
}

async function handleSubmit() {
  if (!formState.value.description.trim()) {
    message.warning('請輸入摘要');
    return;
  }
  if (!formState.value.amount || formState.value.amount <= 0) {
    message.warning('請輸入有效金額');
    return;
  }

  submitting.value = true;
  try {
    if (editingId.value) {
      await updateBankTransactionApi(editingId.value, {
        ...formState.value,
        description: formState.value.description.trim(),
        requestingUnit: formState.value.requestingUnit?.trim() || undefined,
      });
      message.success('交易已更新');
    } else {
      await createBankTransactionApi(props.bankName, {
        ...formState.value,
        description: formState.value.description.trim(),
        requestingUnit: formState.value.requestingUnit?.trim() || undefined,
      });
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
                :precision="2"
                prefix="$"
                title="期初餘額"
                :value="summary.openingBalance"
              />
            </Card>
          </Col>
          <Col :md="6" :xs="12">
            <Card size="small">
              <Statistic
                :precision="2"
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
                :precision="2"
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
                :precision="2"
                prefix="$"
                title="期末餘額"
                :value="summary.closingBalance"
                :value-style="{ color: '#1677ff' }"
              />
            </Card>
          </Col>
        </Row>

        <!-- 新增按鈕 -->
        <div class="mb-3 flex justify-end">
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

            <!-- 需求單位 -->
            <template v-if="column.dataIndex === 'requestingUnit'">
              {{ (record as BankTransactionResponse).requestingUnit ?? '' }}
            </template>

            <!-- 收據 -->
            <template v-if="column.key === 'receipt'">
              <span v-if="(record as BankTransactionResponse).hasReceipt" class="text-green-600">✓</span>
            </template>

            <!-- 已寄送 -->
            <template v-if="column.key === 'mailed'">
              <span v-if="(record as BankTransactionResponse).receiptMailed" class="text-green-600">✓</span>
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
              <Table.Summary.Cell :index="0" :col-span="4">
                <span class="font-bold">合計</span>
              </Table.Summary.Cell>
              <Table.Summary.Cell :index="4" align="right">
                <span class="font-bold text-green-600">{{ formatCurrency(totalIncome) }}</span>
              </Table.Summary.Cell>
              <Table.Summary.Cell :index="5" align="right">
                <span class="font-bold text-red-600">{{ formatCurrency(totalExpense) }}</span>
              </Table.Summary.Cell>
              <Table.Summary.Cell :index="6" align="right">
                <span class="font-bold text-orange-500">{{ formatCurrency(totalFee) }}</span>
              </Table.Summary.Cell>
              <Table.Summary.Cell :index="7" :col-span="4" />
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

        <Row :gutter="16">
          <Col :span="12">
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
          </Col>
          <Col :span="12">
            <FormItem label="需求單位">
              <Input
                v-model:value="formState.requestingUnit"
                :maxlength="100"
                placeholder="提出需求的單位（選填）"
              />
            </FormItem>
          </Col>
        </Row>

        <Row :gutter="16">
          <Col :span="12">
            <FormItem label="金額" required>
              <InputNumber
                v-model:value="formState.amount"
                class="w-full"
                :formatter="(val: string | number | undefined) => `$ ${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')"
                :min="0.01"
                :parser="(val: string | undefined) => Number(val?.replace(/\$\s?|(,*)/g, '') ?? '0')"
                :precision="2"
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
                :precision="2"
                placeholder="選填"
              />
            </FormItem>
          </Col>
        </Row>

        <FormItem>
          <div class="flex gap-6">
            <Checkbox v-model:checked="formState.hasReceipt">有收據</Checkbox>
            <Checkbox v-model:checked="formState.receiptMailed">收據已寄送</Checkbox>
          </div>
        </FormItem>
      </Form>
    </Modal>
  </Page>
</template>
