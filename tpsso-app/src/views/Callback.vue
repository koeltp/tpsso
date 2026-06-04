<template>
  <div>
    <div v-if="loading">正在登录中，请稍后...</div>
    <div v-if="error" class="error">
      <p>登录回调出错：</p>
      <pre>{{ error }}</pre>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { userManager } from '@/auth/oidcConfig'

const router = useRouter()
const loading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    const user = await userManager.signinRedirectCallback()
    // 存储 token 到 localStorage（或 pinia/vuex）
    localStorage.setItem('access_token', user.access_token ?? '')
    localStorage.setItem('id_token', user.id_token ?? '')
    // 跳转到登录前页面
    const redirectUrl = (typeof user.state === 'string' ? user.state : '/')
    router.push(redirectUrl)
  } catch (err) {
    console.error('登录回调失败', err)
    error.value = String(err)
    loading.value = false
    // 5 秒后重定向到首页
    setTimeout(() => router.push('/'), 5000)
  }
})
</script>

<style scoped>
.error {
  color: #e74c3c;
  background: #fdf0ef;
  border: 1px solid #f5c6cb;
  border-radius: 4px;
  padding: 16px;
  margin: 20px;
  max-width: 600px;
}
.error pre {
  white-space: pre-wrap;
  word-break: break-all;
  font-size: 14px;
}
</style>