import api from '@/utils/api'

export interface UserInfoResult {
  username: string
  email: string
  avatarUrl: string
  nickName?: string
  roles: string[]
  twoFactorEnabled: boolean
}

export interface UpdateProfileModel {
  nickName?: string
  avatarUrl?: string
}

export interface ChangePasswordModel {
  currentPassword: string
  newPassword: string
}

export interface TwoFactorSetupResult {
  sharedKey: string
  authenticatorUri: string
  recoveryCodes: string[]
}

/** 获取当前用户信息 */
export const getUserInfo = (): Promise<UserInfoResult> => {
  return api.get('/api/account/me')
}

/** 修改个人信息 */
export const updateProfile = (data: UpdateProfileModel): Promise<boolean> => {
  return api.put('/api/account/profile', data)
}

/** 修改密码 */
export const changePassword = (data: ChangePasswordModel): Promise<boolean> => {
  return api.put('/api/account/password', data)
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
