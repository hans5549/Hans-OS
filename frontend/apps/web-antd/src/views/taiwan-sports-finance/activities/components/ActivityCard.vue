<script setup lang="ts">
import { computed, ref } from 'vue';

import {
  Button,
  Card,
  Col,
  Collapse,
  CollapsePanel,
  Empty,
  Form,
  FormItem,
  Input,
  InputNumber,
  message,
  Modal,
  Popconfirm,
  Row,
  Select,
  SelectOption,
  Table,
  Tag,
  Tooltip,
} from 'ant-design-vue';

import type {
  ActivityDetailResponse,
  ActivityExpenseResponse,
} from '#/api';
import { createPendingRemittanceApi } from '#/api';

const props = defineProps<{
  activity: ActivityDetailResponse;
  loading?: boolean;
}>();

const emit = defineEmits<{
  edit: [activity: ActivityDetailResponse];
  delete: [id: string];
  remittanceCreated: [];
}>();

const expanded = ref<string[]>([]);
const hasGroups = computed(() => props.activity.groups.length > 0);

const accountOptions = ['上海銀行', '合作金庫'];

const expenseColumns = [
  { dataIndex: 'description', title: '開銷說明', ellipsis: true },
  {
    dataIndex: 'amount',
    title: '金額',
    width: 120,
    align: 'right' as const,
  },
  { dataIndex: 'budgetItemName', title: '連結預算', width: 160, ellipsis: true },
  { key: 'remittanceStatus', title: '待匯款', width: 180, align: 'center' as const },
  { dataIndex: 'note', title: '備註', width: 160, ellipsis: true },
];

// ── 建立待匯款 Modal ─────────────────────────────

const remittanceModalVisible = ref(false);
const remittanceSubmitting = ref(false);
const remittanceFormState = ref({
  description: '',
  amount: 0,
  sourceAccount: '',
  targetAccount: '',
});
let pendingExpenseId = '';

function openRemittanceModal(expense: ActivityExpenseResponse) {
  pendingExpenseId = expense.id;
  remittanceFormState.value = {
    description: expense.description,
    amount: expense.amount,
    sourceAccount: '',
    targetAccount: '',
  };
  remittanceModalVisible.value = true;
}

async function handleCreateRemittance() {
  if (!remittanceFormState.value.sourceAccount) {
    message.warning('請選擇來源帳戶');
    return;
  }
  if (!remittanceFormState.value.targetAccount) {
    message.warning('請選擇目標帳戶');
    return;
  }
  remittanceSubmitting.value = true;
  try {
    await createPendingRemittanceApi({
      description: remittanceFormState.value.description,
      amount: remittanceFormState.value.amount,
      sourceAccount: remittanceFormState.value.sourceAccount,
      targetAccount: remittanceFormState.value.targetAccount,
      departmentId: props.activity.departmentId,
      activityExpenseId: pendingExpenseId,
    });
    message.success('待匯款已建立，請至「活動費待匯款」頁面處理');
    remittanceModalVisible.value = false;
    emit('remittanceCreated');
  } finally {
    remittanceSubmitting.value = false;
  }
}

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
              <template v-else-if="column.key === 'remittanceStatus'">
                <template v-if="(record as ActivityExpenseResponse).pendingRemittanceId">
                  <Tag
                    :color="(record as ActivityExpenseResponse).pendingRemittanceStatus === 0 ? 'orange' : 'green'"
                  >
                    {{ (record as ActivityExpenseResponse).pendingRemittanceStatus === 0 ? '💸 待匯款中' : '✅ 已匯款' }}
                  </Tag>
                </template>
                <Button
                  v-else
                  size="small"
                  type="dashed"
                  @click.stop="openRemittanceModal(record as unknown as ActivityExpenseResponse)"
                >
                  + 建立待匯款
                </Button>
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
          <template v-else-if="column.key === 'remittanceStatus'">
            <template v-if="(record as ActivityExpenseResponse).pendingRemittanceId">
              <Tag
                :color="(record as ActivityExpenseResponse).pendingRemittanceStatus === 0 ? 'orange' : 'green'"
              >
                {{ (record as ActivityExpenseResponse).pendingRemittanceStatus === 0 ? '💸 待匯款中' : '✅ 已匯款' }}
              </Tag>
            </template>
            <Button
              v-else
              size="small"
              type="dashed"
              @click.stop="openRemittanceModal(record as unknown as ActivityExpenseResponse)"
            >
              + 建立待匯款
            </Button>
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

  <!-- 建立待匯款 Modal -->
  <Modal
    :confirm-loading="remittanceSubmitting"
    destroy-on-close
    ok-text="建立待匯款"
    :open="remittanceModalVisible"
    title="建立待匯款"
    :width="480"
    @cancel="remittanceModalVisible = false"
    @ok="handleCreateRemittance"
  >
    <Form :model="remittanceFormState" class="mt-4" layout="vertical">
      <FormItem label="摘要說明" required>
        <Input
          v-model:value="remittanceFormState.description"
          :maxlength="200"
          placeholder="匯款說明"
        />
      </FormItem>
      <FormItem label="匯款金額" required>
        <InputNumber
          v-model:value="remittanceFormState.amount"
          class="w-full"
          :formatter="(val: string | number | undefined) => `$ ${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')"
          :min="0.01"
          :parser="(val: string | undefined) => Number(val?.replace(/\$\s?|(,*)/g, '') ?? '0')"
          :precision="2"
        />
      </FormItem>
      <Row :gutter="16">
        <Col :span="12">
          <FormItem label="來源帳戶" required>
            <Select
              v-model:value="remittanceFormState.sourceAccount"
              placeholder="請選擇"
            >
              <SelectOption v-for="acc in accountOptions" :key="acc" :value="acc">{{ acc }}</SelectOption>
            </Select>
          </FormItem>
        </Col>
        <Col :span="12">
          <FormItem label="目標帳戶" required>
            <Select
              v-model:value="remittanceFormState.targetAccount"
              placeholder="請選擇"
            >
              <SelectOption v-for="acc in accountOptions" :key="acc" :value="acc">{{ acc }}</SelectOption>
            </Select>
          </FormItem>
        </Col>
      </Row>
    </Form>
  </Modal>
</template>

<style scoped>
.activity-card :deep(.ant-card-head) {
  min-height: auto;
  padding: 8px 16px;
}
</style>
