<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';

import { useRoute } from 'vue-router';

import {
  Alert,
  Button,
  Card,
  Col,
  message,
  Row,
  Select,
  Spin,
  Tag,
} from 'ant-design-vue';

import type { PublicBudgetItemInput, PublicBudgetResponse } from '#/api';

import {
  getPublicDepartmentBudgetApi,
  getPublicDepartmentOverviewApi,
  savePublicDepartmentBudgetItemsApi,
} from '#/api';
import type { BudgetYearSummary, DepartmentShareOverviewResponse } from '#/api/core/budget-share';

import type { BudgetItemRow } from './components/BudgetItemsTable.vue';
import BudgetItemsTable from './components/BudgetItemsTable.vue';

defineOptions({ name: 'PublicDepartmentBudgetPage' });

const route = useRoute();
const token = computed(() => route.params.token as string);

// ── 狀態 ─────────────────────────────────────────
const overview = ref<DepartmentShareOverviewResponse | null>(null);
const loadingOverview = ref(false);
const error = ref<string | null>(null);

type ViewMode = 'compare' | 'edit';
const viewMode = ref<ViewMode>('edit');

// 編輯模式 / 比對模式右側 — 當前年度
const selectedYear = ref<number>(new Date().getFullYear());
const budget = ref<PublicBudgetResponse | null>(null);
const items = ref<BudgetItemRow[]>([]);
const loadingBudget = ref(false);
const saving = ref(false);

// 比對模式左側 — 歷史年度
const compareYear = ref<number>();
const compareBudget = ref<PublicBudgetResponse | null>(null);
const compareItems = ref<BudgetItemRow[]>([]);
const loadingCompare = ref(false);

let keyCounter = 0;
const nextKey = () => `row-${++keyCounter}`;

const isEditable = computed(
  () => budget.value?.effectivePermission === 'Editable',
);

const statusMap: Record<string, { color: string; label: string }> = {
  Approved: { color: 'green', label: '已核准' },
  Draft: { color: 'default', label: '草稿' },
  Settled: { color: 'purple', label: '已結算' },
  Submitted: { color: 'blue', label: '已提交' },
};

/** 比對模式左側的年度選項（排除當前選擇的年度） */
const compareYearOptions = computed(() =>
  (overview.value?.availableYears ?? [])
    .filter((y) => y.year !== selectedYear.value)
    .map((y) => ({ label: `${y.year} 年`, value: y.year })),
);

/** 編輯模式的年度選項 */
const yearOptions = computed(() =>
  (overview.value?.availableYears ?? []).map((y) => ({
    label: `${y.year} 年`,
    value: y.year,
  })),
);

// ── 資料載入 ─────────────────────────────────────

function toBudgetItemRows(
  budgetItems: PublicBudgetResponse['items'],
): BudgetItemRow[] {
  return budgetItems.map((item) => ({
    ...item,
    _key: nextKey(),
    note: item.note ?? undefined,
  }));
}

async function fetchOverview() {
  loadingOverview.value = true;
  error.value = null;
  try {
    overview.value = await getPublicDepartmentOverviewApi(token.value);

    // 如果有年度資料，預設選擇最新年度
    if (overview.value.availableYears.length > 0) {
      selectedYear.value = overview.value.availableYears[0]!.year;
      await fetchBudget();
    }
  } catch (e: unknown) {
    const err = e as { response?: { status?: number } };
    if (err.response?.status === 401 || err.response?.status === 403) {
      error.value = '此連結已停用或無權存取';
    } else if (err.response?.status === 404) {
      error.value = '連結不存在或已失效';
    } else {
      error.value = '載入失敗，請稍後再試';
    }
  } finally {
    loadingOverview.value = false;
  }
}

async function fetchBudget() {
  loadingBudget.value = true;
  try {
    budget.value = await getPublicDepartmentBudgetApi(
      token.value,
      selectedYear.value,
    );
    items.value = toBudgetItemRows(budget.value.items);
  } catch {
    budget.value = null;
    items.value = [];
    message.error(`載入 ${selectedYear.value} 年度預算失敗`);
  } finally {
    loadingBudget.value = false;
  }
}

