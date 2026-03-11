<script setup lang="ts">
import type { ProfileResponse } from '#/api';

import { computed } from 'vue';

interface Props {
  profile: null | ProfileResponse;
}

const props = defineProps<Props>();

const items = computed(() => {
  const security = props.profile?.security;

  return [
    {
      description: security?.hasPassword
        ? '当前账号已配置登录密码'
        : '当前账号尚未配置登录密码',
      label: '账户密码',
      status: security?.hasPassword ? '已设置' : '未设置',
    },
    {
      description: security?.hasPhoneNumber
        ? '已绑定手机号码，可在基本资料中维护'
        : '尚未绑定手机号码，可在基本资料中补充',
      label: '密保手机',
      status: security?.hasPhoneNumber ? '已绑定' : '未绑定',
    },
    {
      description: security?.hasEmail
        ? '已绑定邮箱，可在基本资料中维护'
        : '尚未绑定邮箱，可在基本资料中补充',
      label: '备用邮箱',
      status: security?.hasEmail ? '已绑定' : '未绑定',
    },
    {
      description: security?.twoFactorEnabled
        ? '已启用 MFA 二次验证'
        : '尚未启用 MFA 二次验证',
      label: 'MFA 设备',
      status: security?.twoFactorEnabled ? '已启用' : '未启用',
    },
  ];
});
</script>
<template>
  <div class="space-y-4">
    <div
      v-for="item in items"
      :key="item.label"
      class="flex items-center justify-between rounded-lg border border-border bg-background p-4"
    >
      <div class="space-y-1">
        <div class="text-base font-medium">{{ item.label }}</div>
        <div class="text-sm text-muted-foreground">{{ item.description }}</div>
      </div>
      <div class="text-sm font-medium text-foreground">{{ item.status }}</div>
    </div>
  </div>
</template>
