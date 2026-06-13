import axios from 'axios'
import { ElMessage, ElNotification } from 'element-plus'
import { useUserStore } from '@/stores/user'

// 注入通知进度条动画样式
if (!document.getElementById('notify-progress-style')) {
  const style = document.createElement('style')
  style.id = 'notify-progress-style'
  style.textContent = `@keyframes notify-shrink{from{width:100%}to{width:0%}}`
  document.head.appendChild(style)
}

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
  const duration = 10000
  const notifyId = `err_${Date.now()}`
  ElNotification({
    key: notifyId,
    title: '操作失败',
    dangerouslyUseHTMLString: true,
    message: `<div>${message}</div><div style="margin-top:8px;font-size:12px;color:#999;display:flex;align-items:center;gap:8px">追踪ID: ${correlationId}<a href="javascript:void(0)" style="color:#409eff;cursor:pointer" onclick="window.__copyTraceId__('${correlationId}')">复制</a></div><div style="margin-top:10px;height:3px;background:#f5f5f5;border-radius:2px;overflow:hidden"><div class="notify-progress" style="height:100%;background:linear-gradient(90deg,#f56c6c,#f89898);border-radius:2px;animation:notify-shrink ${duration}ms linear forwards"></div></div>`,
    type: 'error',
    duration,
    customClass: 'notify-with-progress'
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

    if (error.response?.status === 401 && !originalRequest._retry) {
      const userStore = useUserStore()

      if (!userStore.refreshToken) {
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

      // 刷新失败，logout 内部会 clearAuth 并跳转登录页
      userStore.logout()
      return Promise.reject(error)
    }

    if (!error.response) {
      ElMessage.error('网络连接失败，请检查网络')
    } else if (error.response?.status !== 401) {
      const errorData = error.response.data as ResponseResult | undefined
      const message = errorData?.message || '网络错误'
      showErrorWithTrace(message, errorData?.correlationId)
    }

    return Promise.reject(error)
  }
)

export default api
export type { ResponseResult }
