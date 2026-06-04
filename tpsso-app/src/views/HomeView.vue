<template>
  <div>
    <h1>欢迎回来，{{ username }}！</h1>
    <button @click="logout">退出登录</button>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { userManager } from '@/auth/oidcConfig'

const username = ref('')

onMounted(async () => {
  const user = await userManager.getUser()
  if (user) {
    // 从 id_token 中解析用户信息
    username.value = user.profile?.name || user.profile?.sub
  }
})

const logout = async () => {
  await userManager.signoutRedirect()
  localStorage.removeItem('access_token')
  localStorage.removeItem('id_token')
}
</script>