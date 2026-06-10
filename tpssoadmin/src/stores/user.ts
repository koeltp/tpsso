import { defineStore } from 'pinia'
import { ref, computed, readonly } from 'vue'
import { getUserInfo, type UserInfoResult } from '@/api/auth'
import { ROLE } from '@/utils/client'
import {
  exchangeCodeForToken,
  refreshAccessToken as refreshOAuthToken,
  logoutOAuth
} from '@/utils/oauth'

const TOKEN_KEY = 'admin_access_token'
const REFRESH_TOKEN_KEY = 'admin_refresh_token'

export const useUserStore = defineStore('user', () => {
  const token = ref<string | null>(localStorage.getItem(TOKEN_KEY))
  const refreshToken = ref<string | null>(localStorage.getItem(REFRESH_TOKEN_KEY))
  const userInfo = ref<UserInfoResult | null>(null)

  // 防止并发刷新
  let refreshingPromise: Promise<string | null> | null = null

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() => userInfo.value?.roles?.includes(ROLE.Admin) ?? false)

  /** 设置 token 并持久化到 localStorage */
  function setAuth(accessToken: string, newRefreshToken?: string) {
    token.value = accessToken
    refreshToken.value = newRefreshToken ?? null
    localStorage.setItem(TOKEN_KEY, accessToken)
    if (newRefreshToken) {
      localStorage.setItem(REFRESH_TOKEN_KEY, newRefreshToken)
    }
  }

  /** 清除所有认证信息 */
  function clearAuth() {
    token.value = null
    refreshToken.value = null
    userInfo.value = null
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
  }

  /** 使用授权码换取 Token 并设置认证状态 */
  async function handleCallback(code: string) {
    const result = await exchangeCodeForToken(code)
    setAuth(result.accessToken, result.refreshToken)
  }

  /** 获取当前用户信息 */
  async function fetchUserInfo() {
    if (!token.value) return
    try {
      userInfo.value = await getUserInfo()
    } catch {
      userInfo.value = null
    }
  }

  /** 局部更新用户信息（如修改昵称、头像后） */
  function updateUserInfo(patch: Partial<UserInfoResult>) {
    if (userInfo.value) {
      userInfo.value = { ...userInfo.value, ...patch }
    }
  }

  /** 刷新 Access Token，使用单例模式防止并发 */
  async function refreshAccessToken(): Promise<string | null> {
    if (refreshingPromise) return refreshingPromise

    if (!refreshToken.value) {
      clearAuth()
      return null
    }

    refreshingPromise = refreshOAuthToken(refreshToken.value)
      .then((result) => {
        if (result) {
          setAuth(result.accessToken, result.refreshToken)
          return result.accessToken
        }
        clearAuth()
        return null
      })
      .catch(() => {
        clearAuth()
        return null
      })
      .finally(() => {
        refreshingPromise = null
      })

    return refreshingPromise
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
    isAuthenticated,
    isAdmin,
    setAuth,
    clearAuth,
    handleCallback,
    fetchUserInfo,
    updateUserInfo,
    refreshAccessToken,
    logout
  }
})
