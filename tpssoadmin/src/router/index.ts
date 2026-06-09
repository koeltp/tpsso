import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { isAuthenticated, hasRole, startOAuthLogin } from '@/utils/oauth'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Login.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/callback',
    name: 'Callback',
    component: () => import('@/views/Callback.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/forbidden',
    name: 'Forbidden',
    component: () => import('@/views/Forbidden.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/NotFound.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    component: () => import('@/layouts/Layout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: () => import('@/views/Dashboard.vue')
      },
      {
        path: 'clients',
        name: 'Clients',
        component: () => import('@/views/Client.vue')
      },
      {
        path: 'profile',
        name: 'Profile',
        component: () => import('@/views/Profile.vue')
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫：未登录跳转 SSO 授权，非管理员跳转无权限页
router.beforeEach((to) => {
  if (to.meta.requiresAuth !== false) {
    if (!isAuthenticated()) {
      startOAuthLogin(to.fullPath)
      return false
    }
    if (!hasRole('Admin')) {
      return { name: 'Forbidden' }
    }
  }
})

export default router
