import type { RouteRecordRaw } from 'vue-router';

import { LOGIN_PATH } from '@vben/constants';
import { preferences } from '@vben/preferences';

import { $t } from '#/locales';

const BasicLayout = () => import('#/layouts/basic.vue');
const AuthPageLayout = () => import('#/layouts/auth.vue');

function createLegacyRedirectRoute(
  name: string,
  path: string,
  redirect: string,
  title: string,
): RouteRecordRaw {
  return {
    name,
    path,
    redirect,
    meta: {
      hideInBreadcrumb: true,
      hideInMenu: true,
      hideInTab: true,
      title,
    },
  };
}
/** 全局404页面 */
const fallbackNotFoundRoute: RouteRecordRaw = {
  component: () => import('#/views/_core/fallback/not-found.vue'),
  meta: {
    hideInBreadcrumb: true,
    hideInMenu: true,
    hideInTab: true,
    title: '404',
  },
  name: 'FallbackNotFound',
  path: '/:path(.*)*',
};

/** 基本路由，这些路由是必须存在的 */
const coreRoutes: RouteRecordRaw[] = [
  // 公開部門預算頁面（不需登入）
  {
    component: () => import('#/views/public/budget/index.vue'),
    meta: {
      hideInMenu: true,
      hideInTab: true,
      ignoreAccess: true,
      title: '部門年度預算',
    },
    name: 'PublicDepartmentBudget',
    path: '/public/department-budget/:token',
  },
  /**
   * 根路由
   * 使用基础布局，作为所有页面的父级容器，子级就不必配置BasicLayout。
   * 此路由必须存在，且不应修改
   */
  {
    component: BasicLayout,
    meta: {
      hideInBreadcrumb: true,
      title: 'Root',
    },
    name: 'Root',
    path: '/',
    redirect: preferences.app.defaultHomePath,
    children: [
      {
        name: 'Index',
        path: '/index',
        component: () => import('#/views/_core/home/index.vue'),
        meta: {
          hideInBreadcrumb: true,
          hideInMenu: true,
          hideInTab: true,
          title: '首頁',
        },
      },
      {
        name: 'LegacyDashboard',
        path: '/dashboard',
        redirect: preferences.app.defaultHomePath,
        meta: {
          hideInBreadcrumb: true,
          hideInMenu: true,
          hideInTab: true,
          title: 'Legacy Dashboard Redirect',
        },
      },
      {
        name: 'LegacyAnalytics',
        path: '/analytics',
        redirect: preferences.app.defaultHomePath,
        meta: {
          hideInBreadcrumb: true,
          hideInMenu: true,
          hideInTab: true,
          title: 'Legacy Analytics Redirect',
        },
      },
      {
        name: 'LegacyWorkspace',
        path: '/workspace',
        redirect: preferences.app.defaultHomePath,
        meta: {
          hideInBreadcrumb: true,
          hideInMenu: true,
          hideInTab: true,
          title: 'Legacy Workspace Redirect',
        },
      },
      createLegacyRedirectRoute(
        'LegacySystemDesignQrCodeGenerator',
        '/system-design/qr-code-generator',
        '/system-design/real-world-apps/qr-code-generator',
        'Legacy System Design QR Code Generator Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignEarthquakeNotification',
        '/system-design/earthquake-notification',
        '/system-design/real-world-apps/earthquake-notification',
        'Legacy System Design Earthquake Notification Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignPolymarket',
        '/system-design/polymarket',
        '/system-design/real-world-apps/polymarket',
        'Legacy System Design Polymarket Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignAmazonPriceTracking',
        '/system-design/amazon-price-tracking',
        '/system-design/real-world-apps/amazon-price-tracking',
        'Legacy System Design Amazon Price Tracking Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignTeslaRoboTaxi',
        '/system-design/tesla-robo-taxi',
        '/system-design/real-world-apps/tesla-robo-taxi',
        'Legacy System Design Tesla Robo Taxi Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignSpotifyTrendingSongs',
        '/system-design/spotify-trending-songs',
        '/system-design/real-world-apps/spotify-trending-songs',
        'Legacy System Design Spotify Trending Songs Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignMessenger',
        '/system-design/messenger',
        '/system-design/real-world-apps/messenger',
        'Legacy System Design Messenger Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignWebhookPlatform',
        '/system-design/webhook-platform',
        '/system-design/real-world-apps/webhook-platform',
        'Legacy System Design Webhook Platform Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignGoogleDocs',
        '/system-design/google-docs',
        '/system-design/real-world-apps/google-docs',
        'Legacy System Design Google Docs Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignYoutube',
        '/system-design/youtube',
        '/system-design/real-world-apps/youtube',
        'Legacy System Design Youtube Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignChatgptTasks',
        '/system-design/chatgpt-tasks',
        '/system-design/real-world-apps/chatgpt-tasks',
        'Legacy System Design ChatGPT Tasks Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignAirbnbBooking',
        '/system-design/airbnb-booking',
        '/system-design/real-world-apps/airbnb-booking',
        'Legacy System Design Airbnb Booking Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignAgodaAiSupport',
        '/system-design/agoda-ai-support',
        '/system-design/real-world-apps/agoda-ai-support',
        'Legacy System Design Agoda AI Support Redirect',
      ),
      createLegacyRedirectRoute(
        'LegacySystemDesignLlmInferenceApi',
        '/system-design/llm-inference-api',
        '/system-design/real-world-apps/llm-inference-api',
        'Legacy System Design LLM Inference API Redirect',
      ),
      {
        name: 'Profile',
        path: '/profile',
        component: () => import('#/views/_core/profile/index.vue'),
        meta: {
          hideInMenu: true,
          icon: 'lucide:user',
          title: $t('page.auth.profile'),
        },
      },
    ],
  },
  {
    component: AuthPageLayout,
    meta: {
      hideInTab: true,
      title: 'Authentication',
    },
    name: 'Authentication',
    path: '/auth',
    redirect: LOGIN_PATH,
    children: [
      {
        name: 'Login',
        path: 'login',
        component: () => import('#/views/_core/authentication/login.vue'),
        meta: {
          title: $t('page.auth.login'),
        },
      },
      {
        name: 'CodeLogin',
        path: 'code-login',
        component: () => import('#/views/_core/authentication/code-login.vue'),
        meta: {
          title: $t('page.auth.codeLogin'),
        },
      },
      {
        name: 'QrCodeLogin',
        path: 'qrcode-login',
        component: () =>
          import('#/views/_core/authentication/qrcode-login.vue'),
        meta: {
          title: $t('page.auth.qrcodeLogin'),
        },
      },
      {
        name: 'ForgetPassword',
        path: 'forget-password',
        component: () =>
          import('#/views/_core/authentication/forget-password.vue'),
        meta: {
          title: $t('page.auth.forgetPassword'),
        },
      },
      {
        name: 'Register',
        path: 'register',
        component: () => import('#/views/_core/authentication/register.vue'),
        meta: {
          title: $t('page.auth.register'),
        },
      },
    ],
  },
];

export { coreRoutes, fallbackNotFoundRoute };
