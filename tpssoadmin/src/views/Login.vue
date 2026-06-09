<template>
  <img :src="logoSrc" alt="TPSSO" class="login-logo" />
  <h1 class="title">TPSSO 管理后台</h1>
  <p class="subtitle">需要管理员权限</p>
  <el-button type="primary" size="large" style="width: 100%" :loading="loading" @click="handleLogin">
    通过 SSO 登录
  </el-button>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { startOAuthLogin } from '@/utils/oauth'
import logoSrc from '@/assets/logo-icon.png'

const loading = ref(false)

const handleLogin = async () => {
  loading.value = true
  await startOAuthLogin()
}
</script>

<style scoped>
.login-logo {
  display: block;
  width: 56px;
  height: 56px;
  margin: 0 auto 20px;
}

.title {
  text-align: center;
  font-size: 22px;
  font-weight: 600;
  color: #1a1a2e;
  margin-bottom: 8px;
}

.subtitle {
  text-align: center;
  font-size: 14px;
  color: #999;
  margin-bottom: 32px;
}
</style>
