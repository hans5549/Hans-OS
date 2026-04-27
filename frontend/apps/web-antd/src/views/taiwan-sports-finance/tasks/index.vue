<script lang="ts" setup>
import { computed, onMounted, ref, watch } from 'vue';

import {
  Button,
  Col,
  DatePicker,
  Form,
  FormItem,
  Input,
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
  CreateFinanceTaskRequest,
  DepartmentResponse,
  FinanceTaskStatus,
  UnifiedTaskItem,
  UnifiedTaskType,
} from '#/api';

import {
  completeFinanceTaskApi,
  completePendingRemittanceApi,
  createFinanceTaskApi,
  deleteFinanceTaskApi,
  getDepartmentsApi,
  getUnifiedTasksApi,
  patchReceiptStatusApi,
  updateFinanceTaskApi,
} from '#/api';

import TsfGlassPage from '../_shared/TsfGlassPage.vue';
import TsfMetricCard from '../_shared/TsfMetricCard.vue';

defineOptions({ name: 'TsfTasksPage' });

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
  get: () =>
    currentMonth.value === undefined ? '全年' : `${currentMonth.value}月`,
  set: (val: string) => {
    currentMonth.value =
      val === '全年' ? undefined : Number.parseInt(val, 10);
  },
});

const segmentedOptions = computed(() =>
  monthOptions.value.map((o) => o.label),
);

// ── 狀態篩選 ────────────────────────────────────

const filterStatus = ref<FinanceTaskStatus | undefined>(undefined);
const statusFilterOptions = ['全部', '待處理', '進行中', '已完成'];
const statusSegmentedValue = computed({
  get: () => {
    const map: Record<string, string> = {
      0: '待處理',
      1: '進行中',
      2: '已完成',
    };
    return filterStatus.value === undefined
      ? '全部'
      : (map[filterStatus.value] ?? '全部');
  },
  set: (val: string) => {
    const map: Record<string, FinanceTaskStatus | undefined> = {
      全部: undefined,
      待處理: 0,
      進行中: 1,
      已完成: 2,
    };
    filterStatus.value = map[val];
  },
});

// ── 類型篩選 ────────────────────────────────────

const filterType = ref<UnifiedTaskType | undefined>(undefined);
const typeFilterOptions = ['全部', '一般', '匯款', '收據'];
const typeSegmentedValue = computed({
  get: () => {
    const map: Record<string, string> = {
      0: '一般',
      1: '匯款',
      2: '收據',
    };
    return filterType.value === undefined
      ? '全部'
      : (map[filterType.value] ?? '全部');
  },
  set: (val: string) => {
    const map: Record<string, UnifiedTaskType | undefined> = {
      全部: undefined,
      一般: 0,
      匯款: 1,
      收據: 2,
    };
    filterType.value = map[val];
  },
});

// ── 資料 ────────────────────────────────────────

const loading = ref(false);
const tasks = ref<UnifiedTaskItem[]>([]);
const pendingCount = ref(0);
const inProgressCount = ref(0);
const completedCount = ref(0);
const departments = ref<DepartmentResponse[]>([]);

async function fetchData() {
  loading.value = true;
  try {
    const [result, deptList] = await Promise.all([
      getUnifiedTasksApi({
        year: currentYear.value,
        month: currentMonth.value,
        status: filterStatus.value,
        type: filterType.value,
      }),
      getDepartmentsApi(),
    ]);
    tasks.value = result.tasks;
    pendingCount.value = result.pendingCount;
    inProgressCount.value = result.inProgressCount;
    completedCount.value = result.completedCount;
    departments.value = deptList;
  } finally {
    loading.value = false;
  }
}

watch([currentYear, currentMonth, filterStatus, filterType], () => fetchData());
onMounted(fetchData);

// ── 表格欄位 ────────────────────────────────────

const columns = [
  { key: 'type', title: '類型', width: 80, align: 'center' as const },
  { dataIndex: 'title', title: '標題', ellipsis: true },
  { key: 'priority', title: '優先度', width: 90, align: 'center' as const },
  { key: 'status', title: '狀態', width: 90, align: 'center' as const },
  { dataIndex: 'departmentName', title: '部門', width: 120 },
  { dataIndex: 'dueDate', title: '截止日', width: 110 },
  { dataIndex: 'createdAt', title: '建立時間', width: 110 },
  { key: 'action', title: '操作', width: 160, fixed: 'right' as const },
];

// ── 標籤格式化 ──────────────────────────────────

