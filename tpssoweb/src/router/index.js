import { createRouter, createWebHistory } from 'vue-router'
import LoginPage from '../views/LoginPage.vue'

const routes = [
  { path: '/custom-login', component: LoginPage },
  { path: '/:pathMatch(.*)*', redirect: '/custom-login' }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router