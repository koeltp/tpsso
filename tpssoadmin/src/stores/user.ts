import { defineStore } from 'pinia'
import { ref, computed, readonly } from 'vue'
import { getUserInfo, type UserInfoResult } from '@/api/auth'
import {
  getAccessToken,
  getRefreshToken,
  setTokens,
  clearTokens,
  setCachedRoles,
  getCachedRoles,
  hasRole as checkRole,
  logoutOAuth,
  refreshAccessToken as refreshOAuthToken
} from '@/utils/oauth'

const TOKEN_KEY = 'admin_access_token'
const REFRESH_TOKEN_KEY = 'admin_refresh_token'

export const useUserStore = defineStore('user', () => {
  const token = ref<string | null>(getAccessToken())
  const refreshToken = ref<string | null>(getRefreshToken())
  const userInfo = ref<UserInfoResult | null>(null)
  const roles = ref<string[]>(getCachedRoles())

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() => checkRole('Admin'))

  /** 设置 token 并持久化 */
  function setAuth(accessToken: string, newRefreshToken?: string) {
    token.value = accessToken
    refreshToken.value = newRefreshToken ?? null
    setTokens(accessToken, newRefreshToken)
  }

  /** 清除所有认证信息 */
  function clearAuth() {
    token.value = null
    refreshToken.value = null
    userInfo.value = null
    roles.value = []
    clearTokens()
  }

  /** 获取当前用户信息 */
  async function fetchUserInfo() {
    if (!token.value) return
    try {
      userInfo.value = await getUserInfo()
      roles.value = userInfo.value.roles || []
      setCachedRoles(roles.value)
    } catch {
      userInfo.value = null
    }
  }

  /** 刷新 Access Token，同步 store 状态 */
  async function refreshAccessToken(): Promise<string | null> {
    const newToken = await refreshOAuthToken()
    if (newToken) {
      token.value = newToken
      // refreshOAuthToken 内部已通过 setTokens 写了 localStorage，同步到 store
      refreshToken.value = getRefreshToken()
    } 
    return newToken
  }

  /** 退出登录 */
  function logout() {
    clearAuth()
    logoutOAuth()
  }

  // 多标签页同步：其他标签页修改 localStorage 时只更新内存状态，避免级联触发 storage 事件
  window.addEventListener('storage', (e) => {
    if (e.key === TOKEN_KEY) {
      token.value = e.newValue
      if (!e.newValue) {
        refreshToken.value = null
        userInfo.value = null
        roles.value = []
      }
    }
    if (e.key === REFRESH_TOKEN_KEY) {
      refreshToken.value = e.newValue
    }
  })

  return {
    token: readonly(token),
    refreshToken: readonly(refreshToken),
    userInfo: readonly(userInfo),
    roles: readonly(roles),
    isAuthenticated,
    isAdmin,
    setAuth,
    clearAuth,
    fetchUserInfo,
    refreshAccessToken,
    logout
  }
})
