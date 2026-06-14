<template>
  <!-- 两步验证步骤 -->
  <template v-if="step === '2fa'">
    <div class="logo-area">
      <img :src="logoSrc" alt="TPSSO" class="logo-img" />
    </div>
    <h1 class="title">两步验证</h1>
    <p class="twofa-hint">请输入身份验证器中的6位验证码</p>

    <el-form ref="twoFaFormRef" :model="twoFaForm" :rules="twoFaRules" label-position="top" @submit.prevent="handleTwoFactorSubmit">
      <el-form-item prop="code">
        <el-input-otp v-model="twoFaForm.code" :length="6" size="large" inputmode="numeric" :validator="otpValidator" @finish="handleTwoFactorSubmit" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" size="large" style="width: 100%" :loading="loading" native-type="submit">
          验 证
        </el-button>
      </el-form-item>
    </el-form>

    <div class="recovery-toggle">
      <el-button link type="primary" @click="step = 'recovery'">使用恢复码登录</el-button>
      <span class="divider">|</span>
      <el-button link @click="backToLogin">返回登录</el-button>
    </div>
  </template>

  <!-- 恢复码登录步骤 -->
  <template v-else-if="step === 'recovery'">
    <div class="logo-area">
      <img :src="logoSrc" alt="TPSSO" class="logo-img" />
    </div>
    <h1 class="title">恢复码登录</h1>
    <p class="twofa-hint">请输入一个恢复码</p>

    <el-form ref="recoveryFormRef" :model="recoveryForm" :rules="recoveryRules" label-position="top" @submit.prevent="handleRecoverySubmit">
      <el-form-item prop="code">
        <el-input v-model="recoveryForm.code" placeholder="恢复码（如 A1B2-C3D4）" size="large" autocomplete="off" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" size="large" style="width: 100%" :loading="loading" native-type="submit">
          验 证
        </el-button>
      </el-form-item>
    </el-form>

    <div class="recovery-toggle">
      <el-button link type="primary" @click="step = '2fa'">使用验证码登录</el-button>
      <span class="divider">|</span>
      <el-button link @click="backToLogin">返回登录</el-button>
    </div>
  </template>

  <!-- 正常登录步骤 -->
  <template v-else>
    <div class="logo-area">
      <img :src="logoSrc" alt="TPSSO" class="logo-img" />
    </div>
    <h1 class="title">TPSSO 登录</h1>

    <!-- 外部登录错误提示 -->
    <el-alert v-if="externalError" :title="externalError" type="error" show-icon :closable="true" class="external-error" />

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

    <!-- 第三方登录 -->
    <div v-if="providers.length > 0" class="external-login">
      <div class="divider-line">
        <span>其他登录方式</span>
      </div>
      <div class="provider-buttons">
        <el-button
          v-for="provider in providers"
          :key="provider.scheme"
          size="large"
          class="provider-btn"
          @click="handleExternalLogin(provider.scheme)"
        >
          <svg v-if="provider.scheme === 'GitHub'" class="provider-icon" viewBox="0 0 24 24" fill="currentColor"><path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/></svg>
          <svg v-else-if="provider.scheme === 'Google'" class="provider-icon" viewBox="0 0 24 24"><path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z" fill="#4285F4"/><path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/><path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/><path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/></svg>
          <svg v-else-if="provider.scheme === 'WeChat'" class="provider-icon" viewBox="0 0 24 24" fill="#07C160"><path d="M8.691 2.188C3.891 2.188 0 5.476 0 9.53c0 2.212 1.17 4.203 3.002 5.55a.59.59 0 0 1 .213.665l-.39 1.48c-.019.07-.048.141-.048.213 0 .163.13.295.29.295a.326.326 0 0 0 .167-.054l1.903-1.114a.864.864 0 0 1 .717-.098 10.16 10.16 0 0 0 2.837.403c.276 0 .543-.027.811-.05-.857-2.578.157-4.972 1.932-6.446 1.703-1.415 3.882-1.98 5.853-1.838-.576-3.583-4.196-6.348-8.596-6.348zM5.785 5.991c.642 0 1.162.529 1.162 1.18a1.17 1.17 0 0 1-1.162 1.178A1.17 1.17 0 0 1 4.623 7.17c0-.651.52-1.18 1.162-1.18zm5.813 0c.642 0 1.162.529 1.162 1.18a1.17 1.17 0 0 1-1.162 1.178 1.17 1.17 0 0 1-1.162-1.178c0-.651.52-1.18 1.162-1.18zm5.34 2.867c-1.797-.052-3.746.512-5.28 1.786-1.72 1.428-2.687 3.72-1.78 6.22.942 2.453 3.666 4.229 6.884 4.229.826 0 1.622-.12 2.361-.336a.722.722 0 0 1 .598.082l1.584.926a.272.272 0 0 0 .14.047c.134 0 .24-.111.24-.247 0-.06-.023-.12-.038-.177l-.327-1.233a.582.582 0 0 1-.023-.156.49.49 0 0 1 .201-.398C23.024 18.48 24 16.82 24 14.98c0-3.21-2.931-5.837-7.062-6.122zM14.033 13.3c.535 0 .969.44.969.982a.976.976 0 0 1-.969.983.976.976 0 0 1-.969-.983c0-.542.434-.982.97-.982zm4.844 0c.535 0 .969.44.969.982a.976.976 0 0 1-.969.983.976.976 0 0 1-.969-.983c0-.542.434-.982.97-.982z"/></svg>
          <span class="provider-name">{{ provider.displayName }}</span>
        </el-button>
      </div>
    </div>

    <div class="bottom-link">
      还没有账号？ <router-link to="/register">立即注册</router-link>
      <span class="divider">|</span>
      <router-link to="/forgot-password">忘记密码</router-link>
    </div>
  </template>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { User, Lock } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { login, loginTwoFactor, getExternalProviders, externalLogin, type ExternalProvider } from '@/api/auth'
