import api from '@/utils/api'

/** 字典类型结果（树形） */
export interface DictTypeResult {
  id: string
  code: string
  name: string
  description?: string
  sort: number
  isEnabled: boolean
  parentId?: string
  children: DictTypeResult[]
  items: DictItemResult[]
}

/** 字典项结果 */
export interface DictItemResult {
  id: string
  typeId: string
  key: string
  value: string
  isSensitive: boolean
  description?: string
  sort: number
  isEnabled: boolean
}

/** 字典类型 DTO */
export interface DictTypeDto {
  id?: string
  code: string
  name: string
  description?: string
  sort: number
  isEnabled: boolean
  parentId?: string
}

/** 字典项 DTO */
export interface DictItemDto {
  id?: string
  key: string
  value: string
  description?: string
  isSensitive: boolean
  sort: number
  isEnabled: boolean
}

/** 获取所有字典配置 */
export const getAllDict = (): Promise<DictTypeResult[]> => {
  return api.get('/api/dict')
}

/** 创建或更新字典类型 */
export const saveDictType = (dto: DictTypeDto): Promise<DictTypeResult> => {
  return api.post('/api/dict/types', dto)
}

/** 删除字典类型 */
export const deleteDictType = (id: string): Promise<boolean> => {
  return api.delete(`/api/dict/types/${id}`)
}

/** 创建或更新字典项 */
export const saveDictItem = (typeId: string, dto: DictItemDto): Promise<DictItemResult> => {
  return api.post(`/api/dict/types/${typeId}/items`, dto)
}

/** 删除字典项 */
export const deleteDictItem = (id: string): Promise<boolean> => {
  return api.delete(`/api/dict/items/${id}`)
}
