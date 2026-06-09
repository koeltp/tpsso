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

export interface RejectClientRequest {
  reason: string
}

/** 所有客户端列表 */
export const getAllClients = (): Promise<ClientResult[]> => {
  return api.get('/api/client')
}

/** 待审核列表 */
export const getPendingClients = (): Promise<ClientResult[]> => {
  return api.get('/api/client/pending')
}

/** 客户端详情 */
export const getClientById = (id: string): Promise<ClientResult> => {
  return api.get(`/api/client/${id}`)
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
