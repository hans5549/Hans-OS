<script setup lang="ts">
import { computed, ref, watch } from 'vue';

import { Form, FormItem, Input, Modal, message } from 'ant-design-vue';
import dayjs from 'dayjs';
import customParseFormat from 'dayjs/plugin/customParseFormat';

import { createActivityApi } from '#/api';

dayjs.extend(customParseFormat);

const props = defineProps<{
  departmentId?: string;
  departmentName?: string;
  open: boolean;
  transactionDate: string;
}>();

const emit = defineEmits<{
  close: [];
  created: [activityId: string];
}>();

const saving = ref(false);
const name = ref('');
const description = ref('');

function parseTransactionDate(value: string) {
  const parsed = dayjs(value, 'YYYY-MM-DD', true);
  return parsed.isValid() ? parsed : null;
}

const activityDate = computed(() => {
  return parseTransactionDate(props.transactionDate);
});

watch(
  () => props.open,
  (open) => {
    if (open) {
      name.value = '';
      description.value = '';
    }
  },
);

async function handleSave() {
  if (!props.departmentId) {
    message.warning('請先選擇歸屬部門');
    return;
  }

  if (!name.value.trim()) {
    message.warning('請輸入活動名稱');
    return;
  }

  if (!activityDate.value) {
    message.warning('請先選擇有效的交易日期');
    return;
  }

  saving.value = true;
  try {
    const created = await createActivityApi({
      departmentId: props.departmentId,
      year: activityDate.value.year(),
      month: activityDate.value.month() + 1,
      name: name.value.trim(),
      description: description.value.trim() || undefined,
      expenses: [],
    });
    message.success('活動已新增');
    emit('created', created.id);
    emit('close');
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <Modal
    :confirm-loading="saving"
    destroy-on-close
    ok-text="建立活動"
    :open="open"
    title="快速建立來源活動"
    @cancel="emit('close')"
    @ok="handleSave"
  >
    <Form class="mt-4" layout="vertical">
      <FormItem label="歸屬部門">
        <Input :value="departmentName ?? ''" disabled />
      </FormItem>

      <FormItem label="活動年度 / 月份">
        <Input
          :value="activityDate ? `${activityDate.year()} 年 ${activityDate.month() + 1} 月` : '請先選擇有效的交易日期'"
          disabled
        />
      </FormItem>

      <FormItem label="活動名稱" required>
        <Input
          v-model:value="name"
          :maxlength="200"
          placeholder="請輸入活動名稱"
        />
      </FormItem>

      <FormItem label="活動說明">
        <Input.TextArea
          v-model:value="description"
          :maxlength="1000"
          :rows="3"
          placeholder="選填"
          show-count
        />
      </FormItem>
    </Form>
  </Modal>
</template>
