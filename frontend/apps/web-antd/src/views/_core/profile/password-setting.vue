<script setup lang="ts">
import type { Recordable } from '@vben/types';

import type { VbenFormSchema } from '#/adapter/form';

import { ref } from 'vue';

import { ProfilePasswordSetting, z } from '@vben/common-ui';

import { message } from 'ant-design-vue';

import { changePasswordApi } from '#/api';

const passwordSettingRef = ref();

const formSchema: VbenFormSchema[] = [
  {
    fieldName: 'oldPassword',
    label: '舊密碼',
    component: 'VbenInputPassword',
    componentProps: {
      placeholder: '請輸入舊密碼',
    },
  },
  {
    fieldName: 'newPassword',
    label: '新密碼',
    component: 'VbenInputPassword',
    componentProps: {
      passwordStrength: true,
      placeholder: '請輸入新密碼',
    },
  },
  {
    fieldName: 'confirmPassword',
    label: '確認密碼',
    component: 'VbenInputPassword',
    componentProps: {
      passwordStrength: true,
      placeholder: '請再次輸入新密碼',
    },
    dependencies: {
      rules(values) {
        const { newPassword } = values;
        return z
          .string({ required_error: '請再次輸入新密碼' })
          .min(1, { message: '請再次輸入新密碼' })
          .refine((value) => value === newPassword, {
            message: '兩次輸入的密碼不一致',
          });
      },
      triggerFields: ['newPassword'],
    },
  },
];

async function handleSubmit(values: Recordable<any>) {
  await changePasswordApi({
    oldPassword: values.oldPassword,
    newPassword: values.newPassword,
  });
  message.success('密碼修改成功');
  passwordSettingRef.value.getFormApi().resetForm();
}
</script>
<template>
  <ProfilePasswordSetting
    ref="passwordSettingRef"
    class="w-1/3"
    :form-schema="formSchema"
    @submit="handleSubmit"
  />
</template>
