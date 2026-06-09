import axios from 'axios'
import { ElMessage } from 'element-plus'
import { getAccessToken, logoutOAuth } from '@/utils/oauth'

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

// 响应拦截器：自动解包 ResponseResult，统一弹窗提示错误
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
  (error) => {
    // 401 未登录，清除 token 并跳转 SSO 登录
    if (error.response?.status === 401) {
      logoutOAuth()
      return Promise.reject(error)
    }
    if (!error.response) {
      ElMessage.error('网络连接失败，请检查网络')
    } else {
      ElMessage.error(error.response?.data?.message || '网络错误')
    }
    return Promise.reject(error)
  }
)

export default api
