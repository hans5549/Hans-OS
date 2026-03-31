<script lang="ts" setup>
import { computed, onMounted, ref, watch } from 'vue';

import { Page } from '@vben/common-ui';

import {
  Card,
  Col,
  Row,
  Segmented,
  Select,
  Spin,
  Statistic,
  Table,
  Tag,
} from 'ant-design-vue';
import dayjs from 'dayjs';

import type { ReceiptTrackingResponse } from '#/api';

import { getReceiptTrackingApi } from '#/api';
import { useRouter } from 'vue-router';

defineOptions({ name: 'TsfReceiptTrackingPage' });

const router = useRouter();

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
  { key: 'receiptCollected', title: '已回收', width: 80, align: 'center' as const },
  { key: 'receiptMailed', title: '已寄送', width: 80, align: 'center' as const },
];

// ── 金額格式化 ──────────────────────────────────

function formatCurrency(val: number): string {
  return val.toLocaleString('zh-TW', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

// ── 跳轉到收支表 ────────────────────────────────

function navigateToBank(record: ReceiptTrackingResponse) {
  const bankPath = record.bankName.includes('上海')
    ? '/taiwan-sports-finance/shanghai-bank'
    : '/taiwan-sports-finance/tcb-bank';
  router.push(bankPath);
}
</script>

<template>
  <Page content-class="p-0" title="收據追蹤">
    <Card :body-style="{ padding: '16px 24px' }">
      <!-- 頂部控制列 -->
      <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div class="flex items-center gap-3">
          <span class="text-2xl">🧾</span>
          <span class="text-lg font-medium">收據追蹤</span>
        </div>
        <Select
          v-model:value="currentYear"
          :options="yearOptions.map((y) => ({ label: `${y}年`, value: y }))"
          style="width: 100px"
        />
      </div>

      <!-- 月份切換器 -->
      <div class="mb-4">
        <Segmented
          v-model:value="monthSegmentedValue"
          :options="segmentedOptions"
          block
        />
      </div>

      <!-- 統計卡片 -->
      <Spin :spinning="loading">
        <Row :gutter="16" class="mb-4">
          <Col :md="8" :xs="24">
            <Card size="small">
              <Statistic
                title="待處理總數"
                :value="totalCount"
                :value-style="{ color: '#1677ff' }"
              />
            </Card>
          </Col>
          <Col :md="8" :xs="24">
            <Card size="small">
              <Statistic
                title="未回收"
                :value="notCollectedCount"
                :value-style="{ color: '#cf1322' }"
              />
            </Card>
          </Col>
          <Col :md="8" :xs="24">
            <Card size="small">
              <Statistic
                title="未寄送"
                :value="notMailedCount"
                :value-style="{ color: '#fa8c16' }"
              />
            </Card>
          </Col>
        </Row>

        <!-- 篩選類型切換 -->
        <div class="mb-3">
          <Segmented
            v-model:value="filterSegmentedValue"
            :options="filterSegmentedOptions"
          />
        </div>

        <!-- 追蹤表格 -->
        <Table
          :columns="columns"
          :data-source="filteredItems"
          :loading="loading"
          :pagination="{ pageSize: 50, showTotal: (total: number) => `共 ${total} 筆` }"
          row-key="id"
          size="middle"
          :custom-row="(record: ReceiptTrackingResponse) => ({
            onClick: () => navigateToBank(record),
            style: 'cursor: pointer',
          })"
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
              <Tag :color="(record as ReceiptTrackingResponse).receiptCollected ? 'green' : 'red'">
                {{ (record as ReceiptTrackingResponse).receiptCollected ? '已回收' : '未回收' }}
              </Tag>
            </template>

            <template v-if="column.key === 'receiptMailed'">
              <Tag :color="(record as ReceiptTrackingResponse).receiptMailed ? 'green' : 'orange'">
                {{ (record as ReceiptTrackingResponse).receiptMailed ? '已寄送' : '未寄送' }}
              </Tag>
            </template>
          </template>

          <template #emptyText>
            <div class="py-8 text-center text-gray-400">
              🎉 本期間所有收據均已處理完成
            </div>
          </template>
        </Table>
      </Spin>
    </Card>
  </Page>
</template>
