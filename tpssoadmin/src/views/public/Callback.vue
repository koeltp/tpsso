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
    await userStore.handleCallback(code)
    await userStore.fetchUserInfo()

    // 始终根据角色跳转，不使用保存的 redirect（避免角色切换后跳转错误）
    if (userStore.isAdmin) {
      router.replace('/admin/dashboard')
    } else {
      router.replace('/dashboard')
    }
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
