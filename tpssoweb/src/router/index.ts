import { createRouter, createWebHistory } from 'vue-router'
import Login from '../views/Login.vue'

const routes = [
  { path: '/custom-login', component: Login },
  { path: '/:pathMatch(.*)*', redirect: '/custom-login' }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router