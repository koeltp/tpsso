import { defineStore } from 'pinia'
import { ref, readonly } from 'vue'
import { searchUsers, type UserResult, type SearchPager, type UserSearchCondition, type PagerResponse } from '@/api/user'

export const useUserManageStore = defineStore('userManage', () => {
  const totalUsers = ref(0)

  /** 搜索用户（分页） */
  async function search(pager: SearchPager<UserSearchCondition>): Promise<PagerResponse<UserResult>> {
    return await searchUsers(pager)
  }

  /** 获取用户总数（供仪表盘使用） */
  async function fetchTotalCount() {
    try {
      const result = await searchUsers({ pageIndex: 1, pageSize: 1 })
      totalUsers.value = result.totalCount
    } catch {
      // 拦截器已处理
    }
  }

  return {
    totalUsers: readonly(totalUsers),
    search,
    fetchTotalCount
  }
})