async function fetchCompareBudget() {
  if (compareYear.value === undefined) return;
  loadingCompare.value = true;
  try {
    compareBudget.value = await getPublicDepartmentBudgetApi(
      token.value,
      compareYear.value,
    );
    compareItems.value = toBudgetItemRows(compareBudget.value.items);
  } catch {
    compareBudget.value = null;
    compareItems.value = [];
    message.error(`載入 ${compareYear.value} 年度歷史資料失敗`);
  } finally {
    loadingCompare.value = false;
  }
}

// ── 編輯操作 ─────────────────────────────────────

function addRow() {
  const seq =
    items.value.length > 0
      ? Math.max(...items.value.map((i) => i.sequence)) + 1
      : 1;
  items.value.push({
    _key: nextKey(),
    activityName: '',
    amount: 0,
    contentItem: '',
    note: undefined,
    sequence: seq,
  });
}

function removeRow(index: number) {
  items.value.splice(index, 1);
  items.value.forEach((item, i) => {
    item.sequence = i + 1;
  });
}

async function handleSave() {
  const invalid = items.value.some(
    (item) => !item.activityName.trim() || !item.contentItem.trim(),
  );
  if (invalid) {
    message.warning('請填寫所有項目的活動名稱與內容項目');
    return;
  }

  saving.value = true;
  try {
    const payload = items.value.map(
      ({ _key, ...rest }): PublicBudgetItemInput => rest,
    );
    const result = await savePublicDepartmentBudgetItemsApi(
      token.value,
      selectedYear.value,
      { items: payload },
    );
    items.value = toBudgetItemRows(result);
    message.success('預算項目已儲存');
  } catch {
    message.error('儲存失敗');
  } finally {
    saving.value = false;
  }
}

// ── Watchers ─────────────────────────────────────

watch(selectedYear, () => {
  fetchBudget();
  // 比對模式：重設左側年度
  if (compareYear.value === selectedYear.value) {
    compareYear.value = undefined;
    compareBudget.value = null;
    compareItems.value = [];
  }
});

watch(compareYear, () => {
  if (compareYear.value) {
    fetchCompareBudget();
  } else {
    compareBudget.value = null;
    compareItems.value = [];
  }
});

watch(viewMode, (mode) => {
  if (mode === 'compare' && !compareYear.value) {
    // 預設選擇前一年
    const prevYear = selectedYear.value - 1;
    const available = overview.value?.availableYears ?? [];
    const match = available.find((y: BudgetYearSummary) => y.year === prevYear);
    if (match) {
      compareYear.value = prevYear;
    } else if (available.length > 0) {
      const otherYears = available.filter(
        (y: BudgetYearSummary) => y.year !== selectedYear.value,
      );
      if (otherYears.length > 0) {
        compareYear.value = otherYears[0]!.year;
      }
    }
  }
});

onMounted(fetchOverview);
</script>

