<script setup lang="ts">
import type { VbenFormSchema } from '#/adapter/form';
import type { ProfileResponse } from '#/api';

import { computed, ref, watch } from 'vue';

import { ProfileBaseSetting } from '@vben/common-ui';

import { message } from 'ant-design-vue';

import { updateProfileBasicApi } from '#/api';

interface Props {
  profile: null | ProfileResponse;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  updated: [];
}>();

const profileBaseSettingRef = ref();

const formSchema = computed((): VbenFormSchema[] => {
  return [
    {
      fieldName: 'realName',
      component: 'Input',
      label: '姓名',
    },
    {
      fieldName: 'username',
      component: 'Input',
      componentProps: {
        disabled: true,
      },
      label: '用户名',
    },
    {
      fieldName: 'rolesDisplay',
      component: 'Input',
      componentProps: {
        disabled: true,
      },
      label: '角色',
    },
    {
      fieldName: 'email',
      component: 'Input',
      label: '邮箱',
    },
    {
      fieldName: 'phoneNumber',
      component: 'Input',
      label: '手机号码',
    },
    {
      fieldName: 'introduction',
      component: 'Textarea',
      label: '个人简介',
    },
  ];
});

watch(
  () => props.profile,
  (profile) => {
    if (!profile || !profileBaseSettingRef.value) {
      return;
    }

    profileBaseSettingRef.value.getFormApi().setValues({
      email: profile.basic.email,
      introduction: profile.basic.introduction,
      phoneNumber: profile.basic.phoneNumber,
      realName: profile.basic.realName,
      rolesDisplay: profile.basic.roles.join(', '),
      username: profile.basic.username,
    });
  },
  { immediate: true },
);

async function handleSubmit(values: Record<string, string>) {
  await updateProfileBasicApi({
    email: values.email?.trim() ?? '',
    introduction: values.introduction?.trim() ?? '',
    phoneNumber: values.phoneNumber?.trim() ?? '',
    realName: values.realName?.trim() ?? '',
  });
  message.success('基本资料已更新');
  emit('updated');
}
</script>
<template>
  <ProfileBaseSetting
    ref="profileBaseSettingRef"
    :form-schema="formSchema"
    @submit="handleSubmit"
  />
</template>
