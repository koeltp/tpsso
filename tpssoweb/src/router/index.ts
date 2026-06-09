import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'

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
        meta: { title: '个人中心' }
      },
      {
        path: 'my-clients',
        name: 'MyClients',
        component: () => import('@/views/MyClients.vue'),
        meta: { title: '我的客户端' }
      },
      {
        path: 'client/register',
        name: 'ClientRegister',
        component: () => import('@/views/ClientRegister.vue'),
        meta: { title: '创建客户端应用' }
      }
    ]
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Login.vue'),
    meta: { title: '登录' }
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/Register.vue'),
    meta: { title: '注册' }
  },
  {
    path: '/authorize',
    name: 'Authorize',
    component: () => import('@/views/Authorize.vue'),
    meta: { title: '授权' }
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
