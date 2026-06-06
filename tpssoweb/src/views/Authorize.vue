<template>
  <div class="consent-container">
    <div class="bubble bubble-1"></div>
    <div class="bubble bubble-2"></div>
    <div class="bubble bubble-3"></div>

    <div class="consent-box">
      <div class="logo-area">
        <img :src="logoSrc" alt="TPSSO" class="logo-img" />
      </div>
      <h1 class="title">授权确认</h1>
      <p class="desc">
        <strong>{{ appName }}</strong> <br/>请求访问你的以下信息，你可以选择授权的范围：
      </p>

      <div class="scope-list">
        <el-checkbox-group v-model="checkedScopes">
          <div v-for="item in scopeItems" :key="item.name" class="scope-item">
            <el-checkbox :value="item.name" :disabled="item.required">
              <div class="scope-label">
                <el-icon :size="16" class="scope-icon"><component :is="item.icon" /></el-icon>
                <span>{{ item.label }}</span>
                <el-tag v-if="item.required" size="small" type="info">必须</el-tag>
              </div>
            </el-checkbox>
          </div>
        </el-checkbox-group>
      </div>

      <div class="actions">
        <el-button size="large" @click="handleDeny">拒 绝</el-button>
        <el-button type="primary" size="large" :loading="loading" @click="handleApprove">同 意</el-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { User, Message, Lock } from '@element-plus/icons-vue'
import logoSrc from '@/assets/logo.png'

const route = useRoute()
const router = useRouter()

const appName = computed(() => (route.query.app_name as string) || '未知应用')
const loading = ref(false)

// scope 名称到中文标签的映射
const scopeMap: Record<string, { label: string; icon: typeof User; required?: boolean }> = {
  openid: { label: '你的身份标识', icon: User, required: true },
  profile: { label: '你的个人资料（姓名等）', icon: User },
  email: { label: '你的邮箱地址', icon: Message },
  offline_access: { label: '离线访问（刷新令牌）', icon: Lock }
}

const scopeItems = computed(() => {
  const scopes = ((route.query.scope as string) || '').split(' ').filter(Boolean)
  return scopes.map(s => {
    const mapped = scopeMap[s]
    return mapped
      ? { name: s, label: mapped.label, icon: mapped.icon, required: !!mapped.required }
      : { name: s, label: s, icon: Lock, required: false }
  })
})

// 默认全部勾选
const checkedScopes = ref(
  ((route.query.scope as string) || '').split(' ').filter(Boolean)
)

// 同意授权：用用户选中的 scope 构造表单提交
const handleApprove = () => {
  loading.value = true
  const form = document.createElement('form')
  form.method = 'POST'
  form.action = '/connect/authorize'

  // 提交原始参数，但 scope 使用用户勾选的值
  const params = route.query
  for (const key of Object.keys(params)) {
    if (params[key] === undefined || key === 'app_name') continue
    const input = document.createElement('input')
    input.type = 'hidden'
    input.name = key
    // scope 用用户勾选的值替换
    input.value = key === 'scope' ? checkedScopes.value.join(' ') : (params[key] as string)
    form.appendChild(input)
  }

  document.body.appendChild(form)
  form.submit()
}

// 拒绝授权：返回首页
const handleDeny = () => {
  router.push('/')
}
</script>

<style scoped>
.consent-container {
  width: 100%;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f0f5ff 0%, #e8f0fe 50%, #f5f0ff 100%);
  position: relative;
  overflow: hidden;
}

.bubble {
  position: absolute;
  border-radius: 50%;
  z-index: 0;
  animation: float 6s ease-in-out infinite;
}

.bubble-1 {
  width: 80px;
  height: 80px;
  top: 15%;
  left: 8%;
  background: radial-gradient(circle at 30% 30%, rgba(124,58,237,0.12), rgba(124,58,237,0.04));
  animation-delay: 0s;
}

.bubble-2 {
  width: 120px;
  height: 120px;
  top: 60%;
  right: 8%;
  background: radial-gradient(circle at 30% 30%, rgba(124,58,237,0.1), rgba(124,58,237,0.03));
  animation-delay: 1s;
  animation-duration: 8s;
}

.bubble-3 {
  width: 60px;
  height: 60px;
  top: 25%;
  right: 15%;
  background: radial-gradient(circle at 30% 30%, rgba(103,194,58,0.1), rgba(103,194,58,0.03));
  animation-delay: 2s;
  animation-duration: 7s;
}

@keyframes float {
  0%, 100% { transform: translateY(0) scale(1); }
  50% { transform: translateY(-20px) scale(1.05); }
}

.consent-box {
  position: relative;
  width: 420px;
  margin: 0 auto;
  padding: 40px;
  background: white;
  border-radius: 16px;
  box-shadow: 0 8px 40px rgba(0, 0, 0, 0.08);
  border: 1px solid #f0f2f5;
  z-index: 1;
}

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

.scope-list {
  background: #f8f9fc;
  border-radius: 12px;
  padding: 16px 20px;
  margin-bottom: 28px;
}

.scope-item {
  padding: 6px 0;
}

.scope-item + .scope-item {
  border-top: 1px solid #eee;
}

.scope-label {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  color: #444;
  font-size: 14px;
}

.scope-icon {
  color: #7c3aed;
  flex-shrink: 0;
}

.actions {
  display: flex;
  gap: 12px;
}

.actions .el-button {
  flex: 1;
}
</style>
