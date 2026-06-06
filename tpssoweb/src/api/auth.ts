import api from '@/utils/api'

export interface UserInfoResult {
  username: string
  email: string
  avatarUrl: string
}

export interface LoginRequest {
  username: string
  password: string
}

export interface RegisterRequest {
  username: string
  email: string
  code: string
  password: string
}

export interface SendCodeRequest {
  email: string
  purpose: number
}

/** 登录 */
export const login = (data: LoginRequest): Promise<boolean> => {
  return api.post('/api/account/login', data)
}

/** 退出 */
export const logout = (): Promise<boolean> => {
  return api.post('/api/account/logout')
}

/** 获取当前用户信息 */
export const getUserInfo = (): Promise<UserInfoResult> => {
  return api.get('/api/account/me')
}

/** 发送邮箱验证码 */
export const sendCode = (data: SendCodeRequest): Promise<boolean> => {
  return api.post('/api/account/send-code', data)
}

/** 用户注册 */
export const register = (data: RegisterRequest): Promise<boolean> => {
  return api.post('/api/account/register', data)
}
