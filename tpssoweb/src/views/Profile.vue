<template>
  <div class="profile-page">
    <nav class="navbar">
      <div class="navbar-inner">
        <router-link to="/" class="navbar-brand">
          <svg width="28" height="28" viewBox="0 0 48 46" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M25.946 44.938c-.664.845-2.021.375-2.021-.698V33.937a2.26 2.26 0 0 0-2.262-2.262H10.287c-.92 0-1.456-1.04-.92-1.788l7.48-10.471c1.07-1.497 0-3.578-1.842-3.578H1.237c-.92 0-1.456-1.04-.92-1.788L10.013.474c.214-.297.556-.474.92-.474h28.894c.92 0 1.456 1.04.92 1.788l-7.48 10.471c-1.07 1.498 0 3.579 1.842 3.579h11.377c.943 0 1.473 1.088.89 1.83L25.947 44.94z" fill="#7c3aed"/>
          </svg>
          <span class="navbar-title">TPSSO</span>
        </router-link>
        <div class="navbar-right">
          <router-link to="/" class="nav-link">首页</router-link>
          <el-dropdown trigger="click" v-if="userInfo">
            <span class="user-dropdown">
              <el-avatar :size="32" class="user-avatar">{{ userInfo.username.charAt(0).toUpperCase() }}</el-avatar>
              <span class="user-name">{{ userInfo.username }}</span>
              <el-icon class="dropdown-icon"><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click="goProfile">
                  <el-icon><User /></el-icon>个人中心
                </el-dropdown-item>
                <el-dropdown-item divided @click="doLogout">
                  <el-icon><SwitchButton /></el-icon>退出
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
          <router-link v-else to="/login" class="nav-link">登录</router-link>
        </div>
      </div>
    </nav>

    <div class="profile-content">
      <el-card class="profile-card" shadow="never">
        <template #header>
          <div class="card-header">
            <h1>个人中心</h1>
            <p>管理您的个人信息和账户设置</p>
          </div>
        </template>

        <el-descriptions :column="1" border>
          <el-descriptions-item label="用户名" label-align="right" width="150px">
            {{ profile.username }}
          </el-descriptions-item>
          <el-descriptions-item label="邮箱" label-align="right">
            {{ profile.email }}
          </el-descriptions-item>
          <el-descriptions-item label="手机号" label-align="right">
            {{ profile.phone }}
          </el-descriptions-item>
          <el-descriptions-item label="注册时间" label-align="right">
            {{ profile.createdAt }}
          </el-descriptions-item>
          <el-descriptions-item label="角色" label-align="right">
            <el-tag type="primary">{{ profile.role }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="状态" label-align="right">
            <el-tag type="success">{{ profile.status }}</el-tag>
          </el-descriptions-item>
        </el-descriptions>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ArrowDown, User, SwitchButton } from '@element-plus/icons-vue'
import api from '@/utils/api'

const router = useRouter()

interface UserInfo {
  username: string
  email: string
  avatarUrl: string
}

const userInfo = ref<UserInfo | null>(null)

const profile = ref({
  username: 'admin',
  email: 'admin@tpsso.com',
  phone: '138****8888',
  createdAt: '2024-01-15',
  role: '管理员',
  status: '正常'
})

onMounted(async () => {
  try {
    const res = await api.get('/api/account/me')
    userInfo.value = res.data
    if (res.data.username) {
      profile.value.username = res.data.username
      profile.value.email = res.data.email || 'admin@tpsso.com'
    }
  } catch {
    userInfo.value = null
  }
})

const goProfile = () => {
  router.push('/profile')
}

const doLogout = async () => {
  try {
    await api.post('/api/account/logout')
  } catch {
    // ignore
  }
  userInfo.value = null
  router.push('/')
}
</script>

<style scoped>
.profile-page {
  min-height: 100vh;
  background: linear-gradient(135deg, #f0f5ff 0%, #e8f0fe 50%, #f5f0ff 100%);
}

.navbar {
  display: flex;
  align-items: center;
  height: 60px;
  border-bottom: 1px solid #f0f0f0;
  background: white;
  position: sticky;
  top: 0;
  z-index: 100;
}

.navbar-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 20px;
}

.navbar-brand {
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
}

.navbar-title {
  font-size: 18px;
  font-weight: 600;
  color: #333;
}

.navbar-right {
  display: flex;
  align-items: center;
  gap: 24px;
}

.nav-link {
  font-size: 15px;
  color: #666;
  text-decoration: none;
  transition: color 0.2s;
}

.nav-link:hover {
  color: #409eff;
}

.user-dropdown {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 8px;
  transition: background 0.2s;
}

.user-dropdown:hover {
  background: #f5f7fa;
}

.user-avatar {
  background: #409eff;
  color: white;
  font-weight: 600;
}

.user-name {
  font-size: 14px;
  color: #333;
  font-weight: 500;
}

.dropdown-icon {
  font-size: 12px;
  color: #999;
}

.profile-content {
  max-width: 640px;
  margin: 60px auto;
  padding: 0 20px;
}

.profile-card {
  border-radius: 16px;
}

.card-header h1 {
  font-size: 24px;
  font-weight: 700;
  color: #1a1a2e;
  margin: 0 0 4px;
}

.card-header p {
  font-size: 14px;
  color: #888;
  margin: 0;
}
</style>