import axios from 'axios'
import { ElMessage, ElNotification } from 'element-plus'
import router from '@/router'
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

    const { code, message, data, correlationId } = body as ResponseResult

    // code=0 表示成功，非0表示业务错误码
    if (code !== 0) {
      showErrorWithTrace(message || '请求失败', correlationId)
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
      showErrorWithTrace(message, errorData?.correlationId)
    }

    return Promise.reject(error)
  }
)

export default api
export type { ResponseResult }
