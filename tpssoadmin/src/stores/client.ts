import { defineStore } from 'pinia'
import { ref, readonly } from 'vue'
import { searchClients, type ClientResult, type SearchPager, type ClientSearchCondition, type PagerResponse } from '@/api/client'

export const useClientStore = defineStore('client', () => {
  const pendingCount = ref(0)
  const totalClients = ref(0)
  const approvedCount = ref(0)

  /** 搜索客户端（分页） */
  async function search(pager: SearchPager<ClientSearchCondition>): Promise<PagerResponse<ClientResult>> {
    const result = await searchClients(pager)
    return result
  }

  /** 获取待审核客户端数量（供侧边栏 Badge 使用） */
  async function fetchPendingCount() {
    try {
      const result = await searchClients({
        pageIndex: 1,
        pageSize: 1,
        condition: { status: 'Pending' }
      })
      pendingCount.value = result.totalCount
    } catch {
      // 拦截器已处理
    }
  }

  /** 获取客户端统计概览（供仪表盘使用） */
  async function fetchStats() {
    try {
      // 查第一页拿 totalCount 即可
      const allResult = await searchClients({ pageIndex: 1, pageSize: 1 })
      totalClients.value = allResult.totalCount

      const approvedResult = await searchClients({
        pageIndex: 1,
        pageSize: 1,
        condition: { status: 'Approved' }
      })
      approvedCount.value = approvedResult.totalCount
    } catch {
      // 拦截器已处理
    }
  }

  return {
    pendingCount: readonly(pendingCount),
    totalClients: readonly(totalClients),
    approvedCount: readonly(approvedCount),
    search,
    fetchPendingCount,
    fetchStats
  }
})
