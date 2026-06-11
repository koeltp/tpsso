import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('@/layouts/GuestLayout.vue'),
    children: [
      {
        path: '',
        redirect: '/login'
      },
      {
        path: 'login',
        name: 'Login',
        component: () => import('@/views/Login.vue'),
        meta: { title: '登录' }
      },
      {
        path: 'authorize',
        name: 'Authorize',
        component: () => import('@/views/Authorize.vue'),
        meta: { title: '授权确认' }
      },
      {
        path: ':pathMatch(.*)*',
        name: 'NotFound',
        component: () => import('@/views/NotFound.vue'),
        meta: { title: '页面未找到' }
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to) => {
  document.title = `${to.meta.title || 'TPSSO'} - TPSSO`
})

export default router
