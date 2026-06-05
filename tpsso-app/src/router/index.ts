import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { userManager } from '@/auth/oidcConfig'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'Home',
    component: () => import('@/views/Home.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/callback',
    name: 'Callback',
    component: () => import('@/views/Callback.vue')
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫：未登录时跳转到认证服务器
router.beforeEach(async (to) => {
  if (to.path === '/callback') {
    return true
  }
  const user = await userManager.getUser()
  if (to.meta.requiresAuth && !user) {
    userManager.signinRedirect({ state: to.fullPath })
    return false
  }
  return true
})

export default router