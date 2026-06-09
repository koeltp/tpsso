import { defineStore } from 'pinia'
import { ref, computed, readonly } from 'vue'
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

  /** 局部更新用户信息（如修改昵称、头像后） */
  function updateUserInfo(patch: Partial<UserInfoResult>) {
    if (userInfo.value) {
      userInfo.value = { ...userInfo.value, ...patch }
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
    setAuth,
    clearAuth,
    fetchUserInfo,
    updateUserInfo,
    refreshAccessToken,
    logout
  }
})