<template>
  <div class="mx-auto max-w-[1400px] px-4 py-8">
    <Spin :spinning="loadingOverview" tip="載入中...">
      <!-- 錯誤狀態 -->
      <div v-if="error" class="py-16 text-center">
        <Alert :message="error" show-icon type="error" />
      </div>

      <!-- 無年度資料 -->
      <div
        v-else-if="overview && overview.availableYears.length === 0"
        class="py-16 text-center"
      >
        <Alert
          :message="`${overview.departmentName} — 尚未有任何年度預算資料`"
          show-icon
          type="info"
        />
      </div>

      <!-- 正常內容 -->
      <template v-else-if="overview">
        <!-- 頁首 -->
        <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
          <h1 class="text-2xl font-bold">
            🏢 {{ overview.departmentName }} — 年度預算
          </h1>
          <div class="flex items-center gap-2">
            <Button
              :type="viewMode === 'edit' ? 'primary' : 'default'"
              @click="viewMode = 'edit'"
            >
              📝 編輯模式
            </Button>
            <Button
              :disabled="compareYearOptions.length === 0"
              :type="viewMode === 'compare' ? 'primary' : 'default'"
              @click="viewMode = 'compare'"
            >
              🔀 比對模式
            </Button>
          </div>
        </div>

        <!-- ═══ 編輯模式 ═══ -->
        <template v-if="viewMode === 'edit'">
          <Card>
            <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
              <div class="flex items-center gap-3">
                <span class="text-muted-foreground">年度：</span>
                <Select
                  v-model:value="selectedYear"
                  :options="yearOptions"
                  style="width: 120px"
                />
                <Tag
                  v-if="budget"
                  :color="
                    statusMap[budget.budgetStatus]?.color ?? 'default'
                  "
                >
                  {{
                    statusMap[budget.budgetStatus]?.label ??
                    budget.budgetStatus
                  }}
                </Tag>
              </div>
              <div v-if="isEditable" class="flex items-center gap-2">
                <Button @click="addRow">新增項目</Button>
                <Button
                  :loading="saving"
                  type="primary"
                  @click="handleSave"
                >
                  儲存
                </Button>
              </div>
            </div>

            <!-- 唯讀提示 -->
            <Alert
              v-if="budget && !isEditable"
              class="mb-4"
              message="目前為唯讀模式，無法編輯預算項目"
              show-icon
              type="info"
            />

            <BudgetItemsTable
              v-model:items="items"
              :editable="isEditable"
              :loading="loadingBudget"
              @add-row="addRow"
              @remove-row="removeRow"
            />
          </Card>
        </template>

        <!-- ═══ 比對模式 ═══ -->
        <template v-else>
          <Row :gutter="16">
            <!-- 左側：歷史年度（唯讀） -->
            <Col :lg="12" :xs="24">
              <Card>
                <div class="mb-4 flex items-center gap-3">
                  <span class="text-lg font-semibold">歷史參考</span>
                  <Select
                    v-model:value="compareYear"
                    :options="compareYearOptions"
                    placeholder="選擇年度"
                    style="width: 120px"
                  />
                  <Tag
                    v-if="compareBudget"
                    :color="
                      statusMap[compareBudget.budgetStatus]?.color ??
                      'default'
                    "
                  >
                    {{
                      statusMap[compareBudget.budgetStatus]?.label ??
                      compareBudget.budgetStatus
                    }}
                  </Tag>
                </div>

                <div v-if="!compareYear" class="py-12 text-center text-muted-foreground">
                  請選擇一個歷史年度以供參考
                </div>
                <BudgetItemsTable
                  v-else
                  :editable="false"
                  :items="compareItems"
                  :loading="loadingCompare"
                />
              </Card>
            </Col>

            <!-- 右側：當年度（可編輯） -->
            <Col :lg="12" :xs="24">
              <Card>
                <div
                  class="mb-4 flex flex-wrap items-center justify-between gap-3"
                >
                  <div class="flex items-center gap-3">
                    <span class="text-lg font-semibold">
                      ✏️ {{ selectedYear }} 年度
                    </span>
                    <Tag
                      v-if="budget"
                      :color="
                        statusMap[budget.budgetStatus]?.color ?? 'default'
                      "
                    >
                      {{
                        statusMap[budget.budgetStatus]?.label ??
                        budget.budgetStatus
                      }}
                    </Tag>
                  </div>
                  <div v-if="isEditable" class="flex items-center gap-2">
                    <Button size="small" @click="addRow">新增</Button>
                    <Button
                      :loading="saving"
                      size="small"
                      type="primary"
                      @click="handleSave"
                    >
                      儲存
                    </Button>
                  </div>
                </div>

                <Alert
                  v-if="budget && !isEditable"
                  class="mb-4"
                  message="唯讀模式"
                  show-icon
                  type="info"
                />

                <BudgetItemsTable
                  v-model:items="items"
                  :editable="isEditable"
                  :loading="loadingBudget"
                  @add-row="addRow"
                  @remove-row="removeRow"
                />
              </Card>
            </Col>
          </Row>
        </template>
      </template>
    </Spin>
  </div>
</template>
