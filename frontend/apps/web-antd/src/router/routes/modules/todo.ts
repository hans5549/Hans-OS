import type { RouteRecordRaw } from 'vue-router';

import { $t } from '#/locales';

const routes: RouteRecordRaw[] = [
  {
    meta: {
      icon: 'lucide:check-square',
      order: 1,
      title: $t('page.todo.title'),
    },
    name: 'Todo',
    path: '/todo',
    redirect: '/todo/today',
    children: [
      {
        name: 'TodoToday',
        path: '/todo/today',
        component: () => import('#/views/dashboard/todo/index.vue'),
        meta: { hideInMenu: true, title: $t('page.todo.today') },
      },
      {
        name: 'TodoInbox',
        path: '/todo/inbox',
        component: () => import('#/views/dashboard/todo/index.vue'),
        meta: { hideInMenu: true, title: $t('page.todo.inbox') },
      },
      {
        name: 'TodoUpcoming',
        path: '/todo/upcoming',
        component: () => import('#/views/dashboard/todo/index.vue'),
        meta: { hideInMenu: true, title: $t('page.todo.upcoming') },
      },
      {
        name: 'TodoWeek',
        path: '/todo/week',
        component: () => import('#/views/dashboard/todo/index.vue'),
        meta: { hideInMenu: true, title: $t('page.todo.week') },
      },
      {
        name: 'TodoAll',
        path: '/todo/all',
        component: () => import('#/views/dashboard/todo/index.vue'),
        meta: { hideInMenu: true, title: $t('page.todo.all') },
      },
      {
        name: 'TodoTrash',
        path: '/todo/trash',
        component: () => import('#/views/dashboard/todo/index.vue'),
        meta: { hideInMenu: true, title: $t('page.todo.trash') },
      },
      {
        name: 'TodoProject',
        path: '/todo/project/:id',
        component: () => import('#/views/dashboard/todo/index.vue'),
        meta: { hideInMenu: true, title: $t('page.todo.project') },
      },
      {
        name: 'TodoTag',
        path: '/todo/tag/:id',
        component: () => import('#/views/dashboard/todo/index.vue'),
        meta: { hideInMenu: true, title: $t('page.todo.tag') },
      },
    ],
  },
];

export default routes;
