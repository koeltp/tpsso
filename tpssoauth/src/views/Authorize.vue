<template>
  <div class="authorize-container">
    <!-- 应用信息头部：Logo + 名称/描述（无背景色） -->
    <div class="app-info-header">
      <div class="app-logo-box">
        <img :src="displayLogo" alt="TPSSO" class="app-logo-img" />
      </div>
      <div class="app-info-text">
        <h2 class="app-name">{{ appName }}</h2>
        <p v-if="appDesc" class="app-desc">{{ appDesc }}</p>
      </div>
    </div>

    <!-- 授权确认标题 -->
    <h1 class="title">授权确认</h1>

    <p class="desc">
      请求访问你的以下信息，你可以选择授权的范围：
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
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { User, Message, Lock } from '@element-plus/icons-vue'
import logoSrc from '@/assets/logo.png'

const route = useRoute()
const router = useRouter()

const appName = computed(() => (route.query.app_name as string) || '未知应用')
const appLogo = computed(() => (route.query.app_logo as string) || '')
const appDesc = computed(() => (route.query.app_desc as string) || '')
const loading = ref(false)

/** 显示的Logo：优先应用Logo，否则主站Logo */
const displayLogo = computed(() => {
  if (appLogo.value) {
    if (appLogo.value.startsWith('http')) return appLogo.value
    const base = import.meta.env.VITE_LOGO_URL || ''
    return base + appLogo.value
  }
  return logoSrc
})

const scopeMap: Record<string, { label: string; icon: typeof User; required?: boolean }> = {
  openid: { label: '你的身份标识', icon: User, required: true },
  profile: { label: '你的个人资料（姓名等）', icon: User },
  email: { label: '你的邮箱地址', icon: Message },
  roles: { label: '你的角色信息', icon: Lock },
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

const checkedScopes = ref(
  ((route.query.scope as string) || '').split(' ').filter(Boolean)
)

const handleApprove = () => {
  loading.value = true
  const form = document.createElement('form')
  form.method = 'POST'
  form.action = '/connect/authorize'

  const params = route.query
  for (const key of Object.keys(params)) {
    if (params[key] === undefined || key === 'app_name' || key === 'app_logo' || key === 'app_desc') continue
    const input = document.createElement('input')
    input.type = 'hidden'
    input.name = key
    input.value = key === 'scope' ? checkedScopes.value.join(' ') : (params[key] as string)
    form.appendChild(input)
  }

  document.body.appendChild(form)
  form.submit()
}

const handleDeny = () => {
  router.push('/login')
}
</script>

<style scoped>
.authorize-container {
  max-width: 500px;
  margin: 0 auto;
  padding: 24px 20px;
}

/* 头部区域：无背景色 */
.app-info-header {
  display: flex;
  align-items: center;
  gap: 24px;
  margin-bottom: 32px;
}

.app-logo-box {
  flex-shrink: 0;
}

/* Logo 基础样式 + 过渡动画 */
.app-logo-img {
  width: 80px;
  height: 80px;
  object-fit: contain;
  border-radius: 16px;
  background: white;
  padding: 6px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.08);
  transition: transform 0.25s ease, box-shadow 0.25s ease;
  cursor: pointer;
}

/* 鼠标悬停效果：轻微放大 + 阴影加深 */
.app-logo-img:hover {
  transform: scale(1.05);
  box-shadow: 0 6px 18px rgba(0,0,0,0.12);
}

.app-info-text {
  flex: 1;
  min-width: 0;
}

.app-name {
  font-size: 20px;
  font-weight: 600;
  margin: 0 0 6px 0;
  color: #1f2f3d;
}

.app-desc {
  font-size: 14px;
  color: #909399;
  margin: 0;
  line-height: 1.5;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.title {
  text-align: center;
  margin: 16px 0 8px;
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