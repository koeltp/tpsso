import axios from 'axios'
import { ElMessage, ElNotification } from 'element-plus'
import { useUserStore } from '@/stores/user'
import '@/styles/notify.css'

/** 后端统一响应格式 */
interface ResponseResult<T = unknown> {
  code: number
  message?: string
  data?: T
  correlationId?: string
}

/** 复制追踪ID到剪贴板 */
function copyCorrelationId(id: string) {
  navigator.clipboard.writeText(id).then(() => {
    ElMessage.success('追踪ID已复制')
  }).catch(() => {
    ElMessage.error('复制失败，请手动复制')
  })
}

/** 显示带追踪ID的错误通知 */
function showErrorWithTrace(message: string, correlationId?: string) {
  if (!correlationId) {
    ElMessage.error(message)
    return
  }
  ElNotification({
    title: '操作失败',
    dangerouslyUseHTMLString: true,
    message: `<div>${message}</div><div class="notify-trace">追踪ID: ${correlationId}<a href="javascript:void(0)" class="notify-trace__copy" onclick="window.__copyTraceId__('${correlationId}')">复制</a></div><div class="notify-progress-bar"><div class="notify-progress-bar__inner"></div></div>`,
    type: 'error',
    duration: 10000
  })
}

// 暴露复制函数给内联 onclick 调用
;(window as unknown as Record<string, unknown>).__copyTraceId__ = copyCorrelationId

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '',
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

    const { code, message, data, correlationId } = body as ResponseResult

    // code=0 表示成功，非0表示业务错误码
    if (code !== 0) {
      showErrorWithTrace(message || '请求失败', correlationId)
      return Promise.reject(new Error(message))
    }

    return data ?? body
  },
  async (error) => {
    const originalRequest = error.config
    const response = error.response
    const errorData = response?.data as ResponseResult | undefined
    const errorMessage = errorData?.message || '请求失败'
    const correlationId = errorData?.correlationId

    // 401 且未重试过：尝试刷新 token
    if (response?.status === 401 && !originalRequest._retry) {
      const userStore = useUserStore()

      // 没有 refreshToken 时直接登出
      if (!userStore.refreshToken) {
        showErrorWithTrace(errorMessage || '未授权，请重新登录', correlationId)
        userStore.logout()
        return Promise.reject(error)
      }

      originalRequest._retry = true
      const newToken = await userStore.refreshAccessToken()

      if (newToken) {
        originalRequest.headers.Authorization = `Bearer ${newToken}`
        return api(originalRequest) // 重试原始请求
      }

      // 刷新失败：显示错误并登出
      showErrorWithTrace(errorMessage || '登录已过期，请重新登录', correlationId)
      userStore.logout()
      return Promise.reject(error)
    }

    // 其他所有错误（包括重试后仍然 401、非 401 错误、无响应等）
    if (!response) {
      ElMessage.error('网络连接失败，请检查网络')
    } else {
      showErrorWithTrace(errorMessage, correlationId)
    }

    return Promise.reject(error)
  }
)

export default api
export type { ResponseResult }