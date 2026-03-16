<script lang="ts" setup>
import type { GlobalThemeOverrides } from 'naive-ui';

import { computed } from 'vue';

import { useNaiveDesignTokens } from '@vben/hooks';
import { preferences } from '@vben/preferences';

import {
  darkTheme,
  dateEnUS,
  dateKoKR,
  dateZhTW,
  enUS,
  koKR,
  lightTheme,
  NConfigProvider,
  NMessageProvider,
  NNotificationProvider,
  zhTW,
} from 'naive-ui';

defineOptions({ name: 'App' });

const { commonTokens } = useNaiveDesignTokens();

const localeMap = { 'en-US': enUS, 'ko-KR': koKR, 'zh-TW': zhTW };
const dateLocaleMap = { 'en-US': dateEnUS, 'ko-KR': dateKoKR, 'zh-TW': dateZhTW };

const tokenLocale = computed(() =>
  localeMap[preferences.app.locale] ?? enUS,
);
const tokenDateLocale = computed(() =>
  dateLocaleMap[preferences.app.locale] ?? dateEnUS,
);
const tokenTheme = computed(() =>
  preferences.theme.mode === 'dark' ? darkTheme : lightTheme,
);

const themeOverrides = computed((): GlobalThemeOverrides => ({
  common: commonTokens,
}));
</script>

<template>
  <NConfigProvider
    :date-locale="tokenDateLocale"
    :locale="tokenLocale"
    :theme="tokenTheme"
    :theme-overrides="themeOverrides"
    class="h-full"
  >
    <NNotificationProvider>
      <NMessageProvider>
        <RouterView />
      </NMessageProvider>
    </NNotificationProvider>
  </NConfigProvider>
</template>
