/**
 * 统一日期格式化工具
 * 后端返回 ISO 8601 格式的 UTC 时间（如 2026-06-15T08:40:32Z）
 * 前端统一转为本地时区显示
 */

/** 格式化日期为 YYYY-MM-DD HH:mm:ss */
export function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  if (isNaN(d.getTime())) return ''
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}
