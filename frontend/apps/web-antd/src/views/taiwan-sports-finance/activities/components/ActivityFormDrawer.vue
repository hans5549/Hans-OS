<script setup lang="ts">
import { computed, ref, watch } from 'vue';

import {
  Button,
  Card,
  Divider,
  Drawer,
  Input,
  InputNumber,
  message,
  Popconfirm,
  Select,
  SelectOption,
  Space,
  Switch,
  Table,
  Textarea,
} from 'ant-design-vue';

import type {
  ActivityDetailResponse,
  ActivityExpenseInput,
  ActivityGroupInput,
  BudgetItemResponse,
  CreateActivityRequest,
  UpdateActivityRequest,
} from '#/api';
import {
  createActivityApi,
  getDepartmentBudgetItemsApi,
  updateActivityApi,
} from '#/api';

interface DepartmentOption {
  id: string;
  name: string;
}

const props = defineProps<{
  open: boolean;
  year: number;
  month: number;
  departments: DepartmentOption[];
  editingActivity: ActivityDetailResponse | null;
}>();

const emit = defineEmits<{
  close: [];
  saved: [newMonth?: number];
}>();

const saving = ref(false);
const useGroups = ref(false);
const budgetItems = ref<BudgetItemResponse[]>([]);

// ── Form State ────────────────────────────────
const name = ref('');
const description = ref('');
const departmentId = ref('');
const selectedMonth = ref(0);
const groups = ref<GroupRow[]>([]);
const expenses = ref<ExpenseRow[]>([]);

interface ExpenseRow {
  _key: string;
  description: string;
  amount: number;
  note: string;
  budgetItemId: string | undefined;
  sequence: number;
}

interface GroupRow {
  _key: string;
  id?: string;
  name: string;
  sequence: number;
  expenses: ExpenseRow[];
}

const isEditing = computed(() => props.editingActivity !== null);

const title = computed(() =>
  isEditing.value ? `編輯活動 — ${props.year}年${props.month}月` : `新增活動 — ${props.year}年${props.month}月`,
);

// ── Watch open to reset form ──────────────────
watch(
  () => props.open,
  async (open) => {
    if (!open) return;

    if (props.editingActivity) {
      // 編輯模式：載入現有資料
      const a = props.editingActivity;
      name.value = a.name;
      description.value = a.description ?? '';
      departmentId.value = a.departmentId;
      selectedMonth.value = a.month;
      useGroups.value = a.groups.length > 0;

      groups.value = a.groups.map((g) => ({
        _key: crypto.randomUUID(),
        id: g.id,
        name: g.name,
        sequence: g.sequence,
        expenses: g.expenses.map((e) => ({
          _key: crypto.randomUUID(),
          description: e.description,
          amount: e.amount,
          note: e.note ?? '',
          budgetItemId: e.budgetItemId ?? undefined,
          sequence: e.sequence,
        })),
      }));

      expenses.value = a.ungroupedExpenses.map((e) => ({
        _key: crypto.randomUUID(),
        description: e.description,
        amount: e.amount,
        note: e.note ?? '',
        budgetItemId: e.budgetItemId ?? undefined,
        sequence: e.sequence,
      }));

      // 載入該部門的預算項目
      await fetchBudgetItems(a.departmentId);
    } else {
      // 新增模式：重置表單
      name.value = '';
      description.value = '';
      departmentId.value = '';
      selectedMonth.value = props.month;
      useGroups.value = false;
      groups.value = [];
      expenses.value = [createEmptyExpense(1)];
      budgetItems.value = [];
    }
  },
);

// ── Budget Items Fetch ────────────────────────
watch(departmentId, async (deptId) => {
  if (deptId) {
    await fetchBudgetItems(deptId);
  } else {
    budgetItems.value = [];
  }
});

async function fetchBudgetItems(deptId: string) {
  try {
    budgetItems.value = await getDepartmentBudgetItemsApi(props.year, deptId);
  } catch {
    budgetItems.value = [];
  }
}

