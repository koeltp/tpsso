import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getAllDict, type DictTypeResult } from '@/api/dict'

export const useDictStore = defineStore('dict', () => {
  const dictTypes = ref<DictTypeResult[]>([])
  const loading = ref(false)

  /** 加载所有字典配置 */
  const fetchAll = async () => {
    loading.value = true
    try {
      dictTypes.value = await getAllDict()
    } catch {
      // 拦截器已处理
    } finally {
      loading.value = false
    }
  }

  return {
    dictTypes,
    loading,
    fetchAll,
  }
})
