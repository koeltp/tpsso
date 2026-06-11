<template>
  <div class="forbidden-content">
    <el-icon :size="64" color="#fa8c16"><WarningFilled /></el-icon>
    <h2>无管理权限</h2>
    <p>您没有管理员权限，无法访问此页面</p>
    <div class="action-buttons">
      <el-button type="primary" @click="router.push('/profile')">前往个人中心</el-button>
      <el-button @click="handleLogout">退出登录</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { WarningFilled } from '@element-plus/icons-vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const userStore = useUserStore()

const handleLogout = () => {
  userStore.logout()
}
</script>

<style scoped>
.forbidden-content {
  text-align: center;
}

h2 {
  font-size: 20px;
  font-weight: 600;
  color: #333;
  margin: 16px 0 8px;
}

p {
  font-size: 14px;
  color: #999;
  margin: 0 0 24px;
}

.action-buttons {
  display: flex;
  justify-content: center;
  gap: 12px;
}
</style>
