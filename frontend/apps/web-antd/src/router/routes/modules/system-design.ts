import type { RouteRecordRaw } from 'vue-router';

import { $t } from '#/locales';

const routes: RouteRecordRaw[] = [
  {
    meta: {
      icon: 'lucide:server',
      order: 100,
      title: $t('page.systemDesign.title'),
    },
    name: 'SystemDesign',
    path: '/system-design',
    children: [
      {
        name: 'QrCodeGenerator',
        path: '/system-design/qr-code-generator',
        component: () =>
          import('#/views/system-design/qr-code-generator/index.vue'),
        meta: {
          icon: 'lucide:qr-code',
          title: $t('page.systemDesign.qrCodeGenerator'),
        },
      },
      {
        name: 'EarthquakeNotification',
        path: '/system-design/earthquake-notification',
        component: () =>
          import(
            '#/views/system-design/earthquake-notification/index.vue'
          ),
        meta: {
          icon: 'lucide:bell',
          title: $t('page.systemDesign.earthquakeNotification'),
        },
      },
      {
        name: 'Polymarket',
        path: '/system-design/polymarket',
        component: () =>
          import('#/views/system-design/polymarket/index.vue'),
        meta: {
          icon: 'lucide:trending-up',
          title: $t('page.systemDesign.polymarket'),
        },
      },
      {
        name: 'AmazonPriceTracking',
        path: '/system-design/amazon-price-tracking',
        component: () =>
          import('#/views/system-design/amazon-price-tracking/index.vue'),
        meta: {
          icon: 'lucide:tag',
          title: $t('page.systemDesign.amazonPriceTracking'),
        },
      },
      {
        name: 'TeslaRoboTaxi',
        path: '/system-design/tesla-robo-taxi',
        component: () =>
          import('#/views/system-design/tesla-robo-taxi/index.vue'),
        meta: {
          icon: 'lucide:car',
          title: $t('page.systemDesign.teslaRoboTaxi'),
        },
      },
      {
        name: 'SpotifyTrendingSongs',
        path: '/system-design/spotify-trending-songs',
        component: () =>
          import('#/views/system-design/spotify-trending-songs/index.vue'),
        meta: {
          icon: 'lucide:music',
          title: $t('page.systemDesign.spotifyTrendingSongs'),
        },
      },
      {
        name: 'Messenger',
        path: '/system-design/messenger',
        component: () =>
          import('#/views/system-design/messenger/index.vue'),
        meta: {
          icon: 'lucide:message-circle',
          title: $t('page.systemDesign.messenger'),
        },
      },
      {
        name: 'WebhookPlatform',
        path: '/system-design/webhook-platform',
        component: () =>
          import('#/views/system-design/webhook-platform/index.vue'),
        meta: {
          icon: 'lucide:webhook',
          title: $t('page.systemDesign.webhookPlatform'),
        },
      },
      {
        name: 'GoogleDocs',
        path: '/system-design/google-docs',
        component: () =>
          import('#/views/system-design/google-docs/index.vue'),
        meta: {
          icon: 'lucide:file-text',
          title: $t('page.systemDesign.googleDocs'),
        },
      },
      {
        name: 'Youtube',
        path: '/system-design/youtube',
        component: () => import('#/views/system-design/youtube/index.vue'),
        meta: {
          icon: 'lucide:video',
          title: $t('page.systemDesign.youtube'),
        },
      },
      {
        name: 'ChatgptTasks',
        path: '/system-design/chatgpt-tasks',
        component: () =>
          import('#/views/system-design/chatgpt-tasks/index.vue'),
        meta: {
          icon: 'lucide:bot',
          title: $t('page.systemDesign.chatgptTasks'),
        },
      },
      {
        name: 'AirbnbBooking',
        path: '/system-design/airbnb-booking',
        component: () =>
          import('#/views/system-design/airbnb-booking/index.vue'),
        meta: {
          icon: 'lucide:home',
          title: $t('page.systemDesign.airbnbBooking'),
        },
      },
      {
        name: 'AgodaAiSupport',
        path: '/system-design/agoda-ai-support',
        component: () =>
          import('#/views/system-design/agoda-ai-support/index.vue'),
        meta: {
          icon: 'lucide:headphones',
          title: $t('page.systemDesign.agodaAiSupport'),
        },
      },
      {
        name: 'LlmInferenceApi',
        path: '/system-design/llm-inference-api',
        component: () =>
          import('#/views/system-design/llm-inference-api/index.vue'),
        meta: {
          icon: 'lucide:cpu',
          title: $t('page.systemDesign.llmInferenceApi'),
        },
      },
    ],
  },
];

export default routes;
