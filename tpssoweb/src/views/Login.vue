<template>
  <div class="login-container">
    <div class="bubble bubble-1"></div>
    <div class="bubble bubble-2"></div>
    <div class="bubble bubble-3"></div>
    <div class="bubble bubble-4"></div>
    <div class="bubble bubble-5"></div>

    <div class="login-box">
      <div class="logo-area">
        <img :src="logoSrc" alt="TPSSO" class="logo-img" />
      </div>
      <h1 class="title">TPSSO 登录</h1>

      <el-form ref="formRef" :model="form" :rules="rules" label-position="top" @submit.prevent="handleSubmit">
        <el-form-item prop="username">
          <el-input v-model="form.username" placeholder="用户名" size="large" :prefix-icon="User" />
        </el-form-item>
        <el-form-item prop="password">
          <el-input v-model="form.password" type="password" placeholder="密码" size="large" :prefix-icon="Lock" show-password />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" size="large" style="width: 100%" :loading="loading" native-type="submit">
            登 录
          </el-button>
        </el-form-item>
      </el-form>

      <div class="register-link">
        还没有账号？ <router-link :to="{ path: '/register', query: $route.query.returnUrl ? { returnUrl: $route.query.returnUrl } : {} }">立即注册</router-link>
      </div>

      <div class="admin-link">
        <el-icon style="margin-right: 4px; vertical-align: middle;" :size="14"><UserFilled /></el-icon>
        <a href="https://ssoadmin.taipi.top" target="_blank">管理员登录</a>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRoute } from 'vue-router'
import { User, Lock, UserFilled } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { login } from '@/api/auth'
import logoSrc from '@/assets/logo.png'

const route = useRoute()

const formRef = ref<FormInstance>()
const form = reactive({
  username: '',
  password: ''
})
const rules: FormRules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}
const loading = ref(false)
const returnUrl = ref((route.query.returnUrl as string) || '/')

const handleSubmit = async () => {
  if (!formRef.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  loading.value = true
  try {
    await login({ username: form.username, password: form.password })
    window.location.href = returnUrl.value
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-container {
  width: 100%;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f0f5ff 0%, #e8f0fe 50%, #f5f0ff 100%);
  position: relative;
  overflow: hidden;
}

.login-container::before {
  content: '';
  position: absolute;
  top: -250px;
  right: -200px;
  width: 600px;
  height: 600px;
  background: radial-gradient(circle, rgba(124,58,237,0.1) 0%, transparent 70%);
  border-radius: 50%;
}

.login-container::after {
  content: '';
  position: absolute;
  bottom: -180px;
  left: -150px;
  width: 500px;
  height: 500px;
  background: radial-gradient(circle, rgba(103,194,58,0.07) 0%, transparent 70%);
  border-radius: 50%;
}

.bubble {
  position: absolute;
  border-radius: 50%;
  z-index: 0;
  animation: float 6s ease-in-out infinite;
}

.bubble-1 {
  width: 80px;
  height: 80px;
  top: 15%;
  left: 8%;
  background: radial-gradient(circle at 30% 30%, rgba(124,58,237,0.12), rgba(124,58,237,0.04));
  animation-delay: 0s;
}

.bubble-2 {
  width: 120px;
  height: 120px;
  top: 60%;
  left: 5%;
  background: radial-gradient(circle at 30% 30%, rgba(124,58,237,0.1), rgba(124,58,237,0.03));
  animation-delay: 1s;
  animation-duration: 8s;
}

.bubble-3 {
  width: 60px;
  height: 60px;
  top: 25%;
  right: 12%;
  background: radial-gradient(circle at 30% 30%, rgba(103,194,58,0.1), rgba(103,194,58,0.03));
  animation-delay: 2s;
  animation-duration: 7s;
}

.bubble-4 {
  width: 100px;
  height: 100px;
  top: 70%;
  right: 8%;
  background: radial-gradient(circle at 30% 30%, rgba(124,58,237,0.08), rgba(124,58,237,0.02));
  animation-delay: 0.5s;
  animation-duration: 9s;
}

.bubble-5 {
  width: 40px;
  height: 40px;
  top: 45%;
  left: 50%;
  background: radial-gradient(circle at 30% 30%, rgba(124,58,237,0.12), rgba(124,58,237,0.04));
  animation-delay: 3s;
  animation-duration: 6s;
}

@keyframes float {
  0%, 100% { transform: translateY(0) scale(1); }
  50% { transform: translateY(-20px) scale(1.05); }
}

.login-box {
  position: relative;
  width: 420px;
  margin: 0 auto;
  padding: 40px;
  background: white;
  border-radius: 16px;
  box-shadow: 0 8px 40px rgba(0, 0, 0, 0.08);
  border: 1px solid #f0f2f5;
  z-index: 1;
}

.logo-area {
  text-align: center;
  margin-bottom: 8px;
}

.logo-area .logo-img {
  max-width: 100%;
  height: auto;
  max-height: 64px;
}

.title {
  text-align: center;
  margin-bottom: 30px;
  color: #333;
  font-size: 24px;
  font-weight: 600;
}

.register-link {
  text-align: center;
  margin-top: 16px;
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

.admin-link {
  text-align: center;
  margin-top: 20px;
  color: #999;
  font-size: 14px;
}

.admin-link a {
  color: #999;
  text-decoration: none;
}

.admin-link a:hover {
  color: #409eff;
  text-decoration: underline;
}
</style>