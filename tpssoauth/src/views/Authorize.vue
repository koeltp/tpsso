<template>
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

/** 同意授权：提交表单到 /connect/authorize */
const handleApprove = () => {
  loading.value = true
  const form = document.createElement('form')
  form.method = 'POST'
  form.action = '/connect/authorize'

  const params = route.query
  for (const key of Object.keys(params)) {
    if (params[key] === undefined || key === 'app_name') continue
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
