import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import Home from '../views/Home.vue'
import Callback from '../views/Callback.vue'
import { userManager } from '@/auth/oidcConfig'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: Home, meta: { requiresAuth: true } },
  { path: '/callback', name: 'callback', component: Callback },
  { path: '/about', name: 'about', component: () => import('../views/About.vue'), meta: { requiresAuth: true } }
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