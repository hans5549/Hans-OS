<script setup lang="ts">
import { ref, watch } from 'vue';

import {
  Button,
  Drawer,
  Input,
  InputNumber,
  message,
  Popconfirm,
  Table,
} from 'ant-design-vue';

import type { BudgetItemInput, BudgetItemResponse } from '#/api';

import {
  getDepartmentBudgetItemsApi,
  saveDepartmentBudgetItemsApi,
} from '#/api';

interface BudgetItemRow extends BudgetItemInput {
  _key: string;
}

const props = defineProps<{
  departmentId: string;
  departmentName: string;
  open: boolean;
  year: number;
}>();

const emit = defineEmits<{
  close: [];
  saved: [];
}>();

const loading = ref(false);
const saving = ref(false);
const items = ref<BudgetItemRow[]>([]);

let keyCounter = 0;
const nextKey = () => `row-${++keyCounter}`;
const formatNumericInput = (value?: string | number) =>
  `${value ?? ''}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',');

const columns = [
  { dataIndex: 'sequence', title: '項次', width: 70, align: 'center' as const },
  { dataIndex: 'activityName', title: '活動名稱', width: 180 },
  { dataIndex: 'contentItem', title: '內容項目', width: 160 },
  { dataIndex: 'amount', title: '預算金額', width: 130, align: 'right' as const },
  { dataIndex: 'note', title: '備註說明', ellipsis: true },
  {
    dataIndex: 'actualAmount',
    title: '實際核銷',
    width: 130,
    align: 'right' as const,
  },
  { key: 'action', title: '操作', width: 70, fixed: 'right' as const },
];

watch(
  () => props.open,
  async (open) => {
    if (!open || !props.departmentId) return;
    await fetchItems();
  },
);

async function fetchItems() {
  loading.value = true;
  try {
    const data = await getDepartmentBudgetItemsApi(
      props.year,
      props.departmentId,
    );
    items.value = data.map(toInput);
  } finally {
    loading.value = false;
  }
}

function toInput(item: BudgetItemResponse): BudgetItemRow {
  return {
    _key: nextKey(),
    id: item.id,
    sequence: item.sequence,
    activityName: item.activityName,
    contentItem: item.contentItem,
    amount: item.amount,
    note: item.note ?? undefined,
    actualAmount: item.actualAmount ?? undefined,
    actualNote: item.actualNote ?? undefined,
  };
}

function addRow() {
  items.value.push(createEmptyRow(getNextSequence()));
}

function removeRow(index: number) {
  items.value.splice(index, 1);
  renumberItems();
}

const budgetTotal = () => items.value.reduce((sum, i) => sum + (i.amount || 0), 0);

const actualTotal = () => items.value.reduce((sum, i) => sum + (i.actualAmount || 0), 0);

async function handleSave() {
  if (hasInvalidItems()) {
    message.warning('請填寫所有項目的活動名稱與內容項目');
    return;
  }

  saving.value = true;
  try {
    items.value = (await saveItems()).map(toInput);
    message.success('預算項目已儲存');
    emit('saved');
  } finally {
    saving.value = false;
  }
}

const handleClose = () => emit('close');

const formatCurrency = (value: number) => value.toLocaleString('zh-TW');

function getNextSequence() {
  return items.value.length > 0
    ? Math.max(...items.value.map((item) => item.sequence)) + 1
    : 1;
}

function createEmptyRow(sequence: number): BudgetItemRow {
  return {
    _key: nextKey(),
    sequence,
    activityName: '',
    contentItem: '',
    amount: 0,
    note: undefined,
    actualAmount: undefined,
    actualNote: undefined,
  };
}

function renumberItems() {
  items.value.forEach((item, index) => {
    item.sequence = index + 1;
  });
}

const hasInvalidItems = () =>
  items.value.some((item) => !item.activityName.trim() || !item.contentItem.trim());

const saveItems = () => {
  const payload = items.value.map(({ _key, ...rest }) => rest);
  return saveDepartmentBudgetItemsApi(props.year, props.departmentId, { items: payload });
};
</script>

<template>
  <Drawer
    :destroy-on-close="false"
    placement="right"
    :open="props.open"
    :title="`${props.departmentName} — ${props.year} 年度預算`"
    :width="960"
    @close="handleClose"
  >
    <template #extra>
      <div class="flex items-center gap-2">
        <Button @click="addRow">
          <template #icon>
            <span class="i-lucide-plus" />
          </template>
          新增項目
        </Button>
        <Button :loading="saving" type="primary" @click="handleSave">
          儲存
        </Button>
      </div>
    </template>

    <Table
      :columns="columns"
      :data-source="items"
      :loading="loading"
      :pagination="false"
      row-key="_key"
      size="small"
      :scroll="{ x: 900 }"
    >
      <template #bodyCell="{ column, index }">
        <template v-if="column.dataIndex === 'sequence'">
          {{ index + 1 }}
        </template>

        <template v-else-if="column.dataIndex === 'activityName'">
          <Input
            v-model:value="items[index]!.activityName"
            :maxlength="200"
            placeholder="活動名稱"
            size="small"
          />
        </template>

        <template v-else-if="column.dataIndex === 'contentItem'">
          <Input
            v-model:value="items[index]!.contentItem"
            :maxlength="200"
            placeholder="內容項目"
            size="small"
          />
        </template>

        <template v-else-if="column.dataIndex === 'amount'">
          <InputNumber
            v-model:value="items[index]!.amount"
            :formatter="formatNumericInput"
            :min="0"
            :parser="(v: string) => v.replace(/,/g, '')"
            size="small"
            style="width: 100%"
          />
        </template>

        <template v-else-if="column.dataIndex === 'note'">
          <Input
            v-model:value="items[index]!.note"
            :maxlength="1000"
            placeholder="備註"
            size="small"
          />
        </template>

        <template v-else-if="column.dataIndex === 'actualAmount'">
          <InputNumber
            v-model:value="items[index]!.actualAmount"
            :formatter="formatNumericInput"
            :min="0"
            :parser="(v: string) => v.replace(/,/g, '')"
            placeholder="—"
            size="small"
            style="width: 100%"
          />
        </template>

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

      <template #summary>
        <tr class="font-semibold">
          <td />
          <td colspan="2" class="text-right">合計</td>
          <td class="text-right">{{ formatCurrency(budgetTotal()) }}</td>
          <td />
          <td class="text-right">{{ formatCurrency(actualTotal()) }}</td>
          <td />
        </tr>
      </template>

      <template #emptyText>
        <div class="py-8 text-center text-gray-400">
          尚無預算項目，請點擊「新增項目」開始編列
        </div>
      </template>
    </Table>
  </Drawer>
</template>
