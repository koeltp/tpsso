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
        path: 'profile',
        name: 'Profile',
        component: () => import('@/views/Profile.vue')
      },
      {
        path: 'my-clients',
        name: 'MyClients',
        component: () => import('@/views/MyClients.vue')
      },
      {
        path: 'my-clients/:id/users',
        name: 'ClientUsers',
        component: () => import('@/views/ClientUsers.vue')
      },
      {
        path: 'my-apps',
        name: 'MyApps',
        component: () => import('@/views/MyApps.vue')
      },
      {
        path: 'clients',
        name: 'Clients',
        component: () => import('@/views/Client.vue'),
        meta: { requiresAdmin: true }
      },
      {
        path: 'users',
        name: 'Users',
        component: () => import('@/views/User.vue'),
        meta: { requiresAdmin: true }
      },
      {
        path: 'dict',
        name: 'Dict',
        component: () => import('@/views/Dict.vue'),
        meta: { requiresAdmin: true }
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

// 路由守卫：未登录跳转 SSO 授权，非管理员访问管理页面跳转 Forbidden
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
    // Admin 专属页面检查
    const requiresAdmin = to.matched.some(record => record.meta.requiresAdmin)
    if (requiresAdmin && !userStore.isAdmin) {
      return { name: 'Forbidden' }
    }
  }
})

export default router