import { useUserStore } from '@/stores/user'
import logoSrc from '@/assets/logo.png'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

// 登录步骤：login → 2fa / recovery
const step = ref<'login' | '2fa' | 'recovery'>('login')
const twoFactorUserId = ref('')

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

const twoFaFormRef = ref<FormInstance>()
const twoFaForm = reactive({ code: '' })
const twoFaRules: FormRules = {
  code: [
    { required: true, message: '请输入验证码', trigger: 'blur' },
    { len: 6, message: '验证码为6位数字', trigger: 'blur' }
  ]
}

const recoveryFormRef = ref<FormInstance>()
const recoveryForm = reactive({ code: '' })
const recoveryRules: FormRules = {
  code: [{ required: true, message: '请输入恢复码', trigger: 'blur' }]
}

const loading = ref(false)
const returnUrl = ref((route.query.returnUrl as string) || '/')
const externalError = ref((route.query.externalError as string) || '')
const providers = ref<ExternalProvider[]>([])

/** OTP 输入框只允许数字 */
const otpValidator = (char: string) => /^\d$/.test(char)

onMounted(async () => {
  try {
    providers.value = await getExternalProviders()
  } catch {
    // 获取失败不影响主流程
  }
})

/** 处理登录成功后的跳转 */
function handleLoginSuccess() {
  if (returnUrl.value.startsWith('/connect/') || returnUrl.value.startsWith('http')) {
    window.location.href = returnUrl.value
  } else if (returnUrl.value && returnUrl.value !== '/') {
    router.push(returnUrl.value)
  } else {
    const adminUrl = import.meta.env.VITE_ADMIN_URL || 'http://localhost:3009'
    window.location.href = adminUrl + '/dashboard'
  }
}

/** 返回登录步骤（2FA/恢复码步骤中） */
function backToLogin() {
  step.value = 'login'
  twoFaForm.code = ''
  recoveryForm.code = ''
}

/** 密码登录 */
const handleSubmit = async () => {
  if (!formRef.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  loading.value = true
  try {
    const result = await login({
      username: form.username,
      password: form.password,
      rememberMe: form.rememberMe
    })

    if (result.requiresTwoFactor) {
      // 需要两步验证
      twoFactorUserId.value = result.userId || ''
      step.value = '2fa'
    } else if (result.userInfo) {
      // 直接登录成功
      userStore.setUserInfo(result.userInfo)
      handleLoginSuccess()
    }
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}

/** 两步验证码登录 */
const handleTwoFactorSubmit = async () => {
  if (!twoFaFormRef.value) return
  const valid = await twoFaFormRef.value.validate().catch(() => false)
  if (!valid) return

  loading.value = true
  try {
    const userInfo = await loginTwoFactor({
      userId: twoFactorUserId.value,
      code: twoFaForm.code,
      isRecoveryCode: false,
      rememberMe: form.rememberMe
    })
    userStore.setUserInfo(userInfo)
    handleLoginSuccess()
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}

/** 恢复码登录 */
const handleRecoverySubmit = async () => {
  if (!recoveryFormRef.value) return
  const valid = await recoveryFormRef.value.validate().catch(() => false)
  if (!valid) return

  loading.value = true
  try {
    const userInfo = await loginTwoFactor({
      userId: twoFactorUserId.value,
      code: recoveryForm.code,
      isRecoveryCode: true,
      rememberMe: form.rememberMe
    })
    userStore.setUserInfo(userInfo)
    handleLoginSuccess()
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}

/** 发起第三方登录 */
const handleExternalLogin = (scheme: string) => {
  externalLogin(scheme, returnUrl.value !== '/' ? returnUrl.value : undefined)
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

.twofa-hint {
  text-align: center;
  color: #666;
  font-size: 14px;
  margin-bottom: 20px;
}

.recovery-toggle {
  text-align: center;
  margin-top: 8px;
}

.external-error {
  margin-bottom: 16px;
}

/* 第三方登录区域 */
.external-login {
  margin-top: 20px;
}

.divider-line {
  display: flex;
  align-items: center;
  margin-bottom: 16px;
  color: #999;
  font-size: 13px;
}

.divider-line::before,
.divider-line::after {
  content: '';
  flex: 1;
  height: 1px;
  background: #e4e7ed;
}

.divider-line span {
  padding: 0 12px;
}

.provider-buttons {
  display: flex;
  gap: 10px;
}

.provider-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}

.provider-icon {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
}

.provider-name {
  font-size: 14px;
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

.divider {
  margin: 0 8px;
  color: #ccc;
}
</style>
