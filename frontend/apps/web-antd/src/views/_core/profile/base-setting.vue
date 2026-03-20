<script setup lang="ts">
import type { Recordable } from '@vben/types';

import type { VbenFormSchema } from '#/adapter/form';

import { onMounted, ref } from 'vue';

import { ProfileBaseSetting } from '@vben/common-ui';

import { message } from 'ant-design-vue';

import { getUserInfoApi, updateProfileApi } from '#/api';
import { useAuthStore } from '#/store';

const profileBaseSettingRef = ref();

const formSchema: VbenFormSchema[] = [
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
    label: '使用者名稱',
  },
  {
    fieldName: 'email',
    component: 'Input',
    label: 'Email',
  },
  {
    fieldName: 'phone',
    component: 'Input',
    label: '電話',
  },
  {
    fieldName: 'avatar',
    component: 'Input',
    componentProps: {
      placeholder: '請輸入頭像圖片 URL',
    },
    label: '頭像 URL',
  },
  {
    fieldName: 'desc',
    component: 'Textarea',
    componentProps: {
      rows: 3,
      placeholder: '請輸入自我介紹',
    },
    label: '自我介紹',
  },
  {
    fieldName: 'roles',
    component: 'Input',
    componentProps: {
      disabled: true,
    },
    label: '角色',
  },
];

onMounted(async () => {
  const data = await getUserInfoApi();
  const formData = {
    ...data,
    roles: (data?.roles ?? []).join(', '),
  };
  profileBaseSettingRef.value.getFormApi().setValues(formData);
});

async function handleSubmit(values: Recordable<any>) {
  await updateProfileApi({
    realName: values.realName,
    email: values.email,
    phone: values.phone,
    avatar: values.avatar,
    desc: values.desc,
  });
  await useAuthStore().fetchUserInfo();
  message.success('個人資料已更新');
}
</script>
<template>
  <ProfileBaseSetting
    ref="profileBaseSettingRef"
    :form-schema="formSchema"
    @submit="handleSubmit"
  />
</template>
