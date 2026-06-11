import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'

/** 后端统一响应格式 */
interface ResponseResult<T = unknown> {
  code: number
  message?: string
  data?: T
}

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '',
  timeout: 30000,
  // Cookie 认证：跨域请求携带凭证
  withCredentials: true
})

// 响应拦截器：自动解包 ResponseResult
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
  (error) => {
    if (error.response?.status === 401) {
      // Cookie 过期，跳转登录
      const returnUrl = router.currentRoute.value.fullPath
      if (router.currentRoute.value.path !== '/login') {
        router.push({ path: '/login', query: { returnUrl } })
      }
    } else if (!error.response) {
      ElMessage.error('网络连接失败，请检查网络')
    } else if (error.response?.status !== 401) {
      const errorData = error.response.data as ResponseResult | undefined
      const message = errorData?.message || '网络错误'
      ElMessage.error(message)
    }

    return Promise.reject(error)
  }
)

export default api
export type { ResponseResult }
