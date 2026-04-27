<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';

import {
  Button,
  InputNumber,
  message,
  Select,
  SelectOption,
  Table,
} from 'ant-design-vue';

import type {
  AnnualBudgetOverviewResponse,
  DepartmentBudgetSummaryResponse,
} from '#/api';

import {
  getAnnualBudgetOverviewApi,
  updateBudgetStatusApi,
  updateGrantedBudgetApi,
} from '#/api';

import TsfGlassPage from '../_shared/TsfGlassPage.vue';
import TsfMetricCard from '../_shared/TsfMetricCard.vue';

import DepartmentBudgetDrawer from './components/DepartmentBudgetDrawer.vue';
import SharePopover from './components/SharePopover.vue';

defineOptions({ name: 'TsfAnnualBudgetPage' });

const currentYear = new Date().getFullYear();
const selectedYear = ref(currentYear);
const loading = ref(false);
const overview = ref<AnnualBudgetOverviewResponse | null>(null);
const grantedBudgetInput = ref<number | undefined>(undefined);
const savingGrantedBudget = ref(false);

// Drawer 狀態
const drawerOpen = ref(false);
const drawerDeptId = ref('');
const drawerDeptName = ref('');

const statusMap: Record<string, { color: string; label: string }> = {
  Draft: { color: 'default', label: '草稿' },
  Submitted: { color: 'blue', label: '已提交' },
  Approved: { color: 'green', label: '已核准' },
  Settled: { color: 'purple', label: '已結算' },
};

const formatExecutionRate = (
  actualAmount: number,
  allocatedAmount: number | null,
) =>
  allocatedAmount == null || allocatedAmount === 0
    ? '—'
    : `${((actualAmount / allocatedAmount) * 100).toFixed(1)}%`;

const formatBudgetAmount = (amount: number | null | undefined) =>
  amount != null ? amount.toLocaleString('zh-TW') : '—';

