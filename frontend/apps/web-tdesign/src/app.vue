<script lang="ts" setup>
import type { GlobalConfigProvider } from 'tdesign-vue-next';

import { computed, watch } from 'vue';

import { usePreferences } from '@vben/preferences';

import { merge } from 'es-toolkit/compat';
import { ConfigProvider } from 'tdesign-vue-next';
import enConfig from 'tdesign-vue-next/es/locale/en_US';
import koConfig from 'tdesign-vue-next/es/locale/ko_KR';
import zhTWConfig from 'tdesign-vue-next/es/locale/zh_TW';

defineOptions({ name: 'App' });
const { isDark, locale } = usePreferences();

watch(
  () => isDark.value,
  (dark) => {
    document.documentElement.setAttribute('theme-mode', dark ? 'dark' : '');
  },
  { immediate: true },
);

const localeMap = {
  'en-US': enConfig,
  'ko-KR': koConfig,
  'zh-TW': zhTWConfig,
} as unknown as Record<string, GlobalConfigProvider>;

const customConfig: GlobalConfigProvider = {
  calendar: {},
  table: {},
  pagination: {},
};

const globalConfig = computed(() =>
  merge(localeMap[locale.value] ?? zhTWConfig, customConfig),
);
</script>

<template>
  <ConfigProvider :global-config="globalConfig">
    <RouterView />
  </ConfigProvider>
</template>
