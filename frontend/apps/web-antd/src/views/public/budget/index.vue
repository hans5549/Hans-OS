<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';

import { useRoute } from 'vue-router';

import {
  Alert,
  Button,
  Card,
  Input,
  InputNumber,
  message,
  Popconfirm,
  Spin,
  Table,
  Tag,
} from 'ant-design-vue';

import type {
  PublicBudgetItemInput,
  PublicBudgetResponse,
} from '#/api';

import { getPublicBudgetApi, savePublicBudgetItemsApi } from '#/api';

defineOptions({ name: 'PublicBudgetPage' });

const route = useRoute();
const token = computed(() => route.params.token as string);

const loading = ref(false);
const saving = ref(false);
const budget = ref<PublicBudgetResponse | null>(null);
const error = ref<string | null>(null);

interface BudgetItemRow extends PublicBudgetItemInput {
  _key: string;
}

const items = ref<BudgetItemRow[]>([]);
let keyCounter = 0;
const nextKey = () => `row-${++keyCounter}`;

const isEditable = computed(
  () => budget.value?.effectivePermission === 'Editable',
);

const statusMap: Record<string, { color: string; label: string }> = {
  Draft: { color: 'default', label: '草稿' },
  Submitted: { color: 'blue', label: '已提交' },
  Approved: { color: 'green', label: '已核准' },
  Settled: { color: 'purple', label: '已結算' },
};

const formatNumericInput = (value?: number | string) =>
  `${value ?? ''}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',');

const editableColumns = [
  {
    align: 'center' as const,
    dataIndex: 'sequence',
    title: '項次',
    width: 70,
  },
  { dataIndex: 'activityName', title: '活動名稱', width: 180 },
  { dataIndex: 'contentItem', title: '內容項目', width: 160 },
  {
    align: 'right' as const,
    dataIndex: 'amount',
    title: '預算金額',
    width: 130,
  },
  { dataIndex: 'note', title: '備註說明', ellipsis: true },
  {
    align: 'center' as const,
    fixed: 'right' as const,
    key: 'action',
    title: '操作',
    width: 70,
  },
];

const readonlyColumns = editableColumns.filter((c) => c.key !== 'action');

const columns = computed(() =>
  isEditable.value ? editableColumns : readonlyColumns,
);

const budgetTotal = () =>
  items.value.reduce((sum, i) => sum + (i.amount || 0), 0);

async function fetchBudget() {
  loading.value = true;
  error.value = null;
  try {
    budget.value = await getPublicBudgetApi(token.value);
    items.value = budget.value.items.map((item) => ({
      ...item,
      _key: nextKey(),
      note: item.note ?? undefined,
    }));
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
    loading.value = false;
  }
}

function addRow() {
  const seq =
    items.value.length > 0
      ? Math.max(...items.value.map((i) => i.sequence)) + 1
      : 1;
  items.value.push({
    _key: nextKey(),
    sequence: seq,
    activityName: '',
    contentItem: '',
    amount: 0,
    note: undefined,
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
    const result = await savePublicBudgetItemsApi(token.value, {
      items: payload,
    });
    items.value = result.map((item) => ({
      ...item,
      _key: nextKey(),
      note: item.note ?? undefined,
    }));
    message.success('預算項目已儲存');
  } catch {
    message.error('儲存失敗');
  } finally {
    saving.value = false;
  }
}

const formatCurrency = (value: number) => value.toLocaleString('zh-TW');

onMounted(fetchBudget);
</script>

<template>
  <div class="mx-auto max-w-[1100px] px-4 py-8">
    <Spin :spinning="loading" tip="載入中...">
      <!-- 錯誤狀態 -->
      <div v-if="error" class="py-16 text-center">
        <Alert :message="error" show-icon type="error" />
      </div>

      <!-- 正常內容 -->
      <template v-else-if="budget">
        <Card>
          <!-- 標題 -->
          <div class="mb-4 flex items-center justify-between">
            <div class="flex items-center gap-3">
              <h2 class="text-xl font-semibold">
                {{ budget.departmentName }} — {{ budget.year }} 年度預算
              </h2>
              <Tag
                :color="statusMap[budget.budgetStatus]?.color ?? 'default'"
              >
                {{ statusMap[budget.budgetStatus]?.label ?? budget.budgetStatus }}
              </Tag>
            </div>
            <div v-if="isEditable" class="flex items-center gap-2">
              <Button @click="addRow">新增項目</Button>
              <Button :loading="saving" type="primary" @click="handleSave">
                儲存
              </Button>
            </div>
          </div>

          <!-- 唯讀提示 -->
          <Alert
            v-if="!isEditable"
            class="mb-4"
            message="目前為唯讀模式，無法編輯預算項目"
            show-icon
            type="info"
          />

          <!-- 預算表格 -->
          <Table
            :columns="columns"
            :data-source="items"
            :pagination="false"
            row-key="_key"
            size="small"
            :scroll="{ x: 750 }"
          >
            <template #bodyCell="{ column, index }">
              <!-- 項次 -->
              <template v-if="column.dataIndex === 'sequence'">
                {{ index + 1 }}
              </template>

              <!-- 活動名稱 -->
              <template v-else-if="column.dataIndex === 'activityName'">
                <Input
                  v-if="isEditable"
                  v-model:value="items[index]!.activityName"
                  :maxlength="200"
                  placeholder="活動名稱"
                  size="small"
                />
                <span v-else>{{ items[index]!.activityName }}</span>
              </template>

              <!-- 內容項目 -->
              <template v-else-if="column.dataIndex === 'contentItem'">
                <Input
                  v-if="isEditable"
                  v-model:value="items[index]!.contentItem"
                  :maxlength="200"
                  placeholder="內容項目"
                  size="small"
                />
                <span v-else>{{ items[index]!.contentItem }}</span>
              </template>

              <!-- 預算金額 -->
              <template v-else-if="column.dataIndex === 'amount'">
                <InputNumber
                  v-if="isEditable"
                  v-model:value="items[index]!.amount"
                  :formatter="formatNumericInput"
                  :min="0"
                  :parser="(v: string) => v.replace(/,/g, '')"
                  size="small"
                  style="width: 100%"
                />
                <span v-else>{{
                  formatCurrency(items[index]!.amount)
                }}</span>
              </template>

              <!-- 備註 -->
              <template v-else-if="column.dataIndex === 'note'">
                <Input
                  v-if="isEditable"
                  v-model:value="items[index]!.note"
                  :maxlength="1000"
                  placeholder="備註"
                  size="small"
                />
                <span v-else>{{ items[index]!.note ?? '' }}</span>
              </template>

              <!-- 操作 -->
              <template v-else-if="column.key === 'action'">
                <Popconfirm
                  cancel-text="取消"
                  ok-text="刪除"
                  ok-type="danger"
                  title="確定要刪除此項目？"
                  @confirm="removeRow(index)"
                >
                  <Button danger size="small" type="link">刪除</Button>
                </Popconfirm>
              </template>
            </template>

            <!-- 合計列 -->
            <template #summary>
              <tr class="font-semibold">
                <td />
                <td colspan="2" class="text-right">合計</td>
                <td class="text-right">{{ formatCurrency(budgetTotal()) }}</td>
                <td />
                <td v-if="isEditable" />
              </tr>
            </template>

            <template #emptyText>
              <div class="py-8 text-center text-gray-400">
                <template v-if="isEditable">
                  尚無預算項目，請點擊「新增項目」開始編列
                </template>
                <template v-else> 尚無預算項目 </template>
              </div>
            </template>
          </Table>
        </Card>
      </template>
    </Spin>
  </div>
</template>
