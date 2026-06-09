<template>
  <div class="profile-content">
    <el-card class="profile-card" shadow="never">
      <el-tabs v-model="activeTab">
        <!-- 个人信息 -->
        <el-tab-pane label="个人信息" name="info">
          <div class="avatar-section">
            <el-upload
              class="avatar-uploader"
              action="/api/account/avatar"
              name="file"
              :show-file-list="false"
              :headers="uploadHeaders"
              :on-success="handleAvatarSuccess"
              :before-upload="beforeAvatarUpload"
              accept="image/jpeg,image/png,image/gif,image/webp"
            >
              <el-avatar :size="80" :src="profileForm.avatarUrl || undefined" class="avatar-preview" :class="{'has-avatar':profileForm.avatarUrl}">
                <span v-if="!profileForm.avatarUrl">{{ userStore.userInfo?.username?.charAt(0).toUpperCase() }}</span>
              </el-avatar>
              <div class="avatar-overlay">点击上传</div>
            </el-upload>
          </div>

          <el-form :model="profileForm" label-width="80px" style="max-width: 480px; margin-top: 24px">
            <el-form-item label="用户名">
              <el-input :model-value="userStore.userInfo?.username" disabled />
            </el-form-item>
            <el-form-item label="邮箱">
              <el-input :model-value="userStore.userInfo?.email" disabled />
            </el-form-item>
            <el-form-item label="昵称">
              <el-input v-model="profileForm.nickName" placeholder="请输入昵称" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="profileLoading" @click="handleUpdateProfile">保存修改</el-button>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 修改密码 -->
        <el-tab-pane label="修改密码" name="password">
          <el-form :model="passwordForm" label-width="80px" style="max-width: 480px" :rules="passwordRules" ref="passwordFormRef">
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
import { ref, reactive, onMounted, computed } from 'vue'
import { ElMessage, type FormInstance, type FormRules, type UploadProps } from 'element-plus'
import { updateProfile, changePassword, type UpdateProfileModel, type ChangePasswordModel } from '@/api/auth'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()
const activeTab = ref('info')

const profileForm = reactive<UpdateProfileModel>({ nickName: '', avatarUrl: '' })
const profileLoading = ref(false)

const passwordFormRef = ref<FormInstance>()
const passwordForm = reactive<{ currentPassword: string; newPassword: string; confirmPassword: string }>({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})
const passwordLoading = ref(false)

// 上传头像需要携带 JWT Token
const uploadHeaders = computed(() => {
  return userStore.token ? { Authorization: `Bearer ${userStore.token}` } : {}
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

onMounted(async () => {
  await userStore.fetchUserInfo()
  if (userStore.userInfo) {
    profileForm.nickName = userStore.userInfo.nickName ?? ''
    profileForm.avatarUrl = userStore.userInfo.avatarUrl ?? ''
  }
})

const beforeAvatarUpload: UploadProps['beforeUpload'] = (rawFile) => {
  const allowed = import.meta.env.VITE_AVATAR_ALLOWED_TYPES.split(',')
  if (!allowed.includes(rawFile.type)) {
    const extensions = allowed.map((x:string) => x.split('/')[1].toUpperCase())
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
  if (response.code === 200 && response.data) {
    profileForm.avatarUrl = response.data
    if (userStore.userInfo) {
      userStore.userInfo.avatarUrl = response.data
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
    if (userStore.userInfo) {
      userStore.userInfo.nickName = profileForm.nickName
      userStore.userInfo.avatarUrl = profileForm.avatarUrl ?? ''
    }
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
</script>

<style scoped>
.profile-content {
  max-width: 640px;
  margin: 40px auto;
  padding: 0 20px;
}

.profile-card {
  border-radius: 16px;
}

.profile-card :deep(.el-card__body) {
  padding: 0 24px 24px;
}

.profile-card :deep(.el-tabs__header) {
  margin: 0;
  padding-top: 16px;
}

.profile-card :deep(.el-tab-pane) {
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
</style>
