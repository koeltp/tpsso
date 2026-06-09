import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'
import { useUserStore } from '@/stores/user'

/** 后端统一响应格式 */
interface ResponseResult<T = unknown> {
  code: number
  message?: string
  data?: T
}

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '',
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

// 响应拦截器：自动解包 ResponseResult，401 时尝试刷新 Token
api.interceptors.response.use(
  (response) => {
    const body = response.data

    // 非标准 ResponseResult 格式（如 OIDC 端点），直接返回
    if (!body || typeof body !== 'object' || typeof body.code !== 'number') {
      return body
    }

    const { code, message, data } = body as ResponseResult

    if (code !== 200) {
      ElMessage.error(message || '请求失败')
      return Promise.reject(new Error(message))
    }

    return data ?? body
  },
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      const userStore = useUserStore()

      if (!userStore.refreshToken) {
        userStore.clearAuth()
        redirectToLogin()
        return Promise.reject(error)
      }

      originalRequest._retry = true

      // Store 的 refreshAccessToken 已有单例锁，防止并发刷新
      const result = await userStore.refreshAccessToken()
      if (result?.token) {
        originalRequest.headers.Authorization = `Bearer ${result.token}`
        return api(originalRequest)
      }

      // 刷新失败，清除认证并跳转登录
      userStore.clearAuth()
      redirectToLogin()
      return Promise.reject(error)
    }

    if (!error.response) {
      ElMessage.error('网络连接失败，请检查网络')
    } else if (error.response?.status !== 401) {
      // 后端错误响应也遵循 { code, message, data } 格式
      const errorData = error.response.data as ResponseResult | undefined
      const message = errorData?.message || '网络错误'
      ElMessage.error(message)
    }

    return Promise.reject(error)
  }
)

function redirectToLogin() {
  if (router.currentRoute.value.path !== '/login') {
    const returnUrl = router.currentRoute.value.fullPath
    router.push({ path: '/login', query: { returnUrl } })
  }
}

export default api
export type { ResponseResult }
