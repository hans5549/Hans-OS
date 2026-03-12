import { defineOverridesPreferences } from '@vben/preferences';

/**
 * @description 專案設定檔
 * 只需要覆蓋專案中的部分設定，不需要的設定不用覆蓋，會自動使用預設值
 * !!! 更改設定後請清除快取，否則可能不會生效
 */
export const overridesPreferences = defineOverridesPreferences({
  app: {
    // Backend-driven routing: menus fetched from GET /menu/all
    accessMode: 'backend',
    // Enable refresh token flow (HttpOnly cookie)
    enableRefreshToken: true,
    // Default locale
    locale: 'zh-TW',
    name: import.meta.env.VITE_APP_TITLE,
  },
});
