import axios from 'axios'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'

const api = axios.create({
  timeout: 30000
})

// 请求拦截器：从 store 读取 JWT Token
api.interceptors.request.use((config) => {
  const userStore = useUserStore()
  if (userStore.token) {
    config.headers.Authorization = `Bearer ${userStore.token}`
  }
  return config
})

// 防止并发刷新的锁
let isRefreshing = false
let pendingRequests: Array<(token: string) => void> = []

/** 通知所有等待中的请求继续 */
function onTokenRefreshed(newToken: string) {
  pendingRequests.forEach((cb) => cb(newToken))
  pendingRequests = []
}

// 响应拦截器：自动解包 ResponseResult，401 时尝试刷新 Token
api.interceptors.response.use(
  (response) => {
    const body = response.data

    // 非标准 ResponseResult 格式（如 OIDC 端点），直接返回
    if (!body || typeof body !== 'object' || typeof body.code !== 'number') {
      return body
    }

    const { code, message } = body as { code: number; message?: string; data?: unknown }

    if (code !== 200) {
      ElMessage.error(message || '请求失败')
      return Promise.reject(new Error(message))
    }

    return (body as any).data ?? body
  },
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      const userStore = useUserStore()

      if (!userStore.refreshToken) {
        // 没有 Refresh Token，直接跳登录
        userStore.clearAuth()
        redirectToLogin()
        return Promise.reject(error)
      }

      if (isRefreshing) {
        // 正在刷新，将请求排队等待
        return new Promise((resolve) => {
          pendingRequests.push((newToken: string) => {
            originalRequest.headers.Authorization = `Bearer ${newToken}`
            resolve(api(originalRequest))
          })
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        const result = await userStore.refreshAccessToken()
        if (result?.token) {
          onTokenRefreshed(result.token)

          originalRequest.headers.Authorization = `Bearer ${result.token}`
          return api(originalRequest)
        }
      } catch {
        // 刷新失败
      } finally {
        isRefreshing = false
      }

      // 刷新失败，彻底退出
      userStore.clearAuth()
      redirectToLogin()
      return Promise.reject(error)
    }

    if (!error.response) {
      ElMessage.error('网络连接失败，请检查网络')
    } else if (error.response?.status !== 401) {
      ElMessage.error(error.response?.data?.message || '网络错误')
    }

    return Promise.reject(error)
  }
)

function redirectToLogin() {
  if (window.location.pathname !== '/login') {
    const returnUrl = encodeURIComponent(window.location.pathname + window.location.search)
    window.location.href = `/login?returnUrl=${returnUrl}`
  }
}

export default api
