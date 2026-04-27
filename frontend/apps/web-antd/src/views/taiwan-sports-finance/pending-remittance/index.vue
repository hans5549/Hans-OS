<script lang="ts" setup>
import { onMounted, ref } from 'vue';

import {
  Button,
  Col,
  DatePicker,
  Form,
  FormItem,
  Input,
  InputNumber,
  message,
  Modal,
  Popconfirm,
  Row,
  Segmented,
  Select,
  SelectOption,
  Spin,
  Table,
  Tag,
  Textarea,
} from 'ant-design-vue';
import dayjs from 'dayjs';

import type {
  CompletePendingRemittanceRequest,
  CreatePendingRemittanceRequest,
  DepartmentResponse,
  PendingRemittanceResponse,
  PendingRemittanceStatus,
} from '#/api';

import {
  completePendingRemittanceApi,
  createPendingRemittanceApi,
  deletePendingRemittanceApi,
  getDepartmentsApi,
  getPendingRemittancesApi,
  updatePendingRemittanceApi,
} from '#/api';

import TsfGlassPage from '../_shared/TsfGlassPage.vue';

defineOptions({ name: 'TsfPendingRemittancePage' });

// ── 狀態篩選 ────────────────────────────────────

type FilterStatus = 'all' | 'completed' | 'pending';
const filterStatus = ref<FilterStatus>('pending');

const statusSegmentedOptions = ['待處理', '已完成', '全部'];
const statusSegmentedMap: Record<string, FilterStatus> = {
  '待處理': 'pending',
  '已完成': 'completed',
  '全部': 'all',
};
const statusReverseMap: Record<FilterStatus, string> = {
  'pending': '待處理',
  'completed': '已完成',
  'all': '全部',
};

// ── 帳戶選項 ────────────────────────────────────

const accountOptions = ['上海銀行', '合作金庫'];

// ── 資料 ────────────────────────────────────────

const loading = ref(false);
const remittances = ref<PendingRemittanceResponse[]>([]);
const departments = ref<DepartmentResponse[]>([]);

async function fetchData() {
  loading.value = true;
  try {
    const statusParam: PendingRemittanceStatus | undefined =
      filterStatus.value === 'pending' ? 0 :
      filterStatus.value === 'completed' ? 1 :
      undefined;

    const [list, deptList] = await Promise.all([
      getPendingRemittancesApi(statusParam),
      getDepartmentsApi(),
    ]);
    remittances.value = list;
    departments.value = deptList;
  } finally {
    loading.value = false;
  }
}

function onStatusChange(val: string) {
  filterStatus.value = statusSegmentedMap[val] ?? 'all';
  fetchData();
}

onMounted(fetchData);

// ── 表格欄位 ────────────────────────────────────

const columns = [
  { dataIndex: 'description', title: '摘要', ellipsis: true },
  { dataIndex: 'amount', title: '匯款金額', width: 130, align: 'right' as const },
  { key: 'activitySource', title: '活動費來源', width: 150, ellipsis: true },
  { dataIndex: 'sourceAccount', title: '來源帳戶', width: 120 },
  { dataIndex: 'targetAccount', title: '目標帳戶', width: 120 },
  { dataIndex: 'departmentName', title: '部門', width: 120 },
  { dataIndex: 'recipientName', title: '匯款對象', width: 120 },
  { dataIndex: 'expectedDate', title: '預計日期', width: 110 },
  { key: 'status', title: '狀態', width: 90, align: 'center' as const },
  { key: 'action', title: '操作', width: 160, fixed: 'right' as const },
];

// ── 金額格式化 ──────────────────────────────────

