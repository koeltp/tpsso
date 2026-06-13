<template>
  <div class="logo-area">
    <img :src="logoSrc" alt="TPSSO" class="logo-img" />
  </div>
  <h1 class="title">设备授权</h1>

  <!-- 输入验证码阶段 -->
  <div v-if="step === 'input'" class="verify-area">
    <p class="desc">请在下方输入设备上显示的验证码</p>
    <el-input
      v-model="userCode"
      size="large"
      placeholder="请输入验证码"
      class="code-input"
      @keyup.enter="handleVerify"
    >
      <template #prefix>
        <el-icon><Key /></el-icon>
      </template>
    </el-input>
    <el-button type="primary" size="large" :loading="loading" class="verify-btn" @click="handleVerify">
      验 证
    </el-button>
  </div>

  <!-- 确认授权阶段 -->
  <div v-else-if="step === 'confirm'" class="confirm-area">
    <div class="verify-code-display">
      <span class="verify-label">验证码</span>
      <span class="verify-code">{{ userCode }}</span>
    </div>
    <p class="desc">确认授权此设备访问你的账号？</p>
    <div class="actions">
      <el-button size="large" @click="handleDeny">拒 绝</el-button>
      <el-button type="primary" size="large" :loading="loading" @click="handleApprove">同 意</el-button>
    </div>
  </div>

  <!-- 授权成功 -->
  <div v-else-if="step === 'success'" class="success-area">
    <el-icon :size="48" class="success-icon"><CircleCheck /></el-icon>
    <p class="desc success-text">授权成功！</p>
    <p class="sub-desc">请返回设备继续操作</p>
  </div>

  <!-- 授权失败 -->
  <div v-else-if="step === 'error'" class="error-area">
    <el-icon :size="48" class="error-icon"><CircleClose /></el-icon>
    <p class="desc error-text">授权失败</p>
    <p class="sub-desc">{{ errorMsg }}</p>
    <el-button type="primary" size="large" class="verify-btn" @click="resetAll">重新输入</el-button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { Key, CircleCheck, CircleClose } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import logoSrc from '@/assets/logo.png'

const route = useRoute()
const step = ref<'input' | 'confirm' | 'success' | 'error'>('input')
const userCode = ref('')
const loading = ref(false)
const errorMsg = ref('')

// 如果 URL 中有 user_code 参数，直接进入确认阶段
onMounted(() => {
  const code = route.query.user_code as string
  if (code) {
    userCode.value = code
    step.value = 'confirm'
  }
})

/** 提交验证码，进入确认阶段 */
const handleVerify = () => {
  const code = userCode.value.trim()
  if (!code) {
    ElMessage.warning('请输入验证码')
    return
  }
  step.value = 'confirm'
}

/** 同意授权：POST 表单到 /connect/verify */
const handleApprove = () => {
  loading.value = true

  const form = document.createElement('form')
  form.method = 'POST'
  form.action = '/connect/verify'

  const input = document.createElement('input')
  input.type = 'hidden'
  input.name = 'user_code'
  input.value = userCode.value.trim()
  form.appendChild(input)

  document.body.appendChild(form)
  form.submit()
}

/** 拒绝授权 */
const handleDeny = () => {
  step.value = 'input'
  userCode.value = ''
}

/** 重置 */
const resetAll = () => {
  step.value = 'input'
  userCode.value = ''
  errorMsg.value = ''
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
  margin-bottom: 20px;
  color: #333;
  font-size: 24px;
  font-weight: 600;
}

.desc {
  text-align: center;
  color: #666;
  font-size: 15px;
  line-height: 1.6;
  margin-bottom: 20px;
}

.verify-area {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.code-input {
  font-size: 18px;
  letter-spacing: 2px;
}

.code-input :deep(.el-input__inner) {
  text-align: center;
  text-transform: uppercase;
}

.verify-btn {
  width: 100%;
}

.confirm-area {
  text-align: center;
}

.verify-code-display {
  display: inline-flex;
  flex-direction: column;
  align-items: center;
  background: #f0f5ff;
  border-radius: 12px;
  padding: 12px 24px;
  margin-bottom: 16px;
}

.verify-label {
  font-size: 12px;
  color: #999;
  margin-bottom: 4px;
}

.verify-code {
  font-size: 24px;
  font-weight: 700;
  letter-spacing: 4px;
  color: #1890ff;
  text-transform: uppercase;
}

.actions {
  display: flex;
  gap: 12px;
  margin-top: 20px;
}

.actions .el-button {
  flex: 1;
}

.success-area,
.error-area {
  text-align: center;
}

.success-icon {
  color: #67c23a;
  margin-bottom: 12px;
}

.success-text {
  color: #67c23a;
  font-weight: 600;
  font-size: 18px;
}

.error-icon {
  color: #f56c6c;
  margin-bottom: 12px;
}

.error-text {
  color: #f56c6c;
  font-weight: 600;
  font-size: 18px;
}

.sub-desc {
  color: #999;
  font-size: 14px;
  margin-top: 8px;
}
</style>