// ── Expense Helpers ───────────────────────────
function createEmptyExpense(sequence: number): ExpenseRow {
  return {
    _key: crypto.randomUUID(),
    description: '',
    amount: 0,
    note: '',
    budgetItemId: undefined,
    sequence,
  };
}

function addExpense() {
  const nextSeq = expenses.value.length + 1;
  expenses.value.push(createEmptyExpense(nextSeq));
}

function removeExpense(index: number) {
  expenses.value.splice(index, 1);
  renumberExpenses(expenses.value);
}

function renumberExpenses(list: ExpenseRow[]) {
  list.forEach((e, i) => {
    e.sequence = i + 1;
  });
}

// ── Group Helpers ─────────────────────────────
function addGroup() {
  groups.value.push({
    _key: crypto.randomUUID(),
    name: '',
    sequence: groups.value.length + 1,
    expenses: [createEmptyExpense(1)],
  });
}

function removeGroup(index: number) {
  groups.value.splice(index, 1);
  groups.value.forEach((g, i) => {
    g.sequence = i + 1;
  });
}

function addGroupExpense(groupIndex: number) {
  const group = groups.value[groupIndex];
  if (group) {
    const nextSeq = group.expenses.length + 1;
    group.expenses.push(createEmptyExpense(nextSeq));
  }
}

function removeGroupExpense(groupIndex: number, expenseIndex: number) {
  const group = groups.value[groupIndex];
  if (group) {
    group.expenses.splice(expenseIndex, 1);
    renumberExpenses(group.expenses);
  }
}

// ── Toggle Groups Mode ────────────────────────
function handleToggleGroups(checked: boolean | string | number) {
  if (checked && groups.value.length === 0) {
    addGroup();
  }
}

// ── Submit ────────────────────────────────────
async function handleSave() {
  if (!name.value.trim()) {
    message.warning('請輸入活動名稱');
    return;
  }
  if (!departmentId.value) {
    message.warning('請選擇部門');
    return;
  }

  const groupsPayload: ActivityGroupInput[] | undefined = useGroups.value
    ? groups.value.map((g, gi) => ({
        id: g.id,
        name: g.name,
        sequence: gi + 1,
        expenses: g.expenses.map((e, ei) => mapExpenseInput(e, ei)),
      }))
    : undefined;

  const expensesPayload: ActivityExpenseInput[] | undefined = !useGroups.value
    ? expenses.value.map((e, ei) => mapExpenseInput(e, ei))
    : undefined;

  // 驗證
  if (useGroups.value) {
    if (groups.value.some((g) => !g.name.trim())) {
      message.warning('請填寫所有分組名稱');
      return;
    }
    const allGroupExpenses = groups.value.flatMap((g) => g.expenses);
    if (allGroupExpenses.some((e) => !e.description.trim())) {
      message.warning('請填寫所有開銷說明');
      return;
    }
  } else {
    if (expenses.value.some((e) => !e.description.trim())) {
      message.warning('請填寫所有開銷說明');
      return;
    }
  }

  saving.value = true;
  try {
    if (isEditing.value && props.editingActivity) {
      const monthChanged = selectedMonth.value !== props.editingActivity.month;
      const payload: UpdateActivityRequest = {
        name: name.value.trim(),
        description: description.value.trim() || undefined,
        month: monthChanged ? selectedMonth.value : undefined,
        groups: groupsPayload,
        expenses: expensesPayload,
      };
      await updateActivityApi(props.editingActivity.id, payload);
      message.success('活動已更新');
      emit('saved', monthChanged ? selectedMonth.value : undefined);
    } else {
      const payload: CreateActivityRequest = {
        departmentId: departmentId.value,
        year: props.year,
        month: props.month,
        name: name.value.trim(),
        description: description.value.trim() || undefined,
        groups: groupsPayload,
        expenses: expensesPayload,
      };
      await createActivityApi(payload);
      message.success('活動已新增');
      emit('saved');
    }
    emit('close');
  } finally {
    saving.value = false;
  }
}

