/** 角色名常量，与后端 RoleConstants 保持一致 */
export const ROLE = {
  Admin: 'Admin',
  User: 'User',
} as const

/** 角色名对应的中文标签 */
export function roleLabel(role: string): string {
  switch (role) {
    case ROLE.Admin: return '管理员'
    case ROLE.User: return '用户'
    default: return role
  }
}

/** 角色名对应的 Tag 类型 */
export function roleTagType(role: string): '' | 'danger' | 'info' {
  return role === ROLE.Admin ? 'danger' : 'info'
}

/** 客户端状态对应的 Tag 类型 */
export function statusTagType(status: string): '' | 'success' | 'warning' | 'danger' | 'info' {
  switch (status) {
    case 'Draft': return 'info'
    case 'Pending': return 'warning'
    case 'Approved': return 'success'
    case 'Rejected': return 'danger'
    default: return 'info'
  }
}

/** 客户端状态的中文标签 */
export function statusLabel(status: string): string {
  switch (status) {
    case 'Draft': return '草稿'
    case 'Pending': return '待审核'
    case 'Approved': return '已通过'
    case 'Rejected': return '已拒绝'
    default: return status
  }
}

/** 格式化日期为本地字符串 */
export function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString('zh-CN')
}
