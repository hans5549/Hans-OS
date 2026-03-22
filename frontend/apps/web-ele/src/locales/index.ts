import type { Language } from 'element-plus/es/locale';

import type { App } from 'vue';

import type { LocaleSetupOptions, SupportedLanguagesType } from '@vben/locales';

import { ref } from 'vue';

import {
  $t,
  setupI18n as coreSetup,
  loadLocalesMapFromDir,
} from '@vben/locales';
import { preferences } from '@vben/preferences';

import dayjs from 'dayjs';
import enLocale from 'element-plus/es/locale/lang/en';
import koLocale from 'element-plus/es/locale/lang/ko';
import defaultLocale from 'element-plus/es/locale/lang/zh-tw';

const elementLocale = ref<Language>(defaultLocale);

const modules = import.meta.glob('./langs/**/*.json');

const localesMap = loadLocalesMapFromDir(
  /\.\/langs\/([^/]+)\/(.*)\.json$/,
  modules,
);
/**
 * 加载应用特有的语言包
 * 这里也可以改造为从服务端获取翻译数据
 * @param lang
 */
async function loadMessages(lang: SupportedLanguagesType) {
  const [appLocaleMessages] = await Promise.all([
    localesMap[lang]?.(),
    loadThirdPartyMessage(lang),
  ]);
  return appLocaleMessages?.default;
}

/**
 * 加载第三方组件库的语言包
 * @param lang
 */
async function loadThirdPartyMessage(lang: SupportedLanguagesType) {
  await Promise.all([loadElementLocale(lang), loadDayjsLocale(lang)]);
}

/**
 * 加载dayjs的语言包
 * @param lang
 */
async function loadDayjsLocale(lang: SupportedLanguagesType) {
  const dayjsLocaleMap: Record<SupportedLanguagesType, () => Promise<any>> = {
    'en-US': () => import('dayjs/locale/en'),
    'ko-KR': () => import('dayjs/locale/ko'),
    'zh-TW': () => import('dayjs/locale/zh-tw'),
  };
  const { default: locale } = await (
    dayjsLocaleMap[lang] ?? dayjsLocaleMap['en-US']
  )();
  dayjs.locale(locale);
}

/**
 * 加载element-plus的语言包
 * @param lang
 */
function loadElementLocale(lang: SupportedLanguagesType) {
  const elementLocaleMap: Record<SupportedLanguagesType, Language> = {
    'en-US': enLocale,
    'ko-KR': koLocale,
    'zh-TW': defaultLocale,
  };
  elementLocale.value = elementLocaleMap[lang] ?? defaultLocale;
}

async function setupI18n(app: App, options: LocaleSetupOptions = {}) {
  await coreSetup(app, {
    defaultLocale: preferences.app.locale,
    loadMessages,
    missingWarn: !import.meta.env.PROD,
    ...options,
  });
}

export { $t, elementLocale, setupI18n };