function mapExpenseInput(e: ExpenseRow, index: number): ActivityExpenseInput {
  return {
    description: e.description,
    amount: e.amount,
    note: e.note || undefined,
    sequence: index + 1,
    budgetItemId: e.budgetItemId || undefined,
  };
}

function formatCurrency(val: number | string): string {
  return `$ ${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
}

function parseCurrency(val: string | undefined): number {
  return Number(String(val ?? '').replace(/\$\s?|(,*)/g, '')) || 0;
}

const expenseColumns = [
  { dataIndex: 'description', title: '開銷說明', width: 200 },
  { dataIndex: 'amount', title: '金額', width: 140 },
  { dataIndex: 'budgetItemId', title: '連結預算', width: 180 },
  { dataIndex: 'note', title: '備註', ellipsis: true },
  { key: 'action', title: '', width: 60, fixed: 'right' as const },
];
</script>

<template>
  <Drawer
    :open="props.open"
    :title="title"
    placement="right"
    width="900"
    :destroy-on-close="true"
    @close="emit('close')"
  >
    <template #extra>
      <Space>
        <Button @click="emit('close')">取消</Button>
        <Button :loading="saving" type="primary" @click="handleSave">
          儲存
        </Button>
      </Space>
    </template>

    <!-- 基本資訊 -->
    <div class="mb-4 grid grid-cols-2 gap-4">
      <div>
        <label class="mb-1 block text-sm font-medium">活動名稱 *</label>
        <Input
          v-model:value="name"
          :maxlength="200"
          placeholder="例如：暑期籃球營"
        />
      </div>
      <div>
        <label class="mb-1 block text-sm font-medium">所屬部門 *</label>
        <Select
          v-model:value="departmentId"
          placeholder="請選擇部門"
          style="width: 100%"
          :disabled="isEditing"
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
    </div>

    <div v-if="isEditing" class="mb-4 grid grid-cols-2 gap-4">
      <div>
        <label class="mb-1 block text-sm font-medium">所屬月份</label>
        <Select
          v-model:value="selectedMonth"
          style="width: 100%"
        >
          <SelectOption v-for="m in 12" :key="m" :value="m">
            {{ m }} 月
          </SelectOption>
        </Select>
      </div>
    </div>

    <div class="mb-4">
      <label class="mb-1 block text-sm font-medium">說明</label>
      <Textarea
        v-model:value="description"
        :maxlength="1000"
        :rows="2"
        placeholder="活動說明（選填）"
      />
    </div>

    <Divider />

    <!-- 模式切換 -->
    <div class="mb-4 flex items-center gap-3">
      <span class="text-sm font-medium">分組模式</span>
      <Switch
        v-model:checked="useGroups"
        @change="handleToggleGroups"
      />
      <span class="text-xs text-gray-400">
        {{ useGroups ? '每個分組可獨立管理開銷項目' : '直接列出所有開銷項目' }}
      </span>
    </div>

    <!-- 分組模式 -->
    <template v-if="useGroups">
      <Card
        v-for="(group, gi) in groups"
        :key="group._key"
        size="small"
        class="mb-3"
      >
        <template #title>
          <div class="flex items-center gap-2">
            <Input
              v-model:value="group.name"
              :maxlength="200"
              placeholder="分組名稱"
              size="small"
              style="width: 200px"
            />
            <Popconfirm
              title="確定要刪除此分組？"
              ok-text="確定"
              cancel-text="取消"
              @confirm="removeGroup(gi)"
            >
              <Button danger size="small" type="link">刪除分組</Button>
            </Popconfirm>
          </div>
        </template>
        <template #extra>
          <Button size="small" @click="addGroupExpense(gi)">
            新增開銷
          </Button>
        </template>

        <Table
          :columns="expenseColumns"
          :data-source="group.expenses"
          :pagination="false"
          row-key="_key"
          size="small"
        >
          <template #bodyCell="{ column, index }">
            <template v-if="column.dataIndex === 'description'">
              <Input
                v-model:value="group.expenses[index]!.description"
                :maxlength="200"
                placeholder="開銷說明"
                size="small"
              />
            </template>
            <template v-else-if="column.dataIndex === 'amount'">
              <InputNumber
                v-model:value="group.expenses[index]!.amount"
                :formatter="(v: number | string) => formatCurrency(v)"
                :min="0"
                :parser="(v: string | undefined) => parseCurrency(v)"
                size="small"
                style="width: 100%"
              />
            </template>
            <template v-else-if="column.dataIndex === 'budgetItemId'">
              <Select
                v-model:value="group.expenses[index]!.budgetItemId"
                :allow-clear="true"
                placeholder="選擇預算項目"
                size="small"
                style="width: 100%"
              >
                <SelectOption
                  v-for="bi in budgetItems"
                  :key="bi.id"
                  :value="bi.id"
                >
                  {{ bi.activityName }} - {{ bi.contentItem }}
                </SelectOption>
              </Select>
            </template>
            <template v-else-if="column.dataIndex === 'note'">
              <Input
                v-model:value="group.expenses[index]!.note"
                :maxlength="1000"
                placeholder="備註"
                size="small"
              />
            </template>
            <template v-else-if="column.key === 'action'">
              <Popconfirm
                title="刪除？"
                ok-text="確定"
                cancel-text="取消"
                @confirm="removeGroupExpense(gi, index)"
              >
                <Button danger size="small" type="link">刪除</Button>
              </Popconfirm>
            </template>
          </template>
        </Table>
      </Card>

      <Button block type="dashed" @click="addGroup">
        + 新增分組
      </Button>
    </template>

    <!-- 簡單模式（無分組） -->
    <template v-else>
      <Table
        :columns="expenseColumns"
        :data-source="expenses"
        :pagination="false"
        row-key="_key"
        size="small"
      >
        <template #bodyCell="{ column, index }">
          <template v-if="column.dataIndex === 'description'">
            <Input
              v-model:value="expenses[index]!.description"
              :maxlength="200"
              placeholder="開銷說明"
              size="small"
            />
          </template>
          <template v-else-if="column.dataIndex === 'amount'">
            <InputNumber
              v-model:value="expenses[index]!.amount"
              :formatter="(v: number | string) => formatCurrency(v)"
              :min="0"
              :parser="(v: string | undefined) => parseCurrency(v)"
              size="small"
              style="width: 100%"
            />
          </template>
          <template v-else-if="column.dataIndex === 'budgetItemId'">
            <Select
              v-model:value="expenses[index]!.budgetItemId"
              :allow-clear="true"
              placeholder="選擇預算項目"
              size="small"
              style="width: 100%"
            >
              <SelectOption
                v-for="bi in budgetItems"
                :key="bi.id"
                :value="bi.id"
              >
                {{ bi.activityName }} - {{ bi.contentItem }}
              </SelectOption>
            </Select>
          </template>
          <template v-else-if="column.dataIndex === 'note'">
            <Input
              v-model:value="expenses[index]!.note"
              :maxlength="1000"
              placeholder="備註"
              size="small"
            />
          </template>
          <template v-else-if="column.key === 'action'">
            <Popconfirm
              v-if="expenses.length > 1"
              title="刪除？"
              ok-text="確定"
              cancel-text="取消"
              @confirm="removeExpense(index)"
            >
              <Button danger size="small" type="link">刪除</Button>
            </Popconfirm>
          </template>
        </template>
      </Table>

      <Button block class="mt-2" type="dashed" @click="addExpense">
        + 新增開銷
      </Button>
    </template>
  </Drawer>
</template>