const typeLabels: Record<number, string> = { 0: '一般', 1: '匯款', 2: '收據' };
const typeColors: Record<number, string> = {
  0: 'blue',
  1: 'orange',
  2: 'green',
};

const priorityLabels: Record<number, string> = {
  0: '高',
  1: '中',
  2: '低',
};
const priorityColors: Record<number, string> = {
  0: 'red',
  1: 'orange',
  2: 'default',
};

const statusLabels: Record<number, string> = {
  0: '待處理',
  1: '進行中',
  2: '已完成',
};
const statusColors: Record<number, string> = {
  0: 'default',
  1: 'processing',
  2: 'success',
};

function formatDate(val: null | string): string {
  if (!val) return '—';
  return dayjs(val).format('YYYY/MM/DD');
}

// ── 新增/編輯一般任務 Modal ────────────────────────

const modalVisible = ref(false);
const modalTitle = ref('新增任務');
const editingId = ref<null | string>(null);
const submitting = ref(false);

const formState = ref<CreateFinanceTaskRequest>({
  title: '',
  priority: 1,
});

function openCreateModal() {
  editingId.value = null;
  modalTitle.value = '新增任務';
  formState.value = { title: '', priority: 1 };
  modalVisible.value = true;
}

function openEditModal(record: UnifiedTaskItem) {
  editingId.value = record.id;
  modalTitle.value = '編輯任務';
  formState.value = {
    title: record.title,
    description: record.description ?? undefined,
    priority: record.priority,
    dueDate: record.dueDate ?? undefined,
  };
  modalVisible.value = true;
}

async function handleSubmit() {
  if (!formState.value.title.trim()) {
    message.warning('請輸入任務標題');
    return;
  }
  submitting.value = true;
  try {
    const data = {
      ...formState.value,
      title: formState.value.title.trim(),
      description: formState.value.description?.trim() || undefined,
    };

    if (editingId.value) {
      await updateFinanceTaskApi(editingId.value, data);
      message.success('任務已更新');
    } else {
      await createFinanceTaskApi(data);
      message.success('任務已新增');
    }
    modalVisible.value = false;
    await fetchData();
  } finally {
    submitting.value = false;
  }
}

// ── 快速操作 ─────────────────────────────────────

async function handleCompleteGeneral(id: string) {
  try {
    await completeFinanceTaskApi(id);
    message.success('任務已完成');
    await fetchData();
  } catch {
    message.error('操作失敗');
  }
}

async function handleDeleteGeneral(id: string) {
  try {
    await deleteFinanceTaskApi(id);
    message.success('任務已刪除');
    await fetchData();
  } catch {
    message.error('刪除失敗');
  }
}

// ── 匯款完成 Modal ──────────────────────────────

const completeRemittanceVisible = ref(false);
const completeRemittanceSubmitting = ref(false);
const completingRemittanceId = ref<null | string>(null);
const completeFormState = ref({
  bankName: '',
  transactionDate: dayjs().format('YYYY-MM-DD'),
});

const bankOptions = ['上海銀行', '合作金庫'];

function openCompleteRemittanceModal(id: string) {
  completingRemittanceId.value = id;
  completeFormState.value = {
    bankName: '',
    transactionDate: dayjs().format('YYYY-MM-DD'),
  };
  completeRemittanceVisible.value = true;
}

async function handleCompleteRemittance() {
  if (!completingRemittanceId.value) return;
  if (!completeFormState.value.bankName) {
    message.warning('請選擇銀行');
    return;
  }
  completeRemittanceSubmitting.value = true;
  try {
    await completePendingRemittanceApi(completingRemittanceId.value, {
      bankName: completeFormState.value.bankName,
      transactionDate: completeFormState.value.transactionDate,
    });
    message.success('匯款已完成');
    completeRemittanceVisible.value = false;
    await fetchData();
  } catch {
    message.error('操作失敗');
  } finally {
    completeRemittanceSubmitting.value = false;
  }
}

// ── 收據快速更新 ────────────────────────────────

async function handleToggleReceipt(
  id: string,
  field: 'receiptCollected' | 'receiptMailed',
) {
  try {
    await patchReceiptStatusApi(id, {
      [field]: true,
    });
    message.success('收據狀態已更新');
    await fetchData();
  } catch {
    message.error('更新失敗');
  }
}
</script>

