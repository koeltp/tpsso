<template>
  <div class="callback-content">
    <el-icon class="loading-icon" :size="40"><Loading /></el-icon>
    <p>正在登录...</p>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Loading } from '@element-plus/icons-vue'
import { getSavedRedirect } from '@/utils/oauth'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const userStore = useUserStore()

onMounted(async () => {
  const params = new URLSearchParams(window.location.search)
  const code = params.get('code')
  const error = params.get('error')

  if (error) {
    ElMessage.error(params.get('error_description') || '授权失败')
    router.replace('/login')
    return
  }

  if (!code) {
    ElMessage.error('缺少授权码')
    router.replace('/login')
    return
  }

  try {
    // 通过 Store 统一处理授权码交换和 Token 存储
    await userStore.handleCallback(code)
    await userStore.fetchUserInfo()

    if (!userStore.isAdmin) {
      router.replace('/forbidden')
      return
    }
    const redirect = getSavedRedirect() || '/'
    router.replace(redirect)
  } catch (e: any) {
    ElMessage.error(e.message || '登录失败，请重试')
    router.replace('/login')
  }
})
</script>

<style scoped>
.callback-content {
  text-align: center;
  color: #666;
}

.loading-icon {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>
