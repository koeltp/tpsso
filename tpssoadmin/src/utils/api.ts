import axios from 'axios'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'

const api = axios.create({
  timeout: 30000
})

// 请求拦截器：从 store 读取 token
api.interceptors.request.use((config) => {
  const userStore = useUserStore()
  if (userStore.token) {
    config.headers.Authorization = `Bearer ${userStore.token}`
  }
  return config
})

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
      const userStore = useUserStore()

      if (!userStore.refreshToken) {
        // 没有 Refresh Token，直接退出
        userStore.logout()
        return Promise.reject(error)
      }

      originalRequest._retry = true

      // Store 的 refreshAccessToken 已有单例锁，防止并发刷新
      const newToken = await userStore.refreshAccessToken()
      if (newToken) {
        originalRequest.headers.Authorization = `Bearer ${newToken}`
        return api(originalRequest)
      }

      // 刷新失败，logout 内部会 clearAuth 并跳转
      userStore.logout()
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
