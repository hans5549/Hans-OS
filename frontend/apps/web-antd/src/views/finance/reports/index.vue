<script setup lang="ts">
import type { EchartsUIType } from '@vben/plugins/echarts';

import { computed, onMounted, ref, watch } from 'vue';

import { Page } from '@vben/common-ui';
import { EchartsUI, useEcharts } from '@vben/plugins/echarts';
import { Card, DatePicker, Spin, Tabs, TabPane } from 'ant-design-vue';
import dayjs from 'dayjs';

import type {
  CategoryBreakdownItem,
  CategoryBreakdownResponse,
  TrendResponse,
} from '#/api';

import { getCategoryBreakdownApi, getTrendsApi } from '#/api';

defineOptions({ name: 'FinanceReportsPage' });

// ── State ────────────────────────────────────────

const loading = ref(false);
const trendData = ref<TrendResponse | null>(null);
const breakdownData = ref<CategoryBreakdownResponse | null>(null);

const currentMonth = ref(dayjs());
const breakdownType = ref<'Expense' | 'Income'>('Expense');

// ── Chart Refs ───────────────────────────────────

const trendChartRef = ref<EchartsUIType>();
const { renderEcharts: renderTrend } = useEcharts(trendChartRef);

const breakdownChartRef = ref<EchartsUIType>();
const { renderEcharts: renderBreakdown } = useEcharts(breakdownChartRef);

// ── Computed ─────────────────────────────────────

const displayYear = computed(() => currentMonth.value.year());
const displayMonth = computed(() => currentMonth.value.month() + 1);

const trendRange = computed(() => {
  const end = currentMonth.value;
  const start = end.subtract(5, 'month');
  return {
    startYear: start.year(),
    startMonth: start.month() + 1,
    endYear: end.year(),
    endMonth: end.month() + 1,
  };
});

// ── Helpers ──────────────────────────────────────

function formatAmount(amount: number): string {
  return amount.toLocaleString('zh-TW');
}

// ── Data Fetching ────────────────────────────────

async function fetchTrendData() {
  const { startYear, startMonth, endYear, endMonth } = trendRange.value;
  trendData.value = await getTrendsApi(
    startYear,
    startMonth,
    endYear,
    endMonth,
  );
  renderTrendChart();
}

async function fetchBreakdownData() {
  breakdownData.value = await getCategoryBreakdownApi(
    displayYear.value,
    displayMonth.value,
    breakdownType.value,
  );
  renderBreakdownChart();
}

async function fetchAll() {
  loading.value = true;
  try {
    await Promise.all([fetchTrendData(), fetchBreakdownData()]);
  } finally {
    loading.value = false;
  }
}

// ── Chart Rendering ──────────────────────────────

function renderTrendChart() {
  if (!trendData.value) return;

  const months = trendData.value.months;
  const xLabels = months.map((m) => `${m.year}/${String(m.month).padStart(2, '0')}`);

  renderTrend({
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' },
      formatter(params: unknown) {
        const items = params as Array<{
          seriesName: string;
          value: number;
          marker: string;
        }>;
        let html = `<div style="font-weight:600">${items[0] ? xLabels[0] : ''}</div>`;
        if (Array.isArray(items)) {
          const idx = (params as Array<{ dataIndex: number }>)[0]?.dataIndex ?? 0;
          html = `<div style="font-weight:600">${xLabels[idx]}</div>`;
          for (const item of items) {
            html += `<div>${item.marker} ${item.seriesName}: ${formatAmount(item.value)}</div>`;
          }
        }
        return html;
      },
    },
    legend: {
      data: ['收入', '支出', '結餘'],
      bottom: 0,
    },
    grid: {
      top: 20,
      left: '3%',
      right: '3%',
      bottom: 40,
      containLabel: true,
    },
    xAxis: {
      type: 'category',
      data: xLabels,
      axisTick: { show: false },
    },
    yAxis: {
      type: 'value',
      axisLabel: {
        formatter: (v: number) =>
          v >= 10_000 ? `${(v / 10_000).toFixed(0)}萬` : `${v}`,
      },
    },
    series: [
      {
        name: '收入',
        type: 'bar',
        data: months.map((m) => m.totalIncome),
        itemStyle: { color: '#52c41a' },
        barMaxWidth: 30,
      },
      {
        name: '支出',
        type: 'bar',
        data: months.map((m) => m.totalExpense),
        itemStyle: { color: '#ff4d4f' },
        barMaxWidth: 30,
      },
      {
        name: '結餘',
        type: 'line',
        data: months.map((m) => m.balance),
        itemStyle: { color: '#1890ff' },
        smooth: true,
      },
    ],
  });
}

