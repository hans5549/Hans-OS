import type { RouteRecordRaw } from 'vue-router';

import { $t } from '#/locales';

const routes: RouteRecordRaw[] = [
  {
    component: () => import('#/views/dashboard/todo/index.vue'),
    meta: {
      fullPathKey: false,
      icon: 'lucide:check-square',
      order: 1,
      title: $t('page.todo.title'),
    },
    name: 'Todo',
    path: '/todo',
  },
];

export default routes;