function formatCurrency(val: number): string {
  return val.toLocaleString('zh-TW', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

// ── Modal ───────────────────────────────────────

const modalVisible = ref(false);
const modalTitle = ref('新增待匯款');
const editingId = ref<null | string>(null);
const submitting = ref(false);

const formState = ref<CreatePendingRemittanceRequest>({
  description: '',
  amount: 0,
  sourceAccount: '',
  targetAccount: '',
});

function openCreateModal() {
  editingId.value = null;
  modalTitle.value = '新增待匯款';
  formState.value = {
    description: '',
    amount: 0,
    sourceAccount: '',
    targetAccount: '',
  };
  modalVisible.value = true;
}

function openEditModal(record: PendingRemittanceResponse) {
  editingId.value = record.id;
  modalTitle.value = '編輯待匯款';
  formState.value = {
    description: record.description,
    amount: record.amount,
    sourceAccount: record.sourceAccount,
    targetAccount: record.targetAccount,
    departmentId: record.departmentId ?? undefined,
    recipientName: record.recipientName ?? undefined,
    expectedDate: record.expectedDate ?? undefined,
    note: record.note ?? undefined,
  };
  modalVisible.value = true;
}

async function handleSubmit() {
  if (!formState.value.description.trim()) {
    message.warning('請輸入摘要');
    return;
  }
  if (!formState.value.amount || formState.value.amount <= 0) {
    message.warning('請輸入有效匯款金額');
    return;
  }
  if (!formState.value.sourceAccount) {
    message.warning('請選擇來源帳戶');
    return;
  }
  if (!formState.value.targetAccount) {
    message.warning('請選擇目標帳戶');
    return;
  }

  submitting.value = true;
  try {
    const data = {
      ...formState.value,
      description: formState.value.description.trim(),
      recipientName: formState.value.recipientName?.trim() || undefined,
      note: formState.value.note?.trim() || undefined,
    };

    if (editingId.value) {
      await updatePendingRemittanceApi(editingId.value, data);
      message.success('匯款紀錄已更新');
    } else {
      await createPendingRemittanceApi(data);
      message.success('匯款紀錄已新增');
    }
    modalVisible.value = false;
    await fetchData();
  } finally {
    submitting.value = false;
  }
}

// ── 標記完成（開啟確認 Modal）─────────────────────

const completeModalVisible = ref(false);
const completeSubmitting = ref(false);
const completingId = ref<string | null>(null);
const completeFormState = ref<CompletePendingRemittanceRequest>({
  bankName: '',
  transactionDate: dayjs().format('YYYY-MM-DD'),
});

function handleComplete(id: string) {
  completingId.value = id;
  completeFormState.value = {
    bankName: '',
    transactionDate: dayjs().format('YYYY-MM-DD'),
  };
  completeModalVisible.value = true;
}

async function handleConfirmComplete() {
  if (!completeFormState.value.bankName) {
    message.warning('請選擇銀行帳戶');
    return;
  }
  if (!completeFormState.value.transactionDate) {
    message.warning('請選擇交易日期');
    return;
  }
  completeSubmitting.value = true;
  try {
    await completePendingRemittanceApi(completingId.value!, completeFormState.value);
    message.success('已標記為完成，並自動建立收支表支出紀錄');
    completeModalVisible.value = false;
    await fetchData();
  } catch {
    message.error('操作失敗');
  } finally {
    completeSubmitting.value = false;
  }
}

// ── 刪除 ────────────────────────────────────────

async function handleDelete(id: string) {
  try {
    await deletePendingRemittanceApi(id);
    message.success('紀錄已刪除');
    await fetchData();
  } catch {
    message.error('刪除失敗');
  }
}
</script>

<template>
  <TsfGlassPage
    icon="i-lucide-send"
    subtitle="追蹤活動費匯款清單，完成後自動建立銀行支出紀錄。"
    title="活動費待匯款"
  >
    <template #actions>
      <Button type="primary" @click="openCreateModal">
        <template #icon><span class="i-lucide-plus" /></template>
        新增待匯款
      </Button>
    </template>

    <template #filters>
      <div class="tsf-filter-group">
        <span class="tsf-filter-label">狀態</span>
        <Segmented
          :value="statusReverseMap[filterStatus]"
          :options="statusSegmentedOptions"
          @change="(val: string | number) => onStatusChange(String(val))"
        />
      </div>
    </template>

    <Spin :spinning="loading">
      <section class="tsf-table-panel">
        <Table
          :columns="columns"
          :data-source="remittances"
          :loading="loading"
          :pagination="{ pageSize: 50, showTotal: (total: number) => `共 ${total} 筆` }"
          row-key="id"
          :scroll="{ x: 1100 }"
          size="middle"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'activitySource'">
              <Tag
                v-if="(record as PendingRemittanceResponse).activityExpenseDescription"
                color="cyan"
              >
                {{ (record as PendingRemittanceResponse).activityExpenseDescription }}
              </Tag>
              <span v-else class="text-gray-300">—</span>
            </template>

            <template v-if="column.dataIndex === 'amount'">
              <span class="font-medium">
                {{ formatCurrency((record as PendingRemittanceResponse).amount) }}
              </span>
            </template>

            <template v-if="column.dataIndex === 'departmentName'">
              <Tag v-if="(record as PendingRemittanceResponse).departmentName" color="blue">
                {{ (record as PendingRemittanceResponse).departmentName }}
              </Tag>
            </template>

            <template v-if="column.dataIndex === 'expectedDate'">
              {{ (record as PendingRemittanceResponse).expectedDate ?? '' }}
            </template>

            <template v-if="column.key === 'status'">
              <Tag
                :color="(record as PendingRemittanceResponse).status === 0 ? 'orange' : 'green'"
              >
                {{ (record as PendingRemittanceResponse).status === 0 ? '待處理' : '已完成' }}
              </Tag>
            </template>

            <template v-if="column.key === 'action'">
              <div class="flex gap-1">
                <Button
                  v-if="(record as PendingRemittanceResponse).status === 0"
                  size="small"
                  type="link"
                  @click="openEditModal(record as unknown as PendingRemittanceResponse)"
                >
                  編輯
                </Button>
                <Button
                  v-if="(record as PendingRemittanceResponse).status === 0"
                  class="text-green-600"
                  size="small"
                  type="link"
                  @click="handleComplete((record as PendingRemittanceResponse).id)"
                >
                  完成
                </Button>
                <Popconfirm
                  cancel-text="取消"
                  ok-text="確認刪除"
                  ok-type="danger"
                  :title="`確定要刪除「${(record as PendingRemittanceResponse).description}」嗎？`"
                  @confirm="handleDelete((record as PendingRemittanceResponse).id)"
                >
                  <Button danger size="small" type="link">刪除</Button>
                </Popconfirm>
              </div>
            </template>
          </template>

          <template #emptyText>
            <div class="py-8 text-center text-gray-400">
              {{ filterStatus === 'pending' ? '目前沒有待處理的匯款' : '沒有匯款紀錄' }}
            </div>
          </template>
        </Table>
      </section>
    </Spin>

    <!-- 新增/編輯 Modal -->
    <Modal
      :confirm-loading="submitting"
      destroy-on-close
      ok-text="儲存"
      :open="modalVisible"
      :title="modalTitle"
      :width="560"
      @cancel="modalVisible = false"
      @ok="handleSubmit"
    >
      <Form :model="formState" class="mt-4" layout="vertical">
        <FormItem label="摘要/說明" required>
          <Input
            v-model:value="formState.description"
            :maxlength="200"
            placeholder="請輸入匯款說明"
            show-count
          />
        </FormItem>

        <Row :gutter="16">
          <Col :span="12">
            <FormItem label="匯款金額" required>
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
            <FormItem label="匯款對象">
              <Input
                v-model:value="formState.recipientName"
                :maxlength="100"
                placeholder="匯款對象名稱（選填）"
              />
            </FormItem>
          </Col>
        </Row>

        <Row :gutter="16">
          <Col :span="12">
            <FormItem label="來源帳戶" required>
              <Select
                v-model:value="formState.sourceAccount"
                placeholder="請選擇來源帳戶"
              >
                <SelectOption
                  v-for="acc in accountOptions"
                  :key="acc"
                  :value="acc"
                >
                  {{ acc }}
                </SelectOption>
              </Select>
            </FormItem>
          </Col>
          <Col :span="12">
            <FormItem label="目標帳戶" required>
              <Select
                v-model:value="formState.targetAccount"
                placeholder="請選擇目標帳戶"
              >
                <SelectOption
                  v-for="acc in accountOptions"
                  :key="acc"
                  :value="acc"
                >
                  {{ acc }}
                </SelectOption>
              </Select>
            </FormItem>
          </Col>
        </Row>

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
            <FormItem label="預計匯款日期">
              <DatePicker
                :value="formState.expectedDate ? dayjs(formState.expectedDate) : undefined"
                class="w-full"
                format="YYYY/MM/DD"
                placeholder="選擇日期（選填）"
                @change="(_: unknown, dateStr: string | string[]) => { formState.expectedDate = typeof dateStr === 'string' && dateStr ? dateStr : undefined; }"
              />
            </FormItem>
          </Col>
        </Row>

        <FormItem label="備註">
          <Textarea
            v-model:value="formState.note"
            :maxlength="1000"
            placeholder="備註說明（選填）"
            :rows="3"
            show-count
          />
        </FormItem>
      </Form>
    </Modal>
    <!-- 完成匯款 Modal -->
    <Modal
      :confirm-loading="completeSubmitting"
      destroy-on-close
      ok-text="確認完成並建立收支記錄"
      :open="completeModalVisible"
      title="完成匯款"
      :width="440"
      @cancel="completeModalVisible = false"
      @ok="handleConfirmComplete"
    >
      <p class="mb-4 text-gray-500 text-sm">完成後將自動建立一筆收支表支出紀錄</p>
      <Form :model="completeFormState" layout="vertical">
        <FormItem label="銀行帳戶" required>
          <Select
            v-model:value="completeFormState.bankName"
            placeholder="請選擇匯款來源帳戶"
          >
            <SelectOption
              v-for="acc in accountOptions"
              :key="acc"
              :value="acc"
            >
              {{ acc }}
            </SelectOption>
          </Select>
        </FormItem>
        <FormItem label="交易日期" required>
          <DatePicker
            :value="completeFormState.transactionDate ? dayjs(completeFormState.transactionDate) : undefined"
            class="w-full"
            format="YYYY/MM/DD"
            placeholder="選擇交易日期"
            @change="(_: unknown, dateStr: string | string[]) => { completeFormState.transactionDate = typeof dateStr === 'string' ? dateStr : (dateStr[0] ?? ''); }"
          />
        </FormItem>
      </Form>
    </Modal>
  </TsfGlassPage>
</template>
