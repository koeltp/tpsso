import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores/user'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('@/layouts/Layout.vue'),
    children: [
      {
        path: '',
        name: 'Home',
        component: () => import('@/views/Home.vue'),
        meta: { title: 'TPSSO' }
      },
      {
        path: 'profile',
        name: 'Profile',
        component: () => import('@/views/Profile.vue'),
        meta: { title: '个人中心', requiresAuth: true }
      },
      {
        path: 'my-clients',
        name: 'MyClients',
        component: () => import('@/views/MyClients.vue'),
        meta: { title: '我的客户端', requiresAuth: true }
      },
      {
        path: 'client/register',
        name: 'ClientRegister',
        component: () => import('@/views/ClientRegister.vue'),
        meta: { title: '创建客户端应用', requiresAuth: true }
      }
    ]
  },
  {
    path: '/',
    component: () => import('@/layouts/GuestLayout.vue'),
    children: [
      {
        path: 'login',
        name: 'Login',
        component: () => import('@/views/Login.vue'),
        meta: { title: '登录' }
      },
      {
        path: 'register',
        name: 'Register',
        component: () => import('@/views/Register.vue'),
        meta: { title: '注册' }
      },
      {
        path: 'authorize',
        name: 'Authorize',
        component: () => import('@/views/Authorize.vue'),
        meta: { title: '授权' }
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

  const requiresAuth = to.matched.some(record => record.meta.requiresAuth)
  if (requiresAuth) {
    const userStore = useUserStore()
    if (!userStore.isAuthenticated) {
      return { name: 'Login', query: { returnUrl: to.fullPath } }
    }
  }
})

export default router
