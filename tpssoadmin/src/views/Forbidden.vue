<template>
  <div class="forbidden-container">
    <el-icon :size="64" color="#ff4d4f"><CircleCloseFilled /></el-icon>
    <h2>无访问权限</h2>
    <p>您没有管理员权限，无法访问管理后台</p>
    <el-button type="primary" @click="handleLogout">退出登录</el-button>
  </div>
</template>

<script setup lang="ts">
import { CircleCloseFilled } from '@element-plus/icons-vue'
import { logoutOAuth } from '@/utils/oauth'

const handleLogout = () => {
  logoutOAuth()
}
</script>

<style scoped>
.forbidden-container {
  width: 100%;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 16px;
  background: #f0f2f5;
}

h2 {
  font-size: 20px;
  font-weight: 600;
  color: #333;
  margin: 0;
}

p {
  font-size: 14px;
  color: #999;
  margin: 0;
}
</style>
