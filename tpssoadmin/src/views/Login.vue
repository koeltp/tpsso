<template>
  <div class="login-container">
    <div class="login-box">
      <img :src="logoSrc" alt="TPSSO" class="login-logo" />
      <h1 class="title">TPSSO 管理后台</h1>
      <p class="subtitle">需要管理员权限</p>
      <el-button type="primary" size="large" style="width: 100%" :loading="loading" @click="handleLogin">
        通过 SSO 登录
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { startOAuthLogin } from '@/utils/oauth'
import logoSrc from '@/assets/logo-icon.png'

const loading = ref(false)

const handleLogin = async () => {
  loading.value = true
  await startOAuthLogin()
  // 页面会跳转，不需要后续处理
}
</script>

<style scoped>
.login-container {
  width: 100%;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f0f2f5;
}

.login-box {
  width: 400px;
  padding: 48px 40px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  text-align: center;
}

.login-logo {
  width: 56px;
  height: 56px;
  margin-bottom: 20px;
}

.title {
  font-size: 22px;
  font-weight: 600;
  color: #1a1a2e;
  margin-bottom: 8px;
}

.subtitle {
  font-size: 14px;
  color: #999;
  margin-bottom: 32px;
}
</style>
