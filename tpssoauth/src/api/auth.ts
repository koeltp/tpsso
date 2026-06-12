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

export interface SendCodeRequest {
  email: string
}

export interface RegisterRequest {
  email: string
  code: string
  password: string
  confirmPassword: string
}

export interface ResetPasswordRequest {
  email: string
  code: string
  newPassword: string
  confirmNewPassword: string
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

/** 发送注册验证码 */
export const sendCode = (data: SendCodeRequest): Promise<boolean> => {
  return api.post('/api/account/send-code', data)
}

/** 注册 */
export const register = (data: RegisterRequest): Promise<boolean> => {
  return api.post('/api/account/register', data)
}

/** 发送重置密码验证码 */
export const sendResetCode = (data: SendCodeRequest): Promise<boolean> => {
  return api.post('/api/account/send-reset-code', data)
}

/** 重置密码 */
export const resetPassword = (data: ResetPasswordRequest): Promise<boolean> => {
  return api.post('/api/account/reset-password', data)
}
