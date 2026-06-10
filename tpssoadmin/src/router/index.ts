import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { startOAuthLogin } from '@/utils/oauth'
import { useUserStore } from '@/stores/user'

const routes: RouteRecordRaw[] = [
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
        path: 'users',
        name: 'Users',
        component: () => import('@/views/User.vue')
      },
      {
        path: 'profile',
        name: 'Profile',
        component: () => import('@/views/Profile.vue')
      }
    ]
  },
  {
    path: '/',
    component: () => import('@/layouts/GuestLayout.vue'),
    meta: { requiresAuth: false },
    children: [
      {
        path: 'login',
        name: 'Login',
        component: () => import('@/views/Login.vue')
      },
      {
        path: 'callback',
        name: 'Callback',
        component: () => import('@/views/Callback.vue')
      },
      {
        path: 'forbidden',
        name: 'Forbidden',
        component: () => import('@/views/Forbidden.vue')
      },
      {
        path: ':pathMatch(.*)*',
        name: 'NotFound',
        component: () => import('@/views/NotFound.vue')
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫：未登录跳转 SSO 授权，非管理员跳转无权限页
router.beforeEach(async (to) => {
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth)
  if (requiresAuth) {
    const userStore = useUserStore()
    if (!userStore.isAuthenticated) {
      startOAuthLogin(to.fullPath)
      return false
    }
    // 刷新页面时 userInfo 丢失，需要重新获取
    if (!userStore.userInfo) {
      await userStore.fetchUserInfo()
    }
    if (!userStore.isAdmin) {
      return { name: 'Forbidden' }
    }
  }
})

export default router