const formatBudgetInput = (value: string | number | undefined) =>
  value != null ? `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',') : '';

const parseBudgetInput = (value: string | undefined) =>
  value?.replace(/,/g, '') ?? '';

const handleStatusSelect = (value: unknown) => handleStatusChange(String(value));

const totalExecutionRate = computed(() =>
  overview.value
    ? formatExecutionRate(overview.value.totalActual, overview.value.grantedBudget)
    : '—',
);

const columns = [
  { dataIndex: 'departmentName', title: '部門名稱', width: 180 },
  {
    align: 'right' as const,
    customRender: ({ record }: { record: DepartmentBudgetSummaryResponse }) =>
      record.budgetAmount.toLocaleString('zh-TW'),
    dataIndex: 'budgetAmount',
    title: '需求預算',
    width: 140,
  },
  {
    align: 'right' as const,
    customRender: ({ record }: { record: DepartmentBudgetSummaryResponse }) =>
      formatBudgetAmount(record.allocatedAmount),
    dataIndex: 'allocatedAmount',
    title: '核定預算',
    width: 140,
  },
  {
    align: 'right' as const,
    customRender: ({ record }: { record: DepartmentBudgetSummaryResponse }) =>
      record.actualAmount.toLocaleString('zh-TW'),
    dataIndex: 'actualAmount',
    title: '實際金額',
    width: 140,
  },
  {
    align: 'right' as const,
    customRender: ({ record }: { record: DepartmentBudgetSummaryResponse }) =>
      formatExecutionRate(record.actualAmount, record.allocatedAmount),
    key: 'executionRate',
    title: '執行率',
    width: 100,
  },
  {
    align: 'center' as const,
    dataIndex: 'itemCount',
    title: '項目數',
    width: 80,
  },
  {
    align: 'center' as const,
    fixed: 'right' as const,
    key: 'action',
    title: '操作',
    width: 120,
  },
];

async function fetchOverview() {
  loading.value = true;
  try {
    const annualBudgetOverview = await getAnnualBudgetOverviewApi(
      selectedYear.value,
    );
    overview.value = annualBudgetOverview;
    grantedBudgetInput.value = annualBudgetOverview.grantedBudget ?? undefined;
  } finally {
    loading.value = false;
  }
}

async function saveGrantedBudget() {
  if (grantedBudgetInput.value == null || grantedBudgetInput.value < 0) {
    message.warning('請輸入有效的核定總預算金額');
    return;
  }
  savingGrantedBudget.value = true;
  try {
    overview.value = await updateGrantedBudgetApi(selectedYear.value, {
      grantedBudget: grantedBudgetInput.value,
    });
    message.success('核定總預算已儲存');
  } catch {
    message.error('核定總預算儲存失敗');
  } finally {
    savingGrantedBudget.value = false;
  }
}

function openDrawer({
  departmentId,
  departmentName,
}: DepartmentBudgetSummaryResponse) {
  drawerDeptId.value = departmentId;
  drawerDeptName.value = departmentName;
  drawerOpen.value = true;
}

function handleDrawerClose() {
  drawerOpen.value = false;
}

const handleDrawerSaved = () => fetchOverview();

async function handleStatusChange(value: string) {
  if (value === overview.value?.status) return;
  try {
    await updateBudgetStatusApi(selectedYear.value, { status: value });
    message.success('狀態已更新');
  } catch {
    // 全域攔截器處理 toast，不需要額外處理
  } finally {
    await fetchOverview();
  }
}

watch(selectedYear, fetchOverview);

onMounted(fetchOverview);
</script>

<template>
  <TsfGlassPage
    icon="i-lucide-chart-no-axes-combined"
    subtitle="年度需求、核定與實際執行金額的部門級總覽。"
    title="年度預算"
  >
    <template #actions>
      <div v-if="overview" class="tsf-action-group">
        <span class="tsf-filter-label">核定總預算</span>
        <InputNumber
          v-model:value="grantedBudgetInput"
          :formatter="formatBudgetInput"
          :min="0"
          :parser="parseBudgetInput"
          placeholder="請輸入核定總預算"
          style="width: 200px"
        />
        <Button
          :loading="savingGrantedBudget"
          size="small"
          type="primary"
          @click="saveGrantedBudget"
        >
          儲存
        </Button>
      </div>
    </template>

    <template #filters>
      <div class="tsf-filter-group">
        <span class="tsf-filter-label">年度</span>
        <InputNumber
          v-model:value="selectedYear"
          :max="currentYear + 5"
          :min="2020"
          style="width: 108px"
        />
      </div>
      <div v-if="overview" class="tsf-filter-group">
        <span class="tsf-filter-label">狀態</span>
        <span class="tsf-status-pill">
          <span class="i-lucide-circle-dot" aria-hidden="true" />
          {{ statusMap[overview.status]?.label ?? overview.status }}
        </span>
        <Select
          :value="overview.status"
          style="width: 128px"
          @change="handleStatusSelect"
        >
          <SelectOption
            v-for="(info, key) in statusMap"
            :key="key"
            :value="key"
          >
            {{ info.label }}
          </SelectOption>
        </Select>
      </div>
    </template>

    <div class="tsf-kpi-grid">
      <TsfMetricCard
        icon="i-lucide-file-spreadsheet"
        label="需求預算"
        prefix="$"
        tone="info"
        :value="formatBudgetAmount(overview?.totalBudget)"
      />
      <TsfMetricCard
        icon="i-lucide-badge-dollar-sign"
        label="核定預算"
        prefix="$"
        tone="success"
        :value="formatBudgetAmount(overview?.grantedBudget)"
      />
      <TsfMetricCard
        icon="i-lucide-receipt-text"
        label="實際金額"
        prefix="$"
        tone="warning"
        :value="formatBudgetAmount(overview?.totalActual)"
      />
      <TsfMetricCard
        icon="i-lucide-gauge"
        label="執行率"
        tone="neutral"
        :value="totalExecutionRate"
      />
    </div>

    <section class="tsf-table-panel">
      <Table
        :columns="columns"
        :data-source="overview?.departments ?? []"
        :loading="loading"
        :pagination="false"
        row-key="departmentBudgetId"
        size="middle"
        :scroll="{ x: 860 }"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'action'">
            <Button
              size="small"
              type="link"
              @click="openDrawer(record as unknown as DepartmentBudgetSummaryResponse)"
            >
              編輯
            </Button>
            <SharePopover
              :department-id="(record as unknown as DepartmentBudgetSummaryResponse).departmentId"
              :department-name="(record as unknown as DepartmentBudgetSummaryResponse).departmentName"
            />
          </template>
        </template>

        <template #summary>
          <tr v-if="overview" class="font-semibold">
            <td>合計</td>
            <td class="text-right">
              {{ overview.totalBudget.toLocaleString('zh-TW') }}
            </td>
            <td class="text-right">
              {{ formatBudgetAmount(overview.grantedBudget) }}
            </td>
            <td class="text-right">
              {{ overview.totalActual.toLocaleString('zh-TW') }}
            </td>
            <td class="text-right">
              {{ formatExecutionRate(overview.totalActual, overview.grantedBudget) }}
            </td>
            <td />
            <td />
          </tr>
        </template>

        <template #emptyText>
          <div class="py-8 text-center text-gray-400">
            尚無部門資料，請先至「設定」新增體育部門
          </div>
        </template>
      </Table>
    </section>

    <!-- 部門預算編輯 Drawer -->
    <DepartmentBudgetDrawer
      :department-id="drawerDeptId"
      :department-name="drawerDeptName"
      :open="drawerOpen"
      :year="selectedYear"
      @close="handleDrawerClose"
      @saved="handleDrawerSaved"
    />
  </TsfGlassPage>
</template>
