<template>
  <router-link to="/" class="home-icon" title="返回首页">
    <el-icon><HomeFilled /></el-icon>
  </router-link>
  <div class="logo-area">
    <img :src="logoSrc" alt="TPSSO" />
  </div>
  <h1 class="title">TPSSO 注册</h1>

  <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
    <el-form-item prop="username">
      <el-input v-model="form.username" placeholder="用户名" size="large" :prefix-icon="User" />
    </el-form-item>
    <el-form-item prop="email">
      <el-input v-model="form.email" placeholder="邮箱" size="large" :prefix-icon="Message">
        <template #append>
          <el-button @click="sendCode" :disabled="countdown > 0">
            {{ countdown > 0 ? `${countdown}s` : '获取验证码' }}
          </el-button>
        </template>
      </el-input>
    </el-form-item>
    <el-form-item prop="code">
      <el-input v-model="form.code" placeholder="邮箱验证码" size="large" :prefix-icon="Key" />
    </el-form-item>
    <el-form-item prop="password">
      <el-input v-model="form.password" type="password" placeholder="密码" size="large" :prefix-icon="Lock" show-password />
    </el-form-item>
    <el-form-item prop="confirmPassword">
      <el-input v-model="form.confirmPassword" type="password" placeholder="确认密码" size="large" :prefix-icon="Lock" show-password />
    </el-form-item>
    <el-form-item>
      <el-button type="primary" size="large" style="width: 100%" @click="handleRegister" :loading="loading">
        注册
      </el-button>
    </el-form-item>
  </el-form>

  <div class="login-link">
    已有账号？ <router-link :to="{ path: '/login', query: $route.query.redirect ? { redirect: $route.query.redirect as string } : {} }">立即登录</router-link>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { User, Message, Key, Lock, HomeFilled } from '@element-plus/icons-vue'
import logoSrc from '@/assets/logo-icon.png'

const router = useRouter()
const route = useRoute()

const formRef = ref<FormInstance>()
const loading = ref(false)
const countdown = ref(0)

const form = reactive({
  username: '',
  email: '',
  code: '',
  password: '',
  confirmPassword: ''
})

const validateConfirmPassword = (_rule: any, value: string, callback: any) => {
  if (value !== form.password) {
    callback(new Error('两次输入密码不一致'))
  } else {
    callback()
  }
}

const rules: FormRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 20, message: '用户名长度在 3 到 20 个字符', trigger: 'blur' }
  ],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱格式', trigger: 'blur' }
  ],
  code: [{ required: true, message: '请输入验证码', trigger: 'blur' }],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于 6 位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: 'blur' }
  ]
}

/** 发送验证码（后端接口待实现） */
const sendCode = async () => {
  if (!form.email) {
    ElMessage.warning('请输入邮箱')
    return
  }
  // TODO: 调用后端发送验证码接口
  ElMessage.info('验证码功能待后端实现')
  countdown.value = 60
  const timer = setInterval(() => {
    countdown.value--
    if (countdown.value <= 0) clearInterval(timer)
  }, 1000)
}

/** 注册（后端接口待实现） */
const handleRegister = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    loading.value = true
    try {
      // TODO: 调用后端注册接口
      ElMessage.info('注册功能待后端实现')
      const redirect = route.query.redirect as string
      if (redirect) {
        router.push({ path: '/login', query: { redirect } })
      } else {
        router.push('/login')
      }
    } catch {
      // 拦截器已处理
    } finally {
      loading.value = false
    }
  })
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

.logo-area {
  text-align: center;
  margin-bottom: 16px;
}

.logo-area img {
  height: 56px;
}

.title {
  text-align: center;
  margin-bottom: 30px;
  color: #333;
  font-size: 28px;
  font-weight: 600;
}

.login-link {
  text-align: center;
  margin-top: 20px;
  color: #666;
}

.login-link a {
  color: #409eff;
  text-decoration: none;
}

.login-link a:hover {
  text-decoration: underline;
}
</style>
