<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';

import { Page } from '@vben/common-ui';

import {
  Button,
  Card,
  Col,
  Empty,
  InputNumber,
  Row,
  Select,
  SelectOption,
  Spin,
  Statistic,
  message,
} from 'ant-design-vue';

import type {
  ActivityDetailResponse,
  DepartmentResponse,
  MonthSummaryResponse,
} from '#/api';
import {
  deleteActivityApi,
  getActivitiesApi,
  getActivityDetailApi,
  getActivityMonthSummariesApi,
  getDepartmentsApi,
} from '#/api';

import ActivityCard from './components/ActivityCard.vue';
import ActivityFormDrawer from './components/ActivityFormDrawer.vue';
import MonthSidebar from './components/MonthSidebar.vue';

defineOptions({ name: 'TsfActivitiesPage' });

// ── State ─────────────────────────────────────
const currentYear = new Date().getFullYear();
const selectedYear = ref(currentYear);
const selectedMonth = ref(new Date().getMonth() + 1);
const selectedDepartmentId = ref<string | undefined>(undefined);

const departments = ref<DepartmentResponse[]>([]);
const monthSummaries = ref<MonthSummaryResponse[]>([]);
const activities = ref<ActivityDetailResponse[]>([]);
const loading = ref(false);
const loadingActivities = ref(false);

// ── Drawer ────────────────────────────────────
const drawerOpen = ref(false);
const editingActivity = ref<ActivityDetailResponse | null>(null);

// ── Computed ──────────────────────────────────
const monthTotal = computed(() =>
  activities.value.reduce((sum, a) => sum + a.totalAmount, 0),
);

const yearTotal = computed(() =>
  monthSummaries.value.reduce((sum, s) => sum + s.totalAmount, 0),
);

const departmentOptions = computed(() =>
  departments.value.map((d) => ({ id: d.id, name: d.name })),
);

// ── Data Fetching ─────────────────────────────
async function fetchDepartments() {
  try {
    departments.value = await getDepartmentsApi();
  } catch {
    message.error('載入部門列表失敗');
  }
}

async function fetchMonthSummaries() {
  try {
    monthSummaries.value = await getActivityMonthSummariesApi(
      selectedYear.value,
      selectedDepartmentId.value,
    );
  } catch {
    monthSummaries.value = [];
  }
}

async function fetchActivities() {
  loadingActivities.value = true;
  try {
    const summaries = await getActivitiesApi(
      selectedYear.value,
      selectedMonth.value,
      selectedDepartmentId.value,
    );

    // 載入每個活動的完整明細
    const details = await Promise.all(
      summaries.map((s) => getActivityDetailApi(s.id)),
    );
    activities.value = details;
  } catch {
    activities.value = [];
    message.error('載入活動列表失敗');
  } finally {
    loadingActivities.value = false;
  }
}

async function refreshAll() {
  loading.value = true;
  try {
    await Promise.all([fetchMonthSummaries(), fetchActivities()]);
  } finally {
    loading.value = false;
  }
}

// ── Actions ───────────────────────────────────
function handleCreate() {
  editingActivity.value = null;
  drawerOpen.value = true;
}

function handleEdit(activity: ActivityDetailResponse) {
  editingActivity.value = activity;
  drawerOpen.value = true;
}

async function handleDelete(id: string) {
  try {
    await deleteActivityApi(id);
    message.success('活動已刪除');
    await refreshAll();
  } catch {
    message.error('刪除失敗');
  }
}

async function handleSaved(newMonth?: number) {
  if (newMonth !== undefined) {
    selectedMonth.value = newMonth;
  }
  await refreshAll();
}

// ── Watchers ──────────────────────────────────
watch([selectedYear, selectedDepartmentId], () => refreshAll());
watch(selectedMonth, () => fetchActivities());

// ── Init ──────────────────────────────────────
onMounted(async () => {
  await fetchDepartments();
  await refreshAll();
});
</script>

<template>
  <Page auto-content-height>
    <!-- 頂部篩選列 -->
    <Card class="mb-4">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-4">
          <h3 class="m-0 text-lg font-medium">部門活動記錄</h3>
          <InputNumber
            v-model:value="selectedYear"
            :max="currentYear + 5"
            :min="2020"
            style="width: 100px"
          />
          <Select
            v-model:value="selectedDepartmentId"
            :allow-clear="true"
            placeholder="全部部門"
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
        </div>
        <Button type="primary" @click="handleCreate">
          新增活動
        </Button>
      </div>
    </Card>

    <!-- 統計摘要 -->
    <Row :gutter="16" class="mb-4">
      <Col :span="8">
        <Card>
          <Statistic
            :value="yearTotal"
            :value-style="{ color: 'var(--ant-color-primary)' }"
            title="年度合計"
          >
            <template #prefix>$</template>
          </Statistic>
        </Card>
      </Col>
      <Col :span="8">
        <Card>
          <Statistic
            :value="monthTotal"
            title="本月合計"
          >
            <template #prefix>$</template>
          </Statistic>
        </Card>
      </Col>
      <Col :span="8">
        <Card>
          <Statistic
            :value="activities.length"
            title="本月活動數"
            suffix="項"
          />
        </Card>
      </Col>
    </Row>

    <!-- 主體：左側月份 + 右側活動列表 -->
    <Row :gutter="16">
      <Col :span="4">
        <Card :body-style="{ padding: '8px 0' }">
          <MonthSidebar
            v-model:selected-month="selectedMonth"
            :month-summaries="monthSummaries"
          />
        </Card>
      </Col>

      <Col :span="20">
        <Spin :spinning="loadingActivities">
          <template v-if="activities.length > 0">
            <ActivityCard
              v-for="activity in activities"
              :key="activity.id"
              :activity="activity"
              @delete="handleDelete"
              @edit="handleEdit"
            />
          </template>
          <Card v-else>
            <Empty description="本月尚無活動記錄">
              <Button type="primary" @click="handleCreate">
                新增活動
              </Button>
            </Empty>
          </Card>
        </Spin>
      </Col>
    </Row>

    <!-- 新增/編輯 Drawer -->
    <ActivityFormDrawer
      :departments="departmentOptions"
      :editing-activity="editingActivity"
      :month="selectedMonth"
      :open="drawerOpen"
      :year="selectedYear"
      @close="drawerOpen = false"
      @saved="handleSaved"
    />
  </Page>
</template>
