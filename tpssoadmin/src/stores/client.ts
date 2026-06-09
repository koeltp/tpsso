import { defineStore } from 'pinia'
import { ref, readonly } from 'vue'
import { getPendingClients, getAllClients, type ClientResult } from '@/api/client'

export const useClientStore = defineStore('client', () => {
  const pendingCount = ref(0)
  const totalClients = ref(0)
  const approvedCount = ref(0)

  /** 获取待审核客户端数量（供侧边栏 Badge 和仪表盘共享） */
  async function fetchPendingCount() {
    try {
      const pending = await getPendingClients()
      pendingCount.value = pending.length
    } catch {
      // 拦截器已处理
    }
  }

  /** 获取客户端统计概览（供仪表盘使用） */
  async function fetchStats() {
    try {
      const all = await getAllClients()
      totalClients.value = all.length
      approvedCount.value = all.filter(c => c.status === 'Approved').length
    } catch {
      // 拦截器已处理
    }
  }

  return {
    pendingCount: readonly(pendingCount),
    totalClients: readonly(totalClients),
    approvedCount: readonly(approvedCount),
    fetchPendingCount,
    fetchStats
  }
})
