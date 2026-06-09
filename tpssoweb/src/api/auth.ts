import api from '@/utils/api'

export interface UserInfoResult {
  username: string
  email: string
  avatarUrl: string
  nickName?: string
  roles: string[]
}

export interface LoginResult {
  token: string
  refreshToken: string
  expiresAt: string
  userInfo: UserInfoResult
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

export interface UpdateProfileModel {
  nickName?: string
  avatarUrl?: string
}

export interface ChangePasswordModel {
  currentPassword: string
  newPassword: string
}

/** 登录，返回 JWT Token 和用户信息 */
export const login = (data: LoginRequest): Promise<LoginResult> => {
  return api.post('/api/account/login', data)
}

/** 退出 */
export const logout = (): Promise<boolean> => {
  return api.post('/api/account/logout')
}

/** 使用 Refresh Token 刷新 Access Token */
export const refreshToken = (refreshToken: string): Promise<LoginResult> => {
  return api.post('/api/account/refresh', { refreshToken })
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

/** 修改个人信息 */
export const updateProfile = (data: UpdateProfileModel): Promise<boolean> => {
  return api.put('/api/account/profile', data)
}

/** 修改密码 */
export const changePassword = (data: ChangePasswordModel): Promise<boolean> => {
  return api.put('/api/account/password', data)
}
