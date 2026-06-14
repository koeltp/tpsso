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
import { ElMessage, type FormInstance, type FormRules, type UploadProps } from 'element-plus'
import { updateProfile, changePassword, setupTwoFactor, enableTwoFactor, disableTwoFactor, resetRecoveryCodes, type UpdateProfileModel, type ChangePasswordModel, type TwoFactorSetupResult } from '@/api/auth'
import { useUserStore } from '@/stores/user'

const activeTab = ref('info')
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

onMounted(async () => {
  await userStore.fetchUserInfo()
  if (userStore.userInfo) {
    profileForm.nickName = userStore.userInfo.nickName ?? ''
    profileForm.avatarUrl = userStore.userInfo.avatarUrl ?? ''
  }
})

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
</style>
