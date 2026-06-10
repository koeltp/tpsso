<template>
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

  <el-divider>第三方登录</el-divider>

  <div class="social-login">
    <el-button size="large" style="width: 100%" @click="handleGitHubLogin">
      <svg viewBox="0 0 16 16" width="18" height="18" style="margin-right: 6px; vertical-align: middle;">
        <path fill="currentColor" d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z"/>
      </svg>
      GitHub 登录
    </el-button>
  </div>

  <div class="register-link">
    还没有账号？ <router-link :to="{ path: '/register', query: $route.query.returnUrl ? { returnUrl: $route.query.returnUrl } : {} }">立即注册</router-link>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { User, Lock } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { login } from '@/api/auth'
import { useUserStore } from '@/stores/user'
import logoSrc from '@/assets/logo.png'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

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
    const result = await login({ username: form.username, password: form.password })
    userStore.setAuth(result)
    if (returnUrl.value.startsWith('http') || returnUrl.value.startsWith('/connect/')) {
      window.location.href = returnUrl.value
    } else {
      router.push(returnUrl.value)
    }
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}

/** GitHub 登录：跳转到后端 GitHub OAuth 端点 */
const handleGitHubLogin = () => {
  // 后端会重定向到 GitHub，回调后携带 token 重定向回前端
  const callbackReturnUrl = window.location.origin + '/github-callback'
  window.location.href = `/api/external/github?returnUrl=${encodeURIComponent(callbackReturnUrl)}`
}
</script>

<style scoped>
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

.social-login {
  margin-bottom: 16px;
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
</style>
