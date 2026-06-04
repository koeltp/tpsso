<template>
  <div class="login-container">
    <h2>TPSSO 统一登录</h2>
    <form @submit.prevent="handleLogin">
      <input type="text" v-model="username" placeholder="用户名" required />
      <input type="password" v-model="password" placeholder="密码" required />
      <label>
        <input type="checkbox" v-model="rememberMe" /> 记住我
      </label>
      <button type="submit">登录</button>
    </form>
    <p v-if="error" class="error">{{ error }}</p>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'

const username = ref('')
const password = ref('')
const rememberMe = ref(false)
const error = ref('')
const route = useRoute()
const returnUrl = ref('')

onMounted(() => {
  returnUrl.value = route.query.returnUrl || '/'
})

const handleLogin = async () => {
  error.value = ''
  try {
    await axios.post('/api/account/login', {
      username: username.value,
      password: password.value,
      rememberMe: rememberMe.value
    })
    // 登录成功，重定向回认证服务器的授权端点
    window.location.href = returnUrl.value
  } catch (err) {
    error.value = '用户名或密码错误'
  }
}
</script>