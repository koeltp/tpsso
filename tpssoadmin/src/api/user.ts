import api from '@/utils/api'

/** 角色列表项 */
export interface RoleResult {
  name: string
  description?: string
}

/** 用户列表项 */
export interface UserResult {
  id: string
  username: string
  email: string
  nickName?: string
  avatarUrl?: string
  roles: string[]
  isLockedOut: boolean
  createdAt: string
}

/** 用户搜索条件 */
export interface UserSearchCondition {
  keyword?: string
  role?: string
  status?: string
}

/** 分页请求参数 */
export interface SearchPager<T> {
  pageIndex: number
  pageSize: number
  condition?: T
}

/** 分页响应数据 */
export interface PagerResponse<T> {
  items: T[]
  totalCount: number
  pageIndex: number
  pageSize: number
  pageCount: number
}

/** 获取所有角色列表 */
export const getRoles = (): Promise<RoleResult[]> => {
  return api.get('/api/user/roles')
}

/** 搜索用户（管理员，分页） */
export const searchUsers = (pager: SearchPager<UserSearchCondition>): Promise<PagerResponse<UserResult>> => {
  return api.post('/api/user/search', pager)
}

/** 用户详情 */
export const getUserById = (id: string): Promise<UserResult> => {
  return api.get(`/api/user/${id}`)
}

/** 修改用户角色 */
export const updateUserRoles = (id: string, roles: string[]): Promise<boolean> => {
  return api.put(`/api/user/${id}/roles`, { roles })
}

/** 禁用用户 */
export const lockUser = (id: string): Promise<boolean> => {
  return api.post(`/api/user/${id}/lock`)
}

/** 启用用户 */
export const unlockUser = (id: string): Promise<boolean> => {
  return api.post(`/api/user/${id}/unlock`)
}

/** 重置用户密码 */
export const resetUserPassword = (id: string, newPassword: string): Promise<boolean> => {
  return api.post(`/api/user/${id}/reset-password`, { newPassword })
}
