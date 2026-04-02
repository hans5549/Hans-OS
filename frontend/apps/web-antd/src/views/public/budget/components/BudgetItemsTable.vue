<script setup lang="ts">
import { computed } from 'vue';

import {
  Button,
  Input,
  InputNumber,
  Popconfirm,
  Table,
} from 'ant-design-vue';

import type { PublicBudgetItemInput } from '#/api';

export interface BudgetItemRow extends PublicBudgetItemInput {
  _key: string;
}

const props = defineProps<{
  editable?: boolean;
  items: BudgetItemRow[];
  loading?: boolean;
}>();

const emit = defineEmits<{
  'add-row': [];
  'remove-row': [index: number];
  'update:items': [items: BudgetItemRow[]];
}>();

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
  props.editable ? editableColumns : readonlyColumns,
);

const total = computed(() =>
  props.items.reduce((sum, i) => sum + (i.amount || 0), 0),
);

const formatCurrency = (value: number) => value.toLocaleString('zh-TW');

function updateField(index: number, field: string, value: unknown) {
  const updated = [...props.items];
  (updated[index] as Record<string, unknown>)[field] = value;
  emit('update:items', updated);
}
</script>

<template>
  <Table
    :columns="columns"
    :data-source="items"
    :loading="loading"
    :pagination="false"
    :scroll="{ x: 750 }"
    row-key="_key"
    size="small"
  >
    <template #bodyCell="{ column, index }">
      <!-- 項次 -->
      <template v-if="column.dataIndex === 'sequence'">
        {{ index + 1 }}
      </template>

      <!-- 活動名稱 -->
      <template v-else-if="column.dataIndex === 'activityName'">
        <Input
          v-if="editable"
          :maxlength="200"
          :value="items[index]!.activityName"
          placeholder="活動名稱"
          size="small"
          @update:value="updateField(index, 'activityName', $event)"
        />
        <span v-else>{{ items[index]!.activityName }}</span>
      </template>

      <!-- 內容項目 -->
      <template v-else-if="column.dataIndex === 'contentItem'">
        <Input
          v-if="editable"
          :maxlength="200"
          :value="items[index]!.contentItem"
          placeholder="內容項目"
          size="small"
          @update:value="updateField(index, 'contentItem', $event)"
        />
        <span v-else>{{ items[index]!.contentItem }}</span>
      </template>

      <!-- 預算金額 -->
      <template v-else-if="column.dataIndex === 'amount'">
        <InputNumber
          v-if="editable"
          :formatter="formatNumericInput"
          :min="0"
          :parser="(v: string) => v.replace(/,/g, '')"
          :value="items[index]!.amount"
          size="small"
          style="width: 100%"
          @update:value="updateField(index, 'amount', $event)"
        />
        <span v-else>{{ formatCurrency(items[index]!.amount) }}</span>
      </template>

      <!-- 備註 -->
      <template v-else-if="column.dataIndex === 'note'">
        <Input
          v-if="editable"
          :maxlength="1000"
          :value="items[index]!.note"
          placeholder="備註"
          size="small"
          @update:value="updateField(index, 'note', $event)"
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
          @confirm="emit('remove-row', index)"
        >
          <Button danger size="small" type="link">刪除</Button>
        </Popconfirm>
      </template>
    </template>

    <!-- 合計列 -->
    <template #summary>
      <tr class="font-semibold">
        <td />
        <td class="text-right" colspan="2">合計</td>
        <td class="text-right">{{ formatCurrency(total) }}</td>
        <td />
        <td v-if="editable" />
      </tr>
    </template>

    <template #emptyText>
      <div class="py-8 text-center text-gray-400">
        <template v-if="editable">
          尚無預算項目，請點擊「新增項目」開始編列
        </template>
        <template v-else>尚無預算項目</template>
      </div>
    </template>
  </Table>
</template>
