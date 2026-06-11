<template>
  <router-link to="/" class="home-icon" title="返回首页">
    <el-icon><HomeFilled /></el-icon>
  </router-link>
  <img :src="logoSrc" alt="TPSSO" class="login-logo" />
  <h1 class="title">TPSSO 登录</h1>
  <p class="subtitle">统一身份认证平台</p>
  <el-button type="primary" size="large" style="width: 100%" :loading="loading" @click="handleLogin">
    通过 SSO 登录
  </el-button>
  <div class="register-link">
    还没有账号？ <router-link to="/register">立即注册</router-link>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { HomeFilled } from '@element-plus/icons-vue'
import { startOAuthLogin } from '@/utils/oauth'
import logoSrc from '@/assets/logo-icon.png'

const loading = ref(false)

const handleLogin = async () => {
  loading.value = true
  await startOAuthLogin()
}
</script>

<style scoped>
.home-icon {
  position: absolute;
  top: 20px;
  left: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  color: #999;
  text-decoration: none;
  border-radius: 8px;
  transition: all 0.3s;
  z-index: 1;
}

.home-icon:hover {
  color: #409eff;
  background: #f0f5ff;
}

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

.register-link {
  text-align: center;
  margin-top: 20px;
  color: #666;
  font-size: 14px;
}

.register-link a {
  color: #409eff;
  text-decoration: none;
}

.register-link a:hover {
  text-decoration: underline;
}
</style>
