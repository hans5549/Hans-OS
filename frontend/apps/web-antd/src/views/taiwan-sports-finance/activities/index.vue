<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';

import {
  Button,
  Empty,
  InputNumber,
  Select,
  SelectOption,
  Spin,
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

import TsfGlassPage from '../_shared/TsfGlassPage.vue';
import TsfMetricCard from '../_shared/TsfMetricCard.vue';

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
  <TsfGlassPage
    icon="i-lucide-trophy"
    subtitle="依月份與部門檢視活動費，並從活動明細建立待匯款。"
    title="部門活動記錄"
  >
    <template #actions>
      <Button type="primary" @click="handleCreate">
        <template #icon><span class="i-lucide-plus" /></template>
        新增活動
      </Button>
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
      <div class="tsf-filter-group">
        <span class="tsf-filter-label">部門</span>
        <Select
          v-model:value="selectedDepartmentId"
          placeholder="全部部門"
          style="width: 168px"
        >
          <SelectOption :value="undefined">全部部門</SelectOption>
          <SelectOption
            v-for="dept in departments"
            :key="dept.id"
            :value="dept.id"
          >
            {{ dept.name }}
          </SelectOption>
        </Select>
      </div>
    </template>

    <div class="tsf-kpi-grid tsf-kpi-grid--three">
      <TsfMetricCard
        icon="i-lucide-calendar-range"
        label="年度合計"
        prefix="$"
        tone="info"
        :value="yearTotal.toLocaleString('zh-TW')"
      />
      <TsfMetricCard
        icon="i-lucide-calendar-days"
        label="本月合計"
        prefix="$"
        tone="success"
        :value="monthTotal.toLocaleString('zh-TW')"
      />
      <TsfMetricCard
        icon="i-lucide-clipboard-list"
        label="本月活動數"
        suffix="項"
        tone="neutral"
        :value="activities.length"
      />
    </div>

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-[220px_minmax(0,1fr)]">
      <aside class="tsf-month-rail">
          <MonthSidebar
            v-model:selected-month="selectedMonth"
            :month-summaries="monthSummaries"
          />
      </aside>

      <section>
        <Spin :spinning="loadingActivities">
          <template v-if="activities.length > 0">
            <ActivityCard
              v-for="activity in activities"
              :key="activity.id"
              :activity="activity"
              @delete="handleDelete"
              @edit="handleEdit"
              @remittanceCreated="fetchActivities"
            />
          </template>
          <div v-else class="tsf-empty-panel">
            <Empty description="本月尚無活動記錄">
              <Button type="primary" @click="handleCreate">
                新增活動
              </Button>
            </Empty>
          </div>
        </Spin>
      </section>
    </div>

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
  </TsfGlassPage>
</template>