<template>
  <TsfGlassPage
    icon="i-lucide-list-checks"
    subtitle="整合一般任務、活動匯款與收據追蹤，集中處理財務待辦。"
    title="統一任務清單"
  >
    <template #actions>
      <Select
        v-model:value="currentYear"
        :options="yearOptions.map((y) => ({ label: `${y}年`, value: y }))"
        style="width: 108px"
      />
      <Button type="primary" @click="openCreateModal">
        <template #icon><span class="i-lucide-plus" /></template>
        新增任務
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
      <div class="tsf-filter-group">
        <span class="tsf-filter-label">狀態</span>
        <Segmented
          v-model:value="statusSegmentedValue"
          :options="statusFilterOptions"
        />
      </div>
      <div class="tsf-filter-group">
        <span class="tsf-filter-label">類型</span>
        <Segmented
          v-model:value="typeSegmentedValue"
          :options="typeFilterOptions"
        />
      </div>
    </template>

    <Spin :spinning="loading">
      <div class="tsf-kpi-grid tsf-kpi-grid--three">
        <TsfMetricCard
          icon="i-lucide-circle-alert"
          label="待處理"
          tone="danger"
          :value="pendingCount"
        />
        <TsfMetricCard
          icon="i-lucide-loader-circle"
          label="進行中"
          tone="info"
          :value="inProgressCount"
        />
        <TsfMetricCard
          icon="i-lucide-circle-check"
          label="已完成"
          tone="success"
          :value="completedCount"
        />
      </div>

      <section class="tsf-table-panel mt-4">
        <div class="tsf-section-header">
          <div class="tsf-section-title-group">
            <span class="tsf-section-kicker">任務台帳</span>
            <h3 class="tsf-section-title">任務處理明細</h3>
            <p class="tsf-section-description">
              依類型、優先度、狀態與部門整理，方便快速判斷待處理事項。
            </p>
          </div>
          <div class="tsf-section-meta">
            <span class="tsf-meta-pill">
              {{ currentMonth === undefined ? '全年' : `${currentMonth} 月` }}
            </span>
            <span class="tsf-meta-pill">共 {{ tasks.length }} 筆</span>
          </div>
        </div>

        <div class="tsf-table-body">
          <Table
            :columns="columns"
            :data-source="tasks"
            :loading="loading"
            :pagination="{
              pageSize: 50,
              showTotal: (total: number) => `共 ${total} 筆`,
            }"
            row-key="id"
            :scroll="{ x: 900 }"
            size="middle"
          >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'type'">
              <Tag
                :color="
                  typeColors[(record as UnifiedTaskItem).type] ?? 'default'
                "
              >
                {{ typeLabels[(record as UnifiedTaskItem).type] ?? '—' }}
              </Tag>
            </template>

            <template v-if="column.key === 'priority'">
              <Tag
                :color="
                  priorityColors[(record as UnifiedTaskItem).priority] ??
                  'default'
                "
              >
                {{
                  priorityLabels[(record as UnifiedTaskItem).priority] ?? '—'
                }}
              </Tag>
            </template>

            <template v-if="column.key === 'status'">
              <Tag
                :color="
                  statusColors[(record as UnifiedTaskItem).status] ?? 'default'
                "
              >
                {{
                  statusLabels[(record as UnifiedTaskItem).status] ?? '—'
                }}
              </Tag>
            </template>

            <template v-if="column.dataIndex === 'departmentName'">
              <Tag
                v-if="(record as UnifiedTaskItem).departmentName"
                color="blue"
              >
                {{ (record as UnifiedTaskItem).departmentName }}
              </Tag>
              <span v-else class="text-gray-300">—</span>
            </template>

            <template v-if="column.dataIndex === 'dueDate'">
              {{ formatDate((record as UnifiedTaskItem).dueDate) }}
            </template>

            <template v-if="column.dataIndex === 'createdAt'">
              {{ formatDate((record as UnifiedTaskItem).createdAt) }}
            </template>

            <template v-if="column.key === 'action'">
              <!-- 一般任務操作 -->
              <template v-if="(record as UnifiedTaskItem).type === 0">
                <Button
                  v-if="(record as UnifiedTaskItem).status !== 2"
                  size="small"
                  type="link"
                  @click="openEditModal(record as UnifiedTaskItem)"
                >
                  編輯
                </Button>
                <Popconfirm
                  v-if="(record as UnifiedTaskItem).status !== 2"
                  title="確認完成此任務？"
                  @confirm="
                    handleCompleteGeneral((record as UnifiedTaskItem).id)
                  "
                >
                  <Button size="small" type="link">完成</Button>
                </Popconfirm>
                <Popconfirm
                  title="確認刪除此任務？"
                  @confirm="
                    handleDeleteGeneral((record as UnifiedTaskItem).id)
                  "
                >
                  <Button danger size="small" type="link">刪除</Button>
                </Popconfirm>
              </template>
              <!-- 匯款任務操作 -->
              <template v-else-if="(record as UnifiedTaskItem).type === 1">
                <Button
                  v-if="(record as UnifiedTaskItem).status === 0"
                  size="small"
                  type="link"
                  @click="
                    openCompleteRemittanceModal(
                      (record as UnifiedTaskItem).sourceId ?? (record as UnifiedTaskItem).id,
                    )
                  "
                >
                  標記完成
                </Button>
                <Tag v-else color="success">已匯款</Tag>
              </template>
              <!-- 收據任務操作 -->
              <template v-else-if="(record as UnifiedTaskItem).type === 2">
                <Button
                  v-if="(record as UnifiedTaskItem).status === 0"
                  size="small"
                  type="link"
                  @click="
                    handleToggleReceipt(
                      (record as UnifiedTaskItem).sourceId ?? (record as UnifiedTaskItem).id,
                      'receiptCollected',
                    )
                  "
                >
                  回收
                </Button>
                <Button
                  v-if="(record as UnifiedTaskItem).status === 1"
                  size="small"
                  type="link"
                  @click="
                    handleToggleReceipt(
                      (record as UnifiedTaskItem).sourceId ?? (record as UnifiedTaskItem).id,
                      'receiptMailed',
                    )
                  "
                >
                  寄送
                </Button>
                <Tag v-if="(record as UnifiedTaskItem).status === 2" color="success">
                  已完成
                </Tag>
              </template>
            </template>
          </template>

          <template #emptyText>
            <div class="py-8 text-center text-gray-400">
              目前沒有待處理的任務
            </div>
          </template>
          </Table>
        </div>
      </section>
    </Spin>

    <!-- 新增/編輯一般任務 Modal -->
    <Modal
      v-model:open="modalVisible"
      :confirm-loading="submitting"
      :title="modalTitle"
      @ok="handleSubmit"
    >
      <Form layout="vertical">
        <FormItem label="標題" required>
          <Input
            v-model:value="formState.title"
            placeholder="請輸入任務標題"
          />
        </FormItem>
        <FormItem label="說明">
          <Textarea
            v-model:value="formState.description"
            :rows="3"
            placeholder="請輸入任務說明（選填）"
          />
        </FormItem>
        <Row :gutter="16">
          <Col :span="12">
            <FormItem label="優先度">
              <Select v-model:value="formState.priority">
                <SelectOption :value="0">高</SelectOption>
                <SelectOption :value="1">中</SelectOption>
                <SelectOption :value="2">低</SelectOption>
              </Select>
            </FormItem>
          </Col>
          <Col :span="12">
            <FormItem label="截止日">
              <DatePicker
                :value="
                  formState.dueDate ? dayjs(formState.dueDate) : undefined
                "
                style="width: 100%"
                @change="
                  (_: unknown, dateString: string | string[]) =>
                    (formState.dueDate =
                      (Array.isArray(dateString)
                        ? dateString[0]
                        : dateString) || undefined)
                "
              />
            </FormItem>
          </Col>
        </Row>
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
      </Form>
    </Modal>

    <!-- 匯款完成 Modal -->
    <Modal
      v-model:open="completeRemittanceVisible"
      :confirm-loading="completeRemittanceSubmitting"
      title="確認匯款完成"
      @ok="handleCompleteRemittance"
    >
      <Form layout="vertical">
        <FormItem label="銀行" required>
          <Select
            v-model:value="completeFormState.bankName"
            placeholder="請選擇銀行"
          >
            <SelectOption
              v-for="bank in bankOptions"
              :key="bank"
              :value="bank"
            >
              {{ bank }}
            </SelectOption>
          </Select>
        </FormItem>
        <FormItem label="交易日期" required>
          <DatePicker
            :value="dayjs(completeFormState.transactionDate)"
            style="width: 100%"
            @change="
              (_: unknown, dateString: string | string[]) =>
                (completeFormState.transactionDate =
                  (Array.isArray(dateString)
                    ? dateString[0]
                    : dateString) || dayjs().format('YYYY-MM-DD'))
            "
          />
        </FormItem>
      </Form>
    </Modal>
  </TsfGlassPage>
</template>
