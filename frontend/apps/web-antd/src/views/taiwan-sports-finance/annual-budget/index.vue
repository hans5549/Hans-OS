<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';

import { Page } from '@vben/common-ui';

import {
  Button,
  Card,
  InputNumber,
  message,
  Select,
  SelectOption,
  Table,
  Tag,
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

import DepartmentBudgetDrawer from './components/DepartmentBudgetDrawer.vue';

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
  Approved: { color: 'green', label: '已核准' },
  Draft: { color: 'default', label: '草稿' },
  Settled: { color: 'purple', label: '已結算' },
  Submitted: { color: 'blue', label: '已提交' },
};

function formatExecutionRate(actualAmount: number, budgetAmount: number) {
  if (budgetAmount === 0) return '—';
  return `${((actualAmount / budgetAmount) * 100).toFixed(1)}%`;
}

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
      record.allocatedAmount != null
        ? record.allocatedAmount.toLocaleString('zh-TW')
        : '—',
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
      formatExecutionRate(record.actualAmount, record.budgetAmount),
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
    width: 80,
  },
];

async function fetchOverview() {
  loading.value = true;
  try {
    overview.value = await getAnnualBudgetOverviewApi(selectedYear.value);
    grantedBudgetInput.value = overview.value.grantedBudget ?? undefined;
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

function openDrawer(dept: DepartmentBudgetSummaryResponse) {
  drawerDeptId.value = dept.departmentId;
  drawerDeptName.value = dept.departmentName;
  drawerOpen.value = true;
}

function handleDrawerClose() {
  drawerOpen.value = false;
}

async function handleDrawerSaved() {
  await fetchOverview();
}

async function handleStatusChange(value: string) {
  try {
    await updateBudgetStatusApi(selectedYear.value, { status: value });
    message.success('狀態已更新');
    await fetchOverview();
  } catch {
    message.error('狀態更新失敗');
  }
}

watch(selectedYear, fetchOverview);

onMounted(fetchOverview);
</script>

<template>
  <Page auto-content-height>
    <Card>
      <!-- 標題列 -->
      <div class="mb-4 flex items-center justify-between">
        <div class="flex items-center gap-4">
          <h3 class="text-lg font-medium">年度預算</h3>
          <div class="flex items-center gap-2">
            <span class="text-sm text-gray-500">年度：</span>
            <InputNumber
              v-model:value="selectedYear"
              :max="currentYear + 5"
              :min="2020"
              style="width: 100px"
            />
          </div>
          <Tag
            v-if="overview"
            :color="statusMap[overview.status]?.color ?? 'default'"
          >
            {{ statusMap[overview.status]?.label ?? overview.status }}
          </Tag>
        </div>

        <div v-if="overview" class="flex items-center gap-2">
          <span class="text-sm text-gray-500">切換狀態：</span>
          <Select
            :value="overview.status"
            style="width: 120px"
            @change="(v: unknown) => handleStatusChange(String(v))"
          >
            <SelectOption value="Draft">草稿</SelectOption>
            <SelectOption value="Submitted">已提交</SelectOption>
            <SelectOption value="Approved">已核准</SelectOption>
            <SelectOption value="Settled">已結算</SelectOption>
          </Select>
        </div>
      </div>

      <!-- 核定總預算 -->
      <div v-if="overview" class="mb-4 flex items-center gap-4">
        <span class="text-sm text-gray-500">核定總預算：</span>
        <InputNumber
          v-model:value="grantedBudgetInput"
          :formatter="
            (v: string | number | undefined) =>
              v != null ? `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',') : ''
          "
          :min="0"
          :parser="(v: string | undefined) => v?.replace(/,/g, '') ?? ''"
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

      <!-- 總表 -->
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
          </template>
        </template>

        <template #summary>
          <tr v-if="overview" class="font-semibold">
            <td>合計</td>
            <td class="text-right">
              {{ overview.totalBudget.toLocaleString('zh-TW') }}
            </td>
            <td class="text-right">
              {{
                overview.grantedBudget != null
                  ? overview.grantedBudget.toLocaleString('zh-TW')
                  : '—'
              }}
            </td>
            <td class="text-right">
              {{ overview.totalActual.toLocaleString('zh-TW') }}
            </td>
            <td class="text-right">
              <template v-if="overview.totalBudget > 0">
                {{ formatExecutionRate(overview.totalActual, overview.totalBudget) }}
              </template>
              <template v-else>—</template>
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
    </Card>

    <!-- 部門預算編輯 Drawer -->
    <DepartmentBudgetDrawer
      :department-id="drawerDeptId"
      :department-name="drawerDeptName"
      :open="drawerOpen"
      :year="selectedYear"
      @close="handleDrawerClose"
      @saved="handleDrawerSaved"
    />
  </Page>
</template>
