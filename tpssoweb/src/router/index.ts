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
        component: () => import('@/views/Home.vue')
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
        path: 'client/register',
        name: 'ClientRegister',
        component: () => import('@/views/ClientRegister.vue')
      }
    ]
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Login.vue')
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/Register.vue')
  },
  {
    path: '/authorize',
    name: 'Authorize',
    component: () => import('@/views/Authorize.vue')
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
