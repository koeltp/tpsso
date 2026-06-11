import api from '@/utils/api'

export interface ClientResult {
  id: string
  clientId: string
  clientSecret?: string
  name: string
  description?: string
  logo?: string
  redirectUris: string
  allowedScopes: string
  isPublic: boolean
  status: string
  reviewRemark?: string
  rowVersion?: string
  createdAt: string
  updatedAt?: string
}

export interface ClientCreatedResult extends ClientResult {
  /** 明文 Secret，仅创建时返回一次 */
  plainSecret?: string
}

export interface CreateClientRequest {
  name: string
  description?: string
  logo?: string
  redirectUris: string
  allowedScopes?: string
  isPublic?: boolean
}

export interface UpdateClientRequest {
  name: string
  description?: string
  logo?: string
  redirectUris: string
  allowedScopes?: string
  rowVersion?: string
}

export interface RejectClientRequest {
  reason: string
}

/** 搜索条件 */
export interface ClientSearchCondition {
  keyword?: string
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

/** 搜索客户端（管理员，分页） */
export const searchClients = (pager: SearchPager<ClientSearchCondition>): Promise<PagerResponse<ClientResult>> => {
  return api.post('/api/client/search', pager)
}

/** 搜索我的客户端（当前用户创建的，分页） */
export const searchMyClients = (pager: SearchPager<ClientSearchCondition>): Promise<PagerResponse<ClientResult>> => {
  return api.post('/api/client/my/search', pager)
}

/** 获取客户端的授权用户列表 */
export const getClientAuthorizedUsers = (clientId: string, pager: { pageIndex: number; pageSize: number; keyword?: string }): Promise<PagerResponse<AuthorizedUserResult>> => {
  return api.post(`/api/client/${clientId}/authorized-users`, pager)
}

/** 客户端详情 */
export const getClientById = (id: string): Promise<ClientResult> => {
  return api.get(`/api/client/${id}`)
}

/** 创建客户端 */
export const createClient = (data: CreateClientRequest): Promise<ClientCreatedResult> => {
  return api.post('/api/client', data)
}

/** 更新客户端 */
export const updateClient = (id: string, data: UpdateClientRequest): Promise<boolean> => {
  return api.put(`/api/client/${id}`, data)
}

/** 提交审核 */
export const submitClient = (id: string): Promise<boolean> => {
  return api.post(`/api/client/${id}/submit`)
}

/** 撤回审核 */
export const withdrawClient = (id: string): Promise<boolean> => {
  return api.post(`/api/client/${id}/withdraw`)
}

/** 审核通过 */
export const approveClient = (id: string): Promise<boolean> => {
  return api.post(`/api/client/${id}/approve`)
}

/** 审核拒绝 */
export const rejectClient = (id: string, data: RejectClientRequest): Promise<boolean> => {
  return api.post(`/api/client/${id}/reject`, data)
}

/** 删除客户端 */
export const deleteClient = (id: string): Promise<boolean> => {
  return api.delete(`/api/client/${id}`)
}

/** 重置客户端密钥（仅机密类型），返回新的明文 Secret */
export const regenerateClientSecret = (id: string): Promise<ClientCreatedResult> => {
  return api.post(`/api/client/${id}/regenerate-secret`)
}

/** 授权用户信息 */
export interface AuthorizedUserResult {
  username: string
  nickName?: string
  authorizedAt: string
}

/** 我的授权记录 */
export interface AuthorizationResult {
  id: string
  clientName: string
  scopes: string[]
  createdAt: string
}

/** 获取我的授权列表 */
export const getMyAuthorizations = (): Promise<AuthorizationResult[]> => {
  return api.get('/api/authorization/my')
}

/** 撤销授权 */
export const revokeAuthorization = (id: string): Promise<boolean> => {
  return api.delete(`/api/authorization/${id}`)
}
