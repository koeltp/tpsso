import axios from 'axios'
import { ElMessage } from 'element-plus'
import { getAccessToken, refreshAccessToken, logoutOAuth } from '@/utils/oauth'

const api = axios.create({
  timeout: 30000
})

// 请求拦截器：自动附加 Bearer token
api.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// 防止并发刷新的锁
let isRefreshing = false
let pendingRequests: Array<(token: string) => void> = []

function onTokenRefreshed(newToken: string) {
  pendingRequests.forEach((cb) => cb(newToken))
  pendingRequests = []
}

// 响应拦截器：自动解包 ResponseResult，401 时尝试刷新 Token
api.interceptors.response.use(
  (response) => {
    const body = response.data

    // 非标准 ResponseResult 格式，直接返回
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
        const newToken = await refreshAccessToken()
        if (newToken) {
          onTokenRefreshed(newToken)
          originalRequest.headers.Authorization = `Bearer ${newToken}`
          return api(originalRequest)
        }
      } catch {
        // 刷新失败
      } finally {
        isRefreshing = false
      }

      // 刷新失败，退出登录
      logoutOAuth()
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

export default api
