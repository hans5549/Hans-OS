<script setup lang="ts">
import { computed } from 'vue';

import { ProfileNotificationSetting } from '@vben/common-ui';

const formSchema = computed(() => {
  return [
    {
      value: true,
      fieldName: 'accountPassword',
      label: '账户密码',
      description: '其他使用者的消息将以站内信的形式通知',
    },
    {
      value: true,
      fieldName: 'systemMessage',
      label: '系統消息',
      description: '系統消息将以站内信的形式通知',
    },
    {
      value: true,
      fieldName: 'todoTask',
      label: '待办任务',
      description: '待办任务将以站内信的形式通知',
    },
  ];
});
</script>
<template>
  <ProfileNotificationSetting :form-schema="formSchema" />
</template>
