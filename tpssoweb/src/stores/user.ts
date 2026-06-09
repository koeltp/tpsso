import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { getUserInfo, refreshToken as refreshTokenApi, logout as logoutApi, type LoginResult, type UserInfoResult } from '@/api/auth'

const TOKEN_KEY = 'token'
const REFRESH_TOKEN_KEY = 'refreshToken'

export const useUserStore = defineStore('user', () => {
  const token = ref<string | null>(localStorage.getItem(TOKEN_KEY))
  const refreshToken = ref<string | null>(localStorage.getItem(REFRESH_TOKEN_KEY))
  const userInfo = ref<UserInfoResult | null>(null)

  // 防止并发刷新
  let refreshingPromise: Promise<LoginResult | null> | null = null

  const isAuthenticated = computed(() => !!token.value)

  /** 设置登录凭证并持久化 */
  function setAuth(result: LoginResult) {
    token.value = result.token
    refreshToken.value = result.refreshToken
    localStorage.setItem(TOKEN_KEY, result.token)
    localStorage.setItem(REFRESH_TOKEN_KEY, result.refreshToken)
  }

  /** 清除所有认证信息 */
  function clearAuth() {
    token.value = null
    refreshToken.value = null
    userInfo.value = null
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
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

  /** 刷新 Access Token，使用单例模式防止并发 */
  async function refreshAccessToken(): Promise<LoginResult | null> {
    if (refreshingPromise) return refreshingPromise

    if (!refreshToken.value) {
      clearAuth()
      return null
    }

    refreshingPromise = refreshTokenApi(refreshToken.value)
      .then((result) => {
        setAuth(result)
        return result
      })
      .catch(() => {
        // Refresh Token 也过期了，彻底退出
        clearAuth()
        return null
      })
      .finally(() => {
        refreshingPromise = null
      })

    return refreshingPromise
  }

  /** 退出登录 */
  async function logout() {
    try {
      await logoutApi()
    } catch {
      // 拦截器已处理
    }
    clearAuth()
  }

  return { token, refreshToken, userInfo, isAuthenticated, setAuth, clearAuth, fetchUserInfo, refreshAccessToken, logout }
})
