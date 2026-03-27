<script setup lang="ts">
import { computed, ref } from 'vue';

import {
  Button,
  Card,
  Collapse,
  CollapsePanel,
  Empty,
  Popconfirm,
  Table,
  Tag,
  Tooltip,
} from 'ant-design-vue';

import type { ActivityDetailResponse } from '#/api';

const props = defineProps<{
  activity: ActivityDetailResponse;
  loading?: boolean;
}>();

const emit = defineEmits<{
  edit: [activity: ActivityDetailResponse];
  delete: [id: string];
}>();

const expanded = ref<string[]>([]);

const hasGroups = computed(() => props.activity.groups.length > 0);

const expenseColumns = [
  { dataIndex: 'description', title: '開銷說明', ellipsis: true },
  {
    dataIndex: 'amount',
    title: '金額',
    width: 120,
    align: 'right' as const,
  },
  { dataIndex: 'budgetItemName', title: '連結預算', width: 160, ellipsis: true },
  { dataIndex: 'note', title: '備註', width: 160, ellipsis: true },
];

function formatCurrency(value: number): string {
  return value.toLocaleString('zh-TW');
}
</script>

<template>
  <Card
    size="small"
    class="activity-card mb-3"
    :loading="loading"
  >
    <template #title>
      <div class="flex items-center gap-2">
        <span class="text-base font-medium">{{ activity.name }}</span>
        <Tag v-if="hasGroups" color="blue">
          {{ activity.groups.length }} 個分組
        </Tag>
        <Tag v-else color="default">簡單活動</Tag>
      </div>
    </template>

    <template #extra>
      <div class="flex items-center gap-2">
        <span class="mr-2 text-base font-semibold">
          ${{ formatCurrency(activity.totalAmount) }}
        </span>
        <Button size="small" type="link" @click="emit('edit', activity)">
          編輯
        </Button>
        <Popconfirm
          title="確定要刪除此活動？"
          ok-text="確定"
          cancel-text="取消"
          @confirm="emit('delete', activity.id)"
        >
          <Button danger size="small" type="link">刪除</Button>
        </Popconfirm>
      </div>
    </template>

    <p
      v-if="activity.description"
      class="mb-3 text-sm text-gray-500"
    >
      {{ activity.description }}
    </p>

    <!-- 有分組的活動 -->
    <template v-if="hasGroups">
      <Collapse v-model:activeKey="expanded" ghost>
        <CollapsePanel
          v-for="group in activity.groups"
          :key="group.id"
          :header="`${group.name}（小計: $${formatCurrency(group.subTotal)}）`"
        >
          <Table
            :columns="expenseColumns"
            :data-source="group.expenses"
            :pagination="false"
            row-key="id"
            size="small"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.dataIndex === 'amount'">
                ${{ formatCurrency(record.amount) }}
              </template>
              <template v-else-if="column.dataIndex === 'budgetItemName'">
                <Tooltip v-if="record.budgetItemName" :title="record.budgetItemName">
                  <Tag color="geekblue" class="max-w-[140px] truncate">
                    {{ record.budgetItemName }}
                  </Tag>
                </Tooltip>
                <span v-else class="text-gray-400">—</span>
              </template>
              <template v-else-if="column.dataIndex === 'note'">
                {{ record.note || '—' }}
              </template>
            </template>
          </Table>
        </CollapsePanel>
      </Collapse>
    </template>

    <!-- 無分組的開銷 -->
    <template v-if="activity.ungroupedExpenses.length > 0">
      <Table
        :columns="expenseColumns"
        :data-source="activity.ungroupedExpenses"
        :pagination="false"
        row-key="id"
        size="small"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.dataIndex === 'amount'">
            ${{ formatCurrency(record.amount) }}
          </template>
          <template v-else-if="column.dataIndex === 'budgetItemName'">
            <Tooltip v-if="record.budgetItemName" :title="record.budgetItemName">
              <Tag color="geekblue" class="max-w-[140px] truncate">
                {{ record.budgetItemName }}
              </Tag>
            </Tooltip>
            <span v-else class="text-gray-400">—</span>
          </template>
          <template v-else-if="column.dataIndex === 'note'">
            {{ record.note || '—' }}
          </template>
        </template>
      </Table>
    </template>

    <!-- 空狀態 -->
    <Empty
      v-if="!hasGroups && activity.ungroupedExpenses.length === 0"
      description="尚未新增開銷項目"
    />
  </Card>
</template>

<style scoped>
.activity-card :deep(.ant-card-head) {
  min-height: auto;
  padding: 8px 16px;
}
</style>
