import type { Locale } from 'ant-design-vue/es/locale';

import type { App } from 'vue';

import type { LocaleSetupOptions, SupportedLanguagesType } from '@vben/locales';

import { ref } from 'vue';

import {
  $t,
  setupI18n as coreSetup,
  loadLocalesMapFromDir,
} from '@vben/locales';
import { preferences } from '@vben/preferences';

import antdEnLocale from 'ant-design-vue/es/locale/en_US';
import antdKoLocale from 'ant-design-vue/es/locale/ko_KR';
import antdDefaultLocale from 'ant-design-vue/es/locale/zh_TW';
import dayjs from 'dayjs';

const antdLocale = ref<Locale>(antdDefaultLocale);

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
  await Promise.all([loadAntdLocale(lang), loadDayjsLocale(lang)]);
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
 * 加载antd的语言包
 * @param lang
 */
function loadAntdLocale(lang: SupportedLanguagesType) {
  const antdLocaleMap: Record<SupportedLanguagesType, Locale> = {
    'en-US': antdEnLocale,
    'ko-KR': antdKoLocale,
    'zh-TW': antdDefaultLocale,
  };
  antdLocale.value = antdLocaleMap[lang] ?? antdDefaultLocale;
}

async function setupI18n(app: App, options: LocaleSetupOptions = {}) {
  await coreSetup(app, {
    defaultLocale: preferences.app.locale,
    loadMessages,
    missingWarn: !import.meta.env.PROD,
    ...options,
  });
}

export { $t, antdLocale, setupI18n };
