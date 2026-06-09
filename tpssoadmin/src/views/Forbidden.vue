<template>
  <div class="forbidden-content">
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
</style>
