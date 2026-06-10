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
  createdAt: string
  updatedAt?: string
  rowVersion?: string
}

export interface ClientCreatedResult extends ClientResult {
  plainSecret?: string
}

export interface CreateClientRequest {
  name: string
  description?: string
  logo?: string
  redirectUris: string
  allowedScopes?: string
  isPublic: boolean
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

/** 创建客户端 */
export const createClient = (data: CreateClientRequest): Promise<ClientCreatedResult> => {
  return api.post('/api/client', data)
}

/** 搜索我的客户端（分页） */
export const searchMyClients = (pager: SearchPager<ClientSearchCondition>): Promise<PagerResponse<ClientResult>> => {
  return api.post('/api/client/my/search', pager)
}

/** 客户端详情 */
export const getClientById = (id: string): Promise<ClientResult> => {
  return api.get(`/api/client/${id}`)
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

/** 删除客户端 */
export const deleteClient = (id: string): Promise<boolean> => {
  return api.delete(`/api/client/${id}`)
}
