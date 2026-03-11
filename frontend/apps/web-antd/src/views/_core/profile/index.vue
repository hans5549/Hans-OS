<script setup lang="ts">
import type { BasicUserInfo } from '@vben/types';
import type { ProfileResponse } from '#/api';

import { computed, onMounted, ref } from 'vue';

import { Profile } from '@vben/common-ui';
import { useUserStore } from '@vben/stores';

import { getProfileApi } from '#/api';
import { useAuthStore } from '#/store';
import ProfileBase from './base-setting.vue';
import ProfileNotificationSetting from './notification-setting.vue';
import ProfilePasswordSetting from './password-setting.vue';
import ProfileSecuritySetting from './security-setting.vue';

const authStore = useAuthStore();
const userStore = useUserStore();

const profile = ref<null | ProfileResponse>(null);
const tabsValue = ref<string>('basic');

const tabs = ref([
  {
    label: '基本设置',
    value: 'basic',
  },
  {
    label: '安全设置',
    value: 'security',
  },
  {
    label: '修改密码',
    value: 'password',
  },
  {
    label: '新消息提醒',
    value: 'notice',
  },
]);

const profileUserInfo = computed<BasicUserInfo | null>(() => {
  if (!profile.value) {
    return userStore.userInfo;
  }

  return {
    avatar: profile.value.header.avatar,
    realName: profile.value.header.realName,
    roles: profile.value.header.roles,
    userId: profile.value.header.userId,
    username: profile.value.header.username,
  };
});

async function loadProfile() {
  profile.value = await getProfileApi();
}

async function handleBasicUpdated() {
  await Promise.all([authStore.fetchUserInfo(), loadProfile()]);
}

onMounted(loadProfile);
</script>
<template>
  <Profile
    v-model:model-value="tabsValue"
    title="个人中心"
    :user-info="profileUserInfo"
    :tabs="tabs"
  >
    <template #content>
      <ProfileBase
        v-if="tabsValue === 'basic'"
        :profile="profile"
        @updated="handleBasicUpdated"
      />
      <ProfileSecuritySetting v-if="tabsValue === 'security'" :profile="profile" />
      <ProfilePasswordSetting v-if="tabsValue === 'password'" />
      <ProfileNotificationSetting
        v-if="tabsValue === 'notice'"
        :profile="profile"
        @updated="loadProfile"
      />
    </template>
  </Profile>
</template>
