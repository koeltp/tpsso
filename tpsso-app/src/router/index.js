import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import CallbackView from '../views/CallbackView.vue'
import { userManager } from '@/auth/oidcConfig'

const routes = [
  { path: '/', name: 'home', component: HomeView, meta: { requiresAuth: true } },
  { path: '/callback', name: 'callback', component: CallbackView },
  { path: '/about', name: 'about', component: () => import('../views/AboutView.vue'), meta: { requiresAuth: true } }
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
    userManager.signinRedirect({ data: { redirectUrl: to.fullPath } })
    return false
  }
  return true
})

export default router