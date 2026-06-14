<template>
  <div class="profile-container">
    <el-card shadow="never">
      <el-tabs v-model="activeTab">
        <!-- 个人信息 -->
        <el-tab-pane label="个人信息" name="info">
          <div class="avatar-section">
            <el-upload class="avatar-uploader" action="/api/account/avatar" name="file" :show-file-list="false"
              :headers="uploadHeaders" :on-success="handleAvatarSuccess" :before-upload="beforeAvatarUpload"
              accept="image/jpeg,image/png,image/gif,image/webp">
              <el-avatar :size="80" :src="profileForm.avatarUrl || undefined" class="avatar-preview"
                :class="{ 'has-avatar': profileForm.avatarUrl }">
                <span v-if="!profileForm.avatarUrl">{{ userInfo.username?.charAt(0).toUpperCase() }}</span>
              </el-avatar>
              <div class="avatar-overlay">点击上传</div>
            </el-upload>
          </div>

          <el-form :model="profileForm" label-width="80px" style="max-width: 480px; margin-top: 24px">
            <el-form-item label="用户名">
              <el-input :model-value="userInfo.username" disabled />
            </el-form-item>
            <el-form-item label="邮箱">
              <el-input :model-value="userInfo.email" disabled />
            </el-form-item>
            <el-form-item label="昵称">
              <el-input v-model="profileForm.nickName" placeholder="请输入昵称" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="profileLoading" @click="handleUpdateProfile">保存修改</el-button>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 两步验证 -->
        <el-tab-pane label="两步验证" name="2fa">
          <div class="twofa-section">
            <!-- 已启用状态 -->
            <template v-if="userInfo.twoFactorEnabled">
              <el-result icon="success" title="两步验证已启用" sub-title="您的账户受到额外保护">
                <template #extra>
                  <el-button type="danger" :loading="disableLoading" @click="handleDisable2FA">禁用两步验证</el-button>
                  <el-button @click="handleResetCodes">重新生成恢复码</el-button>
                </template>
              </el-result>
            </template>

            <!-- 未启用 - 绑定流程 -->
            <template v-else>
              <!-- 步骤1: 启用两步验证 -->
              <template v-if="setupStep === 0">
                <el-result icon="warning" title="两步验证未启用" sub-title="启用后登录时需要额外输入验证码，大幅提升账户安全性">
                  <template #extra>
                    <el-button type="primary" :loading="setupLoading" @click="handleSetup2FA">启用两步验证</el-button>
                  </template>
                </el-result>
              </template>

              <!-- 步骤2: 扫描二维码 -->
              <template v-else-if="setupStep === 1">
                <h4 style="margin-bottom: 12px">第一步：扫描二维码</h4>
                <p class="twofa-step-hint">使用 Google Authenticator、Microsoft Authenticator 等App扫描下方二维码</p>
                <div class="qrcode-wrapper">
                  <canvas ref="qrcodeCanvas"></canvas>
                </div>
                <div class="shared-key-section">
                  <p class="twofa-step-hint">无法扫描？手动输入密钥：</p>
                  <el-input :model-value="setupData.sharedKey" readonly>
                    <template #append>
                      <el-button @click="copyKey">复制</el-button>
                    </template>
                  </el-input>
                </div>

                <h4 style="margin: 20px 0 12px">第二步：输入验证码确认</h4>
                <el-form :model="verifyForm" :rules="verifyRules" ref="verifyFormRef" style="max-width: 300px">
                  <el-form-item prop="code">
                    <el-input-otp v-model="verifyForm.code" :length="6" size="large" inputmode="numeric" :validator="otpValidator" @finish="handleEnable2FA" />
                  </el-form-item>
                  <el-form-item>
                    <el-button type="primary" :loading="enableLoading" @click="handleEnable2FA">确认启用</el-button>
                    <el-button @click="cancelSetup">取消</el-button>
                  </el-form-item>
                </el-form>
              </template>
            </template>

            <!-- 恢复码展示弹窗 -->
            <el-dialog v-model="recoveryDialogVisible" title="恢复码" width="480px" :close-on-click-modal="false">
              <el-alert type="warning" :closable="false" show-icon style="margin-bottom: 16px">
                <template #title>请妥善保存这些恢复码！每个恢复码只能使用一次。丢失验证器时可用恢复码登录。</template>
              </el-alert>
              <div class="recovery-codes-grid">
                <div v-for="code in recoveryCodes" :key="code" class="recovery-code-item">{{ code }}</div>
              </div>
              <template #footer>
                <el-button @click="downloadRecoveryCodes">下载恢复码</el-button>
                <el-button type="primary" @click="recoveryDialogVisible = false">我已妥善保存</el-button>
              </template>
            </el-dialog>
          </div>
        </el-tab-pane>

        <!-- 第三方账号 -->
        <el-tab-pane label="第三方账号" name="external">
          <div class="external-section">
            <div v-for="provider in externalProviders" :key="provider.scheme" class="external-provider-item">
              <div class="provider-info">
                <svg v-if="provider.scheme === 'GitHub'" class="provider-icon" viewBox="0 0 24 24" fill="currentColor"><path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/></svg>
                <svg v-else-if="provider.scheme === 'Google'" class="provider-icon" viewBox="0 0 24 24"><path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z" fill="#4285F4"/><path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/><path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/><path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/></svg>
                <svg v-else-if="provider.scheme === 'WeChat'" class="provider-icon" viewBox="0 0 24 24" fill="#07C160"><path d="M8.691 2.188C3.891 2.188 0 5.476 0 9.53c0 2.212 1.17 4.203 3.002 5.55a.59.59 0 0 1 .213.665l-.39 1.48c-.019.07-.048.141-.048.213 0 .163.13.295.29.295a.326.326 0 0 0 .167-.054l1.903-1.114a.864.864 0 0 1 .717-.098 10.16 10.16 0 0 0 2.837.403c.276 0 .543-.027.811-.05-.857-2.578.157-4.972 1.932-6.446 1.703-1.415 3.882-1.98 5.853-1.838-.576-3.583-4.196-6.348-8.596-6.348zM5.785 5.991c.642 0 1.162.529 1.162 1.18a1.17 1.17 0 0 1-1.162 1.178A1.17 1.17 0 0 1 4.623 7.17c0-.651.52-1.18 1.162-1.18zm5.813 0c.642 0 1.162.529 1.162 1.18a1.17 1.17 0 0 1-1.162 1.178 1.17 1.17 0 0 1-1.162-1.178c0-.651.52-1.18 1.162-1.18zm5.34 2.867c-1.797-.052-3.746.512-5.28 1.786-1.72 1.428-2.687 3.72-1.78 6.22.942 2.453 3.666 4.229 6.884 4.229.826 0 1.622-.12 2.361-.336a.722.722 0 0 1 .598.082l1.584.926a.272.272 0 0 0 .14.047c.134 0 .24-.111.24-.247 0-.06-.023-.12-.038-.177l-.327-1.233a.582.582 0 0 1-.023-.156.49.49 0 0 1 .201-.398C23.024 18.48 24 16.82 24 14.98c0-3.21-2.931-5.837-7.062-6.122zM14.033 13.3c.535 0 .969.44.969.982a.976.976 0 0 1-.969.983.976.976 0 0 1-.969-.983c0-.542.434-.982.97-.982zm4.844 0c.535 0 .969.44.969.982a.976.976 0 0 1-.969.983.976.976 0 0 1-.969-.983c0-.542.434-.982.97-.982z"/></svg>
                <span v-else class="provider-icon-fallback">{{ provider.displayName.charAt(0) }}</span>
                <div class="provider-detail">
                  <span class="provider-name">{{ provider.displayName }}</span>
                  <span v-if="provider.isBound" class="provider-bound-info">已绑定{{ provider.boundDisplayName ? ` (${provider.boundDisplayName})` : '' }}</span>
                  <span v-else class="provider-unbound-info">未绑定</span>
                </div>
              </div>
              <div class="provider-action">
                <el-button v-if="provider.isBound" type="danger" plain size="small" :loading="unbindLoading === provider.scheme" @click="handleUnbind(provider.scheme)">解绑</el-button>
                <el-button v-else type="primary" plain size="small" @click="handleBind(provider.scheme)">绑定</el-button>
              </div>
            </div>

            <el-empty v-if="externalProviders.length === 0" description="暂无可用的第三方登录" />
          </div>
        </el-tab-pane>

        <!-- 修改密码 -->
        <el-tab-pane label="修改密码" name="password">
          <el-form :model="passwordForm" label-width="80px" style="max-width: 480px" :rules="passwordRules"
            ref="passwordFormRef">
            <el-form-item label="当前密码" prop="currentPassword">
              <el-input v-model="passwordForm.currentPassword" type="password" show-password placeholder="请输入当前密码" />
            </el-form-item>
            <el-form-item label="新密码" prop="newPassword">
              <el-input v-model="passwordForm.newPassword" type="password" show-password placeholder="请输入新密码" />
            </el-form-item>
            <el-form-item label="确认密码" prop="confirmPassword">
              <el-input v-model="passwordForm.confirmPassword" type="password" show-password placeholder="请再次输入新密码" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="passwordLoading" @click="handleChangePassword">修改密码</el-button>
            </el-form-item>
          </el-form>
        </el-tab-pane>
      </el-tabs>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed, nextTick } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules, type UploadProps } from 'element-plus'
import { updateProfile, changePassword, setupTwoFactor, enableTwoFactor, disableTwoFactor, resetRecoveryCodes, getExternalLogins, getBindExternalLoginUrl, removeExternalLogin, type UpdateProfileModel, type ChangePasswordModel, type TwoFactorSetupResult, type ExternalLoginProvider } from '@/api/auth'
import { useUserStore } from '@/stores/user'

const activeTab = ref('info')
const route = useRoute()
const userStore = useUserStore()
const userInfo = computed(() => userStore.userInfo || { username: '', email: '', avatarUrl: '', nickName: '', roles: [] as string[], twoFactorEnabled: false })
const profileForm = reactive<UpdateProfileModel>({ nickName: '', avatarUrl: '' })
const profileLoading = ref(false)

const passwordFormRef = ref<FormInstance>()
const passwordForm = reactive<{ currentPassword: string; newPassword: string; confirmPassword: string }>({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})
const passwordLoading = ref(false)

const uploadHeaders = computed(() => {
  const token = userStore.token
  return token ? { Authorization: `Bearer ${token}` } : {}
})

const passwordRules: FormRules = {
  currentPassword: [{ required: true, message: '请输入当前密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请再次输入新密码', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value !== passwordForm.newPassword) {
          callback(new Error('两次输入的密码不一致'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ]
}

// ──────── 两步验证 ────────
const setupStep = ref(0)
const setupLoading = ref(false)
const enableLoading = ref(false)
const disableLoading = ref(false)
const setupData = ref<TwoFactorSetupResult>({ sharedKey: '', authenticatorUri: '', recoveryCodes: [] })
const qrcodeCanvas = ref<HTMLCanvasElement>()
const verifyFormRef = ref<FormInstance>()
const verifyForm = reactive({ code: '' })
const verifyRules: FormRules = {
  code: [
    { required: true, message: '请输入验证码', trigger: 'blur' },
    { len: 6, message: '验证码为6位数字', trigger: 'blur' }
  ]
}
const recoveryDialogVisible = ref(false)
const recoveryCodes = ref<string[]>([])

/** OTP 输入框只允许数字 */
const otpValidator = (char: string) => /^\d$/.test(char)

// ──────── 外部登录绑定 ────────
const externalProviders = ref<ExternalLoginProvider[]>([])
const unbindLoading = ref('')

onMounted(async () => {
  await userStore.fetchUserInfo()
  if (userStore.userInfo) {
    profileForm.nickName = userStore.userInfo.nickName ?? ''
    profileForm.avatarUrl = userStore.userInfo.avatarUrl ?? ''
  }

  // 加载外部登录列表
  await loadExternalProviders()

  // 处理绑定回调的URL参数
  const tab = route.query.tab as string
  if (tab === 'external') {
    activeTab.value = 'external'
  }
  const bindSuccess = route.query.bindSuccess as string
  const bindError = route.query.bindError as string
  if (bindSuccess) {
    ElMessage.success(`${bindSuccess} 绑定成功`)
    await loadExternalProviders()
  }
  if (bindError) {
    ElMessage.error(bindError)
  }
})

/** 加载第三方登录绑定列表 */
async function loadExternalProviders() {
  try {
    externalProviders.value = await getExternalLogins()
  } catch {
    // 拦截器已处理
  }
}

/** 发起绑定第三方登录 */
function handleBind(provider: string) {
  // 跳转到 Auth 项目的绑定端点，会302到第三方授权页
  const url = getBindExternalLoginUrl(provider)
  window.location.href = url
}

/** 解绑第三方登录 */
async function handleUnbind(provider: string) {
  try {
    await ElMessageBox.confirm(
      `确定要解绑 ${provider} 账号吗？解绑后将无法使用 ${provider} 登录。`,
      '解绑确认',
      { confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning' }
    )
  } catch {
    return // 用户取消
  }

  unbindLoading.value = provider
  try {
    await removeExternalLogin(provider)
    ElMessage.success('已解绑')
    await loadExternalProviders()
  } catch {
    // 拦截器已处理
  } finally {
    unbindLoading.value = ''
  }
}

const beforeAvatarUpload: UploadProps['beforeUpload'] = (rawFile) => {
  const allowed = (import.meta.env.VITE_AVATAR_ALLOWED_TYPES || 'image/jpeg,image/png,image/gif,image/webp').split(',')
  if (!allowed.includes(rawFile.type)) {
    const extensions = allowed.map((x: string) => x.split('/')[1].toUpperCase())
    ElMessage.error(`仅支持 ${extensions.join('、')} 格式`)
    return false
  }
  const maxSizeMB = Number(import.meta.env.VITE_AVATAR_MAX_SIZE_MB) || 2
  if (rawFile.size / 1024 / 1024 > maxSizeMB) {
    ElMessage.error(`图片大小不能超过 ${maxSizeMB}MB`)
    return false
  }
  return true
}

const handleAvatarSuccess: UploadProps['onSuccess'] = (response) => {
  if (response.code === 0 && response.data) {
    profileForm.avatarUrl = response.data
    if (userStore.userInfo) {
      userStore.updateUserInfo({
        avatarUrl: response.data
      })
    }
    ElMessage.success('头像上传成功')
  } else {
    ElMessage.error(response.message || '上传失败')
  }
}

const handleUpdateProfile = async () => {
  profileLoading.value = true
  try {
    await updateProfile(profileForm)
    ElMessage.success('修改成功')
    userStore.updateUserInfo({
      nickName: profileForm.nickName,
      avatarUrl: profileForm.avatarUrl ?? ''
    })
  } catch {
    // 拦截器已处理
  } finally {
    profileLoading.value = false
  }
}

const handleChangePassword = async () => {
  if (!passwordFormRef.value) return
  await passwordFormRef.value.validate(async (valid) => {
    if (!valid) return
    passwordLoading.value = true
    try {
      const data: ChangePasswordModel = {
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword
      }
      await changePassword(data)
      ElMessage.success('密码修改成功')
      passwordForm.currentPassword = ''
      passwordForm.newPassword = ''
      passwordForm.confirmPassword = ''
      passwordFormRef.value?.resetFields()
    } catch {
      // 拦截器已处理
    } finally {
      passwordLoading.value = false
    }
  })
}

// ──────── 两步验证操作 ────────

/** 生成密钥和二维码 */
const handleSetup2FA = async () => {
  setupLoading.value = true
  try {
    setupData.value = await setupTwoFactor()
    setupStep.value = 1
    // 等待 DOM 渲染后绘制二维码
    await nextTick()
    drawQRCode(setupData.value.authenticatorUri)
  } catch {
    // 拦截器已处理
  } finally {
    setupLoading.value = false
  }
}

/** 取消设置流程 */
function cancelSetup() {
  setupStep.value = 0
  verifyForm.code = ''
}

/** 绘制二维码到 Canvas */
async function drawQRCode(text: string) {
  if (!qrcodeCanvas.value) return
  // 动态导入 qrcode 库
  const QRCode = (await import('qrcode')).default
  QRCode.toCanvas(qrcodeCanvas.value, text, {
    width: 200,
    margin: 2,
    color: { dark: '#000000', light: '#ffffff' }
  })
}

/** 确认启用两步验证 */
const handleEnable2FA = async () => {
  if (!verifyFormRef.value) return
  const valid = await verifyFormRef.value.validate().catch(() => false)
  if (!valid) return

  enableLoading.value = true
  try {
    const result = await enableTwoFactor(verifyForm.code)
    // 展示恢复码
    recoveryCodes.value = result.recoveryCodes
    recoveryDialogVisible.value = true
    // 更新用户信息
    userStore.updateUserInfo({ twoFactorEnabled: true })
    setupStep.value = 0
    verifyForm.code = ''
  } catch {
    // 拦截器已处理
  } finally {
    enableLoading.value = false
  }
}

/** 禁用两步验证 */
const handleDisable2FA = async () => {
  disableLoading.value = true
  try {
    await disableTwoFactor()
    ElMessage.success('两步验证已禁用')
    userStore.updateUserInfo({ twoFactorEnabled: false })
    setupStep.value = 0
  } catch {
    // 拦截器已处理
  } finally {
    disableLoading.value = false
  }
}

/** 重新生成恢复码 */
const handleResetCodes = async () => {
  try {
    recoveryCodes.value = await resetRecoveryCodes()
    recoveryDialogVisible.value = true
  } catch {
    // 拦截器已处理
  }
}

/** 复制密钥 */
const copyKey = () => {
  navigator.clipboard.writeText(setupData.value.sharedKey).then(() => {
    ElMessage.success('密钥已复制')
  }).catch(() => {
    ElMessage.error('复制失败')
  })
}

/** 下载恢复码为文本文件 */
function downloadRecoveryCodes() {
  const lines = [
    'TPSSO 两步验证恢复码',
    `生成时间：${new Date().toLocaleString()}`,
    '注意：每个恢复码只能使用一次，请妥善保管！',
    '',
    ...recoveryCodes.value.map((code, i) => `${String(i + 1).padStart(2, ' ')}. ${code}`)
  ]
  const blob = new Blob([lines.join('\n')], { type: 'text/plain;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `TPSSO-恢复码-${new Date().toISOString().slice(0, 10)}.txt`
  a.click()
  URL.revokeObjectURL(url)
}
</script>

<style scoped>
.profile-container {
  padding: 20px;
}

.profile-container :deep(.el-card__body) {
  padding: 0 24px 24px;
}

.profile-container :deep(.el-tabs__header) {
  margin: 0;
  padding-top: 16px;
}

.profile-container :deep(.el-tab-pane) {
  padding-top: 20px;
}

.avatar-section {
  padding-top: 24px;
  display: flex;
  justify-content: flex-start;
  align-items: center;
}

.avatar-uploader {
  position: relative;
  cursor: pointer;
  border-radius: 50%;
  overflow: hidden;
}

.avatar-preview {
  width: 80px;
  height: 80px;
  font-size: 28px;
  color: white;
  font-weight: 600;
}

.avatar-preview:not(.has-avatar) {
  background: orange;
}

.avatar-overlay {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 28px;
  line-height: 28px;
  text-align: center;
  background: rgba(0, 0, 0, 0.5);
  color: white;
  font-size: 12px;
  opacity: 0;
  transition: opacity 0.2s;
}

.avatar-uploader:hover .avatar-overlay {
  opacity: 1;
}

/* 两步验证 */
.twofa-section {
  padding-top: 12px;
}

.twofa-step-hint {
  color: #666;
  font-size: 14px;
  margin-bottom: 12px;
}

.qrcode-wrapper {
  display: flex;
  justify-content: center;
  margin: 16px 0;
}

.shared-key-section {
  max-width: 400px;
  margin-bottom: 16px;
}

.recovery-codes-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
}

.recovery-code-item {
  padding: 8px 12px;
  background: #f5f7fa;
  border-radius: 4px;
  font-family: monospace;
  font-size: 14px;
  text-align: center;
  user-select: all;
}

/* 第三方账号 */
.external-section {
  padding-top: 12px;
}

.external-provider-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 0;
  border-bottom: 1px solid #f0f0f0;
}

.external-provider-item:last-child {
  border-bottom: none;
}

.provider-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.provider-icon {
  width: 32px;
  height: 32px;
  flex-shrink: 0;
}

.provider-icon-fallback {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #409eff;
  color: white;
  border-radius: 50%;
  font-size: 14px;
  font-weight: 600;
  flex-shrink: 0;
}

.provider-detail {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.provider-name {
  font-size: 15px;
  font-weight: 500;
  color: #303133;
}

.provider-bound-info {
  font-size: 12px;
  color: #67c23a;
}

.provider-unbound-info {
  font-size: 12px;
  color: #909399;
}
</style>
