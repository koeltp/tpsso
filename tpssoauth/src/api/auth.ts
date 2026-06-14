import api from '@/utils/api'

export interface UserInfoResult {
  username: string
  email: string
  avatarUrl: string
  nickName?: string
  roles: string[]
  twoFactorEnabled: boolean
}

export interface LoginResult {
  userInfo?: UserInfoResult
  requiresTwoFactor: boolean
  userId?: string
}

export interface LoginRequest {
  username: string
  password: string
  rememberMe?: boolean
}

export interface LoginTwoFactorRequest {
  userId: string
  code: string
  isRecoveryCode?: boolean
  rememberMe?: boolean
}

export interface TwoFactorSetupResult {
  sharedKey: string
  authenticatorUri: string
  recoveryCodes: string[]
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
export const login = (data: LoginRequest): Promise<LoginResult> => {
  return api.post('/api/account/login', data)
}

/** 两步验证登录 */
export const loginTwoFactor = (data: LoginTwoFactorRequest): Promise<UserInfoResult> => {
  return api.post('/api/account/login-2fa', data)
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

// ──────── 两步验证 ────────

/** 生成两步验证密钥和二维码 */
export const setupTwoFactor = (): Promise<TwoFactorSetupResult> => {
  return api.post('/api/account/2fa/setup')
}

/** 启用两步验证 */
export const enableTwoFactor = (code: string): Promise<TwoFactorSetupResult> => {
  return api.post('/api/account/2fa/enable', { code })
}

/** 禁用两步验证 */
export const disableTwoFactor = (): Promise<boolean> => {
  return api.post('/api/account/2fa/disable')
}

/** 重新生成恢复码 */
export const resetRecoveryCodes = (): Promise<string[]> => {
  return api.post('/api/account/2fa/reset-codes')
}

// ──────── 第三方登录 ────────

export interface ExternalProvider {
  scheme: string    // GitHub / Google / WeChat
  displayName: string  // GitHub / Google / 微信
}

/** 获取已启用的第三方登录 Provider 列表 */
export const getExternalProviders = (): Promise<ExternalProvider[]> => {
  return api.get('/api/external-login/providers')
}

/**
 * 发起第三方登录
 * 后端会 302 重定向到第三方授权页，所以需要整页跳转
 */
export const externalLogin = (provider: string, returnUrl?: string) => {
  const params = new URLSearchParams()
  if (returnUrl) {
    params.set('returnUrl', returnUrl)
  }
  const query = params.toString()
  // 整页跳转到后端 Challenge 端点
  window.location.href = `/api/external-login/${provider}${query ? '?' + query : ''}`
}
