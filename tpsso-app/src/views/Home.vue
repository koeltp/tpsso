<template>
  <div class="home-container">
    <div class="home-card">
      <div class="avatar-area">
        <el-avatar :size="72" :icon="UserFilled" />
      </div>
      <h1 class="greeting">欢迎回来，{{ displayName }}！</h1>

      <div class="profile-section">
        <h2 class="section-title">授权信息</h2>
        <el-descriptions :column="1" border size="large">
          <el-descriptions-item label="用户 ID">{{ userInfo.sub }}</el-descriptions-item>
          <el-descriptions-item v-if="userInfo.name" label="用户名">
            {{ userInfo.name }}
          </el-descriptions-item>
          <el-descriptions-item v-if="userInfo.email" label="邮箱">
            {{ userInfo.email }}
          </el-descriptions-item>
        </el-descriptions>
      </div>

      <div v-if="tokenInfo" class="profile-section">
        <h2 class="section-title">令牌信息</h2>
        <el-descriptions :column="1" border size="large">
          <el-descriptions-item label="授权范围">
            <el-tag v-for="s in tokenInfo.scopes" :key="s" size="small" style="margin: 2px 4px 2px 0">
              {{ s }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="Access Token">
            <el-tooltip :content="tokenInfo.accessToken" placement="top-start">
              <span class="token-text">{{ tokenInfo.accessToken }}</span>
            </el-tooltip>
          </el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="actions">
        <el-button type="danger" @click="logout">退出登录</el-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { UserFilled } from '@element-plus/icons-vue'
import { userManager } from '@/auth/oidcConfig'

const displayName = ref('')
const userInfo = ref<Record<string, string>>({})
const tokenInfo = ref<{ scopes: string[]; accessToken: string } | null>(null)

onMounted(async () => {
  const user = await userManager.getUser()
  if (user) {
    // 从 id_token 的 profile 中解析用户信息
    const profile = user.profile ?? {}
    displayName.value = profile.name || profile.sub || ''
    userInfo.value = {
      sub: profile.sub ?? '',
      name: profile.name ?? '',
      email: profile.email ?? ''
    }
    tokenInfo.value = {
      scopes: user.scopes ?? [],
      accessToken: user.access_token ?? ''
    }
  }
})

const logout = async () => {
  const user = await userManager.getUser()
  await userManager.signoutRedirect({ id_token_hint: user?.id_token })
}
</script>

<style scoped>
.home-container {
  width: 100%;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f0f5ff 0%, #e8f0fe 50%, #f5f0ff 100%);
}

.home-card {
  width: 520px;
  padding: 40px;
  background: white;
  border-radius: 16px;
  box-shadow: 0 8px 40px rgba(0, 0, 0, 0.08);
  border: 1px solid #f0f2f5;
}

.avatar-area {
  text-align: center;
  margin-bottom: 12px;
}

.greeting {
  text-align: center;
  margin-bottom: 28px;
  color: #333;
  font-size: 22px;
  font-weight: 600;
}

.profile-section {
  margin-bottom: 24px;
}

.section-title {
  font-size: 16px;
  font-weight: 600;
  color: #555;
  margin-bottom: 12px;
}

.token-text {
  display: inline-block;
  max-width: 300px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-family: monospace;
  font-size: 12px;
  color: #999;
  vertical-align: middle;
}

.actions {
  text-align: center;
  margin-top: 8px;
}
</style>
