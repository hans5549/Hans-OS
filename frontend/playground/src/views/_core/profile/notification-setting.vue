<script setup lang="ts">
import type { ProfileNotificationsResponse, ProfileResponse } from '#/api';

import { computed, ref, watch } from 'vue';

import { ProfileNotificationSetting } from '@vben/common-ui';

import { message } from 'ant-design-vue';

import { updateProfileNotificationsApi } from '#/api';

interface Props {
  profile: null | ProfileResponse;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  updated: [];
}>();

const notificationState = ref<ProfileNotificationsResponse>({
  notifyAccountPassword: true,
  notifySystemMessage: true,
  notifyTodoTask: true,
});

const formSchema = computed(() => {
  return [
    {
      value: notificationState.value.notifyAccountPassword,
      fieldName: 'notifyAccountPassword',
      label: '账户密码',
      description: '密码相关消息会通过站内通知提醒',
    },
    {
      value: notificationState.value.notifySystemMessage,
      fieldName: 'notifySystemMessage',
      label: '系统消息',
      description: '系统更新与公告会通过站内通知提醒',
    },
    {
      value: notificationState.value.notifyTodoTask,
      fieldName: 'notifyTodoTask',
      label: '待办任务',
      description: '任务变更会通过站内通知提醒',
    },
  ];
});

watch(
  () => props.profile?.notifications,
  (notifications) => {
    if (!notifications) {
      return;
    }

    notificationState.value = { ...notifications };
  },
  { immediate: true },
);

async function handleChange({
  fieldName,
  value,
}: {
  fieldName: keyof ProfileNotificationsResponse;
  value: boolean;
}) {
  notificationState.value = {
    ...notificationState.value,
    [fieldName]: value,
  };

  await updateProfileNotificationsApi(notificationState.value);
  message.success('通知设置已更新');
  emit('updated');
}
</script>
<template>
  <ProfileNotificationSetting
    :form-schema="formSchema"
    @change="handleChange"
  />
</template>
