import { defineStore } from 'pinia'
import { ref, computed, readonly } from 'vue'
import { getUserInfo, logout as logoutApi, type UserInfoResult } from '@/api/auth'

export const useUserStore = defineStore('user', () => {
  const userInfo = ref<UserInfoResult | null>(null)

  const isAuthenticated = computed(() => !!userInfo.value)

  /** 获取当前用户信息 */
  async function fetchUserInfo() {
    try {
      userInfo.value = await getUserInfo()
    } catch {
      userInfo.value = null
    }
  }

  /** 设置用户信息（登录成功后调用） */
  function setUserInfo(info: UserInfoResult) {
    userInfo.value = info
  }

  /** 退出登录 */
  async function logout() {
    try {
      await logoutApi()
    } catch {
      // 拦截器已处理
    }
    userInfo.value = null
  }

  return {
    userInfo: readonly(userInfo),
    isAuthenticated,
    fetchUserInfo,
    setUserInfo,
    logout
  }
})
