<script setup lang="ts">
import { computed } from 'vue';

import { Badge, Menu, MenuItem } from 'ant-design-vue';

import type { MonthSummaryResponse } from '#/api';

const props = defineProps<{
  monthSummaries: MonthSummaryResponse[];
  selectedMonth: number;
}>();

const emit = defineEmits<{
  'update:selectedMonth': [month: number];
}>();

const months = [
  { label: '1月', value: 1 },
  { label: '2月', value: 2 },
  { label: '3月', value: 3 },
  { label: '4月', value: 4 },
  { label: '5月', value: 5 },
  { label: '6月', value: 6 },
  { label: '7月', value: 7 },
  { label: '8月', value: 8 },
  { label: '9月', value: 9 },
  { label: '10月', value: 10 },
  { label: '11月', value: 11 },
  { label: '12月', value: 12 },
];

const summaryMap = computed(() => {
  const map = new Map<number, MonthSummaryResponse>();
  for (const s of props.monthSummaries) {
    map.set(s.month, s);
  }
  return map;
});

function getCount(month: number): number {
  return summaryMap.value.get(month)?.activityCount ?? 0;
}

function handleSelect(info: { key: string | number }) {
  emit('update:selectedMonth', Number(info.key));
}
</script>

<template>
  <Menu
    :selected-keys="[String(selectedMonth)]"
    mode="inline"
    class="month-sidebar"
    @select="handleSelect"
  >
    <MenuItem v-for="m in months" :key="String(m.value)">
      <div class="flex items-center justify-between">
        <span>{{ m.label }}</span>
        <Badge
          v-if="getCount(m.value) > 0"
          :count="getCount(m.value)"
          :number-style="{ backgroundColor: 'var(--ant-color-primary)' }"
        />
      </div>
    </MenuItem>
  </Menu>
</template>

<style scoped>
.month-sidebar {
  border-inline-end: none !important;
}
</style>
