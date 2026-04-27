<script lang="ts" setup>
import { computed, onMounted, ref, watch } from 'vue';

import {
  message,
  Segmented,
  Select,
  Spin,
  Switch,
  Table,
  Tag,
} from 'ant-design-vue';
import dayjs from 'dayjs';

import type { ReceiptTrackingResponse } from '#/api';

import { getReceiptTrackingApi, patchReceiptStatusApi } from '#/api';

import TsfGlassPage from '../_shared/TsfGlassPage.vue';
import TsfMetricCard from '../_shared/TsfMetricCard.vue';

defineOptions({ name: 'TsfReceiptTrackingPage' });

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

// ── 篩選類型 ────────────────────────────────────

type FilterType = 'all' | 'not-collected' | 'not-mailed';
const filterType = ref<FilterType>('all');

const filterSegmentedOptions = ['全部', '未回收', '未寄送'];
const filterSegmentedValue = computed({
  get: () => {
    const map: Record<FilterType, string> = {
      'all': '全部',
      'not-collected': '未回收',
      'not-mailed': '未寄送',
    };
    return map[filterType.value];
  },
  set: (val: string) => {
    const map: Record<string, FilterType> = {
      '全部': 'all',
      '未回收': 'not-collected',
      '未寄送': 'not-mailed',
    };
    filterType.value = map[val] ?? 'all';
  },
});

// ── 資料 ────────────────────────────────────────

const loading = ref(false);
const totalCount = ref(0);
const notCollectedCount = ref(0);
const notMailedCount = ref(0);
const allItems = ref<ReceiptTrackingResponse[]>([]);
const updatingIds = ref(new Set<string>());

const filteredItems = computed(() => {
  if (filterType.value === 'not-collected') {
    return allItems.value.filter((i) => !i.receiptCollected);
  }
  if (filterType.value === 'not-mailed') {
    return allItems.value.filter((i) => !i.receiptMailed);
  }
  return allItems.value;
});

async function fetchData() {
  loading.value = true;
  try {
    const result = await getReceiptTrackingApi(
      currentYear.value,
      currentMonth.value,
    );
    totalCount.value = result.totalCount;
    notCollectedCount.value = result.notCollectedCount;
    notMailedCount.value = result.notMailedCount;
    allItems.value = result.items;
  } finally {
    loading.value = false;
  }
}

watch([currentYear, currentMonth], () => fetchData());
onMounted(fetchData);

// ── 表格欄位 ────────────────────────────────────

const columns = [
  { dataIndex: 'bankName', title: '銀行', width: 120 },
  { dataIndex: 'transactionDate', title: '日期', width: 110 },
  { dataIndex: 'description', title: '摘要', ellipsis: true },
  { dataIndex: 'departmentName', title: '部門', width: 120 },
  { dataIndex: 'amount', title: '金額', width: 120, align: 'right' as const },
  { key: 'receiptCollected', title: '已回收', width: 90, align: 'center' as const },
  { key: 'receiptMailed', title: '已寄送', width: 90, align: 'center' as const },
];

// ── 金額格式化 ──────────────────────────────────

function formatCurrency(val: number): string {
  return val.toLocaleString('zh-TW', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  });
}

// ── 內聯更新收據狀態 ─────────────────────────────

async function handleToggleReceiptStatus(
  record: ReceiptTrackingResponse,
  field: 'receiptCollected' | 'receiptMailed',
  newValue: boolean,
) {
  const key = `${record.id}-${field}`;
  updatingIds.value = new Set([...updatingIds.value, key]);
  // 先更新本地狀態（樂觀更新）
  const item = allItems.value.find((i) => i.id === record.id);
  if (item) {
    if (field === 'receiptCollected') item.receiptCollected = newValue;
    else item.receiptMailed = newValue;
  }
  try {
    await patchReceiptStatusApi(record.id, {
      receiptCollected: field === 'receiptCollected' ? newValue : undefined,
      receiptMailed: field === 'receiptMailed' ? newValue : undefined,
    });
    message.success('收據狀態已更新');
    await fetchData();
  } catch {
    // 還原本地狀態
    if (item) {
      if (field === 'receiptCollected') item.receiptCollected = !newValue;
      else item.receiptMailed = !newValue;
    }
    message.error('更新失敗');
  } finally {
    updatingIds.value = new Set([...updatingIds.value].filter((k) => k !== key));
  }
}
</script>

<template>
  <TsfGlassPage
    icon="i-lucide-receipt-text"
    subtitle="集中檢查銀行支出的收據回收與寄送狀態。"
    title="收據追蹤"
  >
    <template #actions>
      <Select
        v-model:value="currentYear"
        :options="yearOptions.map((y) => ({ label: `${y}年`, value: y }))"
        style="width: 108px"
      />
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
        <span class="tsf-filter-label">追蹤</span>
        <Segmented
          v-model:value="filterSegmentedValue"
          :options="filterSegmentedOptions"
        />
      </div>
    </template>

    <Spin :spinning="loading">
      <div class="tsf-kpi-grid tsf-kpi-grid--three">
        <TsfMetricCard
          icon="i-lucide-list-todo"
          label="待處理總數"
          tone="info"
          :value="totalCount"
        />
        <TsfMetricCard
          icon="i-lucide-hand"
          label="未回收"
          tone="danger"
          :value="notCollectedCount"
        />
        <TsfMetricCard
          icon="i-lucide-mail-warning"
          label="未寄送"
          tone="warning"
          :value="notMailedCount"
        />
      </div>

      <section class="tsf-table-panel mt-4">
        <Table
          :columns="columns"
          :data-source="filteredItems"
          :loading="loading"
          :pagination="{ pageSize: 50, showTotal: (total: number) => `共 ${total} 筆` }"
          row-key="id"
          :scroll="{ x: 820 }"
          size="middle"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.dataIndex === 'bankName'">
              <Tag :color="(record as ReceiptTrackingResponse).bankName.includes('上海') ? 'blue' : 'green'">
                {{ (record as ReceiptTrackingResponse).bankName }}
              </Tag>
            </template>

            <template v-if="column.dataIndex === 'departmentName'">
              <Tag v-if="(record as ReceiptTrackingResponse).departmentName" color="blue">
                {{ (record as ReceiptTrackingResponse).departmentName }}
              </Tag>
            </template>

            <template v-if="column.dataIndex === 'amount'">
              {{ formatCurrency((record as ReceiptTrackingResponse).amount) }}
            </template>

            <template v-if="column.key === 'receiptCollected'">
              <Switch
                :checked="(record as ReceiptTrackingResponse).receiptCollected"
                :loading="updatingIds.has(`${(record as ReceiptTrackingResponse).id}-receiptCollected`)"
                size="small"
                @change="(val) => handleToggleReceiptStatus(record as unknown as ReceiptTrackingResponse, 'receiptCollected', val as boolean)"
              />
            </template>

            <template v-if="column.key === 'receiptMailed'">
              <Switch
                :checked="(record as ReceiptTrackingResponse).receiptMailed"
                :loading="updatingIds.has(`${(record as ReceiptTrackingResponse).id}-receiptMailed`)"
                size="small"
                @change="(val) => handleToggleReceiptStatus(record as unknown as ReceiptTrackingResponse, 'receiptMailed', val as boolean)"
              />
            </template>
          </template>

          <template #emptyText>
            <div class="py-8 text-center text-gray-400">
              本期間所有收據均已處理完成
            </div>
          </template>
        </Table>
      </section>
    </Spin>
  </TsfGlassPage>
</template>
