import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { getUserInfo, type UserInfoResult } from '@/api/auth'
import {
  getAccessToken,
  setCachedRoles,
  getCachedRoles,
  hasRole as checkRole,
  isAuthenticated as checkAuth,
  logoutOAuth
} from '@/utils/oauth'

export const useUserStore = defineStore('user', () => {
  const userInfo = ref<UserInfoResult | null>(null)
  const roles = ref<string[]>(getCachedRoles())

  const isAuthenticated = computed(() => checkAuth())
  const isAdmin = computed(() => checkRole('Admin'))

  /** 获取当前用户信息 */
  async function fetchUserInfo() {
    if (!getAccessToken()) return
    try {
      userInfo.value = await getUserInfo()
      roles.value = userInfo.value.roles || []
      setCachedRoles(roles.value)
    } catch {
      userInfo.value = null
    }
  }

  /** 退出登录 */
  function logout() {
    logoutOAuth()
    userInfo.value = null
    roles.value = []
  }

  return { userInfo, roles, isAuthenticated, isAdmin, fetchUserInfo, logout }
})
