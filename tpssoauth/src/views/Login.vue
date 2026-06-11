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
      <el-checkbox v-model="form.rememberMe">记住我</el-checkbox>
    </el-form-item>
    <el-form-item>
      <el-button type="primary" size="large" style="width: 100%" :loading="loading" native-type="submit">
        登 录
      </el-button>
    </el-form-item>
  </el-form>
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
  password: '',
  rememberMe: false
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
    const userInfo = await login({
      username: form.username,
      password: form.password,
      rememberMe: form.rememberMe
    })
    userStore.setUserInfo(userInfo)

    // 如果 returnUrl 指向 OAuth 授权端点，直接跳转
    if (returnUrl.value.startsWith('/connect/') || returnUrl.value.startsWith('http')) {
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
</style>