function renderBreakdownChart() {
  if (!breakdownData.value || breakdownData.value.items.length === 0) {
    renderBreakdown({
      title: {
        text: '本月無資料',
        left: 'center',
        top: 'center',
        textStyle: { color: '#999', fontSize: 14 },
      },
    });
    return;
  }

  const items: CategoryBreakdownItem[] = breakdownData.value.items;
  const colorPalette = [
    '#5470c6', '#91cc75', '#fac858', '#ee6666', '#73c0de',
    '#3ba272', '#fc8452', '#9a60b4', '#ea7ccc', '#ff9f7f',
  ];

  renderBreakdown({
    tooltip: {
      trigger: 'item',
      formatter(params: unknown) {
        const p = params as {
          name: string;
          value: number;
          percent: number;
          data: { count: number };
        };
        return `${p.name}<br/>金額: ${formatAmount(p.value)}<br/>佔比: ${p.percent.toFixed(1)}%<br/>筆數: ${p.data.count}`;
      },
    },
    legend: {
      orient: 'vertical',
      right: 10,
      top: 'center',
      type: 'scroll',
    },
    series: [
      {
        name: breakdownType.value === 'Expense' ? '支出分類' : '收入分類',
        type: 'pie',
        radius: ['40%', '70%'],
        center: ['35%', '50%'],
        avoidLabelOverlap: true,
        itemStyle: {
          borderRadius: 6,
          borderColor: '#fff',
          borderWidth: 2,
        },
        label: {
          show: false,
        },
        emphasis: {
          label: {
            show: true,
            fontSize: 14,
            fontWeight: 'bold',
          },
        },
        data: items.map((item, i) => ({
          value: Number(item.amount),
          name: item.categoryName,
          count: item.transactionCount,
          itemStyle: { color: colorPalette[i % colorPalette.length] },
        })),
      },
    ],
  });
}

// ── Event Handlers ───────────────────────────────

function onMonthChange(value: dayjs.Dayjs | string) {
  currentMonth.value = dayjs(value);
}

function onBreakdownTypeChange(key: string | number) {
  breakdownType.value = key as 'Expense' | 'Income';
  fetchBreakdownData();
}

// ── Watchers & Lifecycle ─────────────────────────

watch(currentMonth, fetchAll);

onMounted(fetchAll);
</script>

<template>
  <Page auto-content-height>
    <!-- Month Selector -->
    <div class="mb-4 flex items-center justify-center">
      <DatePicker
        :allow-clear="false"
        :value="currentMonth"
        picker="month"
        style="width: 180px"
        @change="onMonthChange"
      />
    </div>

    <Spin :spinning="loading">
      <!-- Trend Chart -->
      <Card class="mb-4" title="收支趨勢（近 6 個月）">
        <EchartsUI ref="trendChartRef" :style="{ height: '320px' }" />
      </Card>

      <!-- Category Breakdown -->
      <Card>
        <template #title>
          <div class="flex items-center justify-between">
            <span>分類佔比</span>
            <Tabs
              :active-key="breakdownType"
              class="!mb-0"
              size="small"
              @change="onBreakdownTypeChange"
            >
              <TabPane key="Expense" tab="支出" />
              <TabPane key="Income" tab="收入" />
            </Tabs>
          </div>
        </template>

        <EchartsUI ref="breakdownChartRef" :style="{ height: '360px' }" />

        <!-- Breakdown List -->
        <div
          v-if="breakdownData && breakdownData.items.length > 0"
          class="mt-4 border-t pt-4"
        >
          <div class="mb-2 text-sm font-semibold text-gray-500">
            合計：{{ formatAmount(breakdownData.total) }}
          </div>
          <div class="flex flex-col gap-2">
            <div
              v-for="item in breakdownData.items"
              :key="item.categoryId"
              class="flex items-center justify-between rounded px-2 py-1 hover:bg-gray-50 dark:hover:bg-gray-800"
            >
              <div class="flex items-center gap-2">
                <span
                  v-if="item.categoryIcon"
                  :class="item.categoryIcon"
                  class="text-base text-gray-500"
                />
                <span class="text-sm">{{ item.categoryName }}</span>
                <span class="text-xs text-gray-400">
                  {{ item.transactionCount }}筆
                </span>
              </div>
              <div class="flex items-center gap-3">
                <span class="text-sm font-medium">
                  {{ formatAmount(item.amount) }}
                </span>
                <span class="w-12 text-right text-xs text-gray-400">
                  {{ item.percentage.toFixed(1) }}%
                </span>
              </div>
            </div>
          </div>
        </div>
      </Card>
    </Spin>
  </Page>
</template>
