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
}

export interface RejectClientRequest {
  reason: string
}

/** 创建客户端 */
export const createClient = (data: CreateClientRequest): Promise<ClientCreatedResult> => {
  return api.post('/api/client', data)
}

/** 我创建的客户端 */
export const getMyClients = (): Promise<ClientResult[]> => {
  return api.get('/api/client/my')
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

/** 待审核列表（管理员） */
export const getPendingClients = (): Promise<ClientResult[]> => {
  return api.get('/api/client/pending')
}

/** 审核通过（管理员） */
export const approveClient = (id: string): Promise<boolean> => {
  return api.post(`/api/client/${id}/approve`)
}

/** 审核拒绝（管理员） */
export const rejectClient = (id: string, data: RejectClientRequest): Promise<boolean> => {
  return api.post(`/api/client/${id}/reject`, data)
}
