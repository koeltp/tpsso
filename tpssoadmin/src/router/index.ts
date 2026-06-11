import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { startOAuthLogin } from '@/utils/oauth'
import { useUserStore } from '@/stores/user'

const routes: RouteRecordRaw[] = [
  // ──────── 公共页面（GuestLayout） ────────
  {
    path: '/',
    component: () => import('@/layouts/GuestLayout.vue'),
    children: [
      {
        path: '',
        name: 'Home',
        component: () => import('@/views/public/Home.vue'),
        meta: { fullPage: true }
      },
      {
        path: 'login',
        name: 'Login',
        component: () => import('@/views/public/Login.vue')
      },
      {
        path: 'register',
        name: 'Register',
        component: () => import('@/views/public/Register.vue')
      },
      {
        path: 'callback',
        name: 'Callback',
        component: () => import('@/views/public/Callback.vue')
      },
      {
        path: 'forbidden',
        name: 'Forbidden',
        component: () => import('@/views/public/Forbidden.vue')
      }
    ]
  },

  // ──────── 管理员布局（Layout - 侧边栏） ────────
  {
    path: '/admin',
    component: () => import('@/layouts/Layout.vue'),
    meta: { requiresAuth: true, requiresAdmin: true },
    children: [
      {
        path: 'dashboard',
        name: 'AdminDashboard',
        component: () => import('@/views/dashboard/AdminDashboard.vue')
      },
      {
        path: 'clients',
        name: 'Clients',
        component: () => import('@/views/client/Client.vue')
      },
      {
        path: 'users',
        name: 'Users',
        component: () => import('@/views/account/User.vue')
      },
      {
        path: 'dict',
        name: 'Dict',
        component: () => import('@/views/system/Dict.vue')
      }
    ]
  },

  // ──────── 普通用户布局（UserLayout - 顶部导航） ────────
  {
    path: '/',
    component: () => import('@/layouts/UserLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: 'dashboard',
        name: 'UserDashboard',
        component: () => import('@/views/dashboard/UserDashboard.vue')
      },
      {
        path: 'profile',
        name: 'Profile',
        component: () => import('@/views/account/Profile.vue')
      },
      {
        path: 'my-clients',
        name: 'MyClients',
        component: () => import('@/views/client/MyClients.vue')
      },
      {
        path: 'my-clients/:id/users',
        name: 'ClientUsers',
        component: () => import('@/views/client/ClientUsers.vue')
      },
      {
        path: 'my-apps',
        name: 'MyApps',
        component: () => import('@/views/client/MyApps.vue')
      }
    ]
  },

  // ──────── 404 兜底 ────────
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/public/NotFound.vue')
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫
router.beforeEach(async (to) => {
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth)

  if (requiresAuth) {
    const userStore = useUserStore()

    // 未登录 → 跳转 SSO
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

    // Admin 用户访问普通用户路由时，重定向到管理端
    if (userStore.isAdmin && !to.matched.some(record => record.meta.requiresAdmin)) {
      if (to.path === '/dashboard') {
        return { name: 'AdminDashboard' }
      }
      if (to.path === '/my-clients') {
        return { path: '/admin/clients' }
      }
    }

    // 已登录用户访问根路径 / 时，根据角色跳转
    if (to.path === '/' && to.name === 'Home') {
      if (userStore.isAdmin) {
        return { name: 'AdminDashboard' }
      }
      return { name: 'UserDashboard' }
    }
  }
})

export default router
