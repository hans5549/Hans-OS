import { defineOverridesPreferences } from '@vben/preferences';

/**
 * @description 项目配置文件
 * 只需要覆盖项目中的一部分配置，不需要的配置不用覆盖，会自动使用默认配置
 * !!! 更改配置后请清空缓存，否则可能不生效
 */
export const overridesPreferences = defineOverridesPreferences({
  app: {
    // Backend-driven routing: menus fetched from GET /menu/all
    accessMode: 'backend',
    // Enable refresh token flow (HttpOnly cookie)
    enableRefreshToken: true,
    // Default locale
    locale: 'zh-CN',
    name: import.meta.env.VITE_APP_TITLE,
  },
});
