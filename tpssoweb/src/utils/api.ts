import axios from 'axios'
import { ElMessage } from 'element-plus'

const api = axios.create({
  timeout: 30000
})

// 响应拦截器：自动解包 ResponseResult，统一弹窗提示错误
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
  (error) => {
    if (!error.response) {
      ElMessage.error('网络连接失败，请检查网络')
    } else {
      ElMessage.error(error.response?.data?.message || '网络错误')
    }
    return Promise.reject(error)
  }
)

export default api
