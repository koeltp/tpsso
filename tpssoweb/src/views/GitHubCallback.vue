<template>
  <div class="callback-page">
    <div class="callback-card">
      <el-icon class="loading-icon" :size="48" color="#409eff"><Loading /></el-icon>
      <p>正在完成 GitHub 登录...</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Loading } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

onMounted(async () => {
  const token = route.query.token as string
  const refreshToken = route.query.refreshToken as string
  const error = route.query.error as string

  if (error) {
    ElMessage.error('GitHub 登录失败，请重试')
    router.replace('/login')
    return
  }

  if (!token || !refreshToken) {
    ElMessage.error('登录凭证缺失，请重试')
    router.replace('/login')
    return
  }

  // 存储 token
  userStore.setAuth({ token, refreshToken, expiresAt: '', userInfo: null as any })

  // 获取用户信息
  try {
    await userStore.fetchUserInfo()
    router.replace('/')
  } catch {
    ElMessage.error('获取用户信息失败')
    router.replace('/login')
  }
})
</script>

<style scoped>
.callback-page {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: #f5f7fa;
}

.callback-card {
  text-align: center;
  padding: 40px;
}

.loading-icon {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.callback-card p {
  margin-top: 16px;
  color: #666;
  font-size: 16px;
}
</style>
