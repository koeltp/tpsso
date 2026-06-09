import api from '@/utils/api'

export interface UserInfoResult {
  username: string
  email: string
  avatarUrl: string
  nickName?: string
  roles: string[]
}

export interface UpdateProfileModel {
  nickName?: string
  avatarUrl?: string
}

export interface ChangePasswordModel {
  currentPassword: string
  newPassword: string
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
