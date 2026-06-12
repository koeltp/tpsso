<template>
  <div class="logo-area">
    <img :src="logoSrc" alt="TPSSO" class="logo-img" />
  </div>
  <h1 class="title">重置密码</h1>

  <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
    <el-form-item prop="email">
      <el-input v-model="form.email" placeholder="注册邮箱" size="large" :prefix-icon="Message">
        <template #append>
          <el-button @click="handleSendCode" :disabled="countdown > 0" :loading="sendingCode">
            {{ countdown > 0 ? `${countdown}s` : '获取验证码' }}
          </el-button>
        </template>
      </el-input>
    </el-form-item>
    <el-form-item prop="code">
      <el-input v-model="form.code" placeholder="邮箱验证码" size="large" :prefix-icon="Key" />
    </el-form-item>
    <el-form-item prop="newPassword">
      <el-input v-model="form.newPassword" type="password" placeholder="新密码" size="large" :prefix-icon="Lock" show-password />
    </el-form-item>
    <el-form-item prop="confirmNewPassword">
      <el-input v-model="form.confirmNewPassword" type="password" placeholder="确认新密码" size="large" :prefix-icon="Lock" show-password />
    </el-form-item>
    <el-form-item>
      <el-button type="primary" size="large" style="width: 100%" @click="handleReset" :loading="loading">
        重置密码
      </el-button>
    </el-form-item>
  </el-form>

  <div class="bottom-link">
    <router-link to="/login">返回登录</router-link>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Message, Key, Lock } from '@element-plus/icons-vue'
import { sendResetCode, resetPassword } from '@/api/auth'
import logoSrc from '@/assets/logo.png'

const router = useRouter()

const formRef = ref<FormInstance>()
const loading = ref(false)
const sendingCode = ref(false)
const countdown = ref(0)

const form = reactive({
  email: '',
  code: '',
  newPassword: '',
  confirmNewPassword: ''
})

const validateConfirmPassword = (_rule: any, value: string, callback: any) => {
  if (value !== form.newPassword) {
    callback(new Error('两次输入密码不一致'))
  } else {
    callback()
  }
}

const rules: FormRules = {
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱格式', trigger: 'blur' }
  ],
  code: [{ required: true, message: '请输入验证码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于 6 位', trigger: 'blur' }
  ],
  confirmNewPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: 'blur' }
  ]
}

/** 发送重置验证码 */
const handleSendCode = async () => {
  if (!form.email) {
    ElMessage.warning('请输入邮箱')
    return
  }
  const emailReg = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  if (!emailReg.test(form.email)) {
    ElMessage.warning('请输入正确的邮箱格式')
    return
  }

  sendingCode.value = true
  try {
    await sendResetCode({ email: form.email })
    ElMessage.success('验证码已发送')
    countdown.value = 60
    const timer = setInterval(() => {
      countdown.value--
      if (countdown.value <= 0) clearInterval(timer)
    }, 1000)
  } catch {
    // 拦截器已处理
  } finally {
    sendingCode.value = false
  }
}

/** 重置密码 */
const handleReset = async () => {
  if (!formRef.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  loading.value = true
  try {
    await resetPassword({
      email: form.email,
      code: form.code,
      newPassword: form.newPassword,
      confirmNewPassword: form.confirmNewPassword
    })
    ElMessage.success('密码重置成功，请登录')
    router.push('/login')
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

.bottom-link {
  text-align: center;
  margin-top: 20px;
  color: #666;
}

.bottom-link a {
  color: #409eff;
  text-decoration: none;
}

.bottom-link a:hover {
  text-decoration: underline;
}
</style>
