import api from '@/utils/api'

export interface UserInfoResult {
  username: string
  email: string
  avatarUrl: string
  nickName?: string
  roles: string[]
}

export interface LoginRequest {
  username: string
  password: string
  rememberMe?: boolean
}

/** Cookie 登录 */
export const login = (data: LoginRequest): Promise<UserInfoResult> => {
  return api.post('/api/account/login', data)
}

/** 登出 */
export const logout = (): Promise<boolean> => {
  return api.post('/api/account/logout')
}

/** 获取当前用户信息 */
export const getUserInfo = (): Promise<UserInfoResult> => {
  return api.get('/api/account/me')
}
