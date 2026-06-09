<template>
  <div class="layout-page">
    <nav class="navbar">
      <div class="navbar-inner">
        <router-link to="/" class="navbar-brand">
          <img :src="logoSrc" alt="TPSSO" width="28" height="28" class="navbar-logo" />
          <span class="navbar-title">SSO</span>
        </router-link>
        <div class="navbar-right">
          <router-link to="/" class="nav-link">首页</router-link>
          <router-link v-if="userInfo" to="/my-clients" class="nav-link">我的客户端</router-link>
          <el-dropdown trigger="click" v-if="userInfo">
            <span class="user-dropdown">
              <el-avatar :size="32" :src="userInfo.avatarUrl || undefined" class="user-avatar" :class="{'has-avator':userInfo.avatarUrl}">{{ !userInfo.avatarUrl ? userInfo.username.charAt(0).toUpperCase() : '' }}</el-avatar>
              <span class="user-name">{{ userInfo.nickName || userInfo.username }}</span>
              <el-icon class="dropdown-icon"><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click="router.push('/profile')">
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

    <router-view />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ArrowDown, User, SwitchButton } from '@element-plus/icons-vue'
import { getUserInfo, logout as logoutApi, type UserInfoResult } from '@/api/auth'
import logoSrc from '@/assets/logo-icon.png'

const router = useRouter()
const userInfo = ref<UserInfoResult | null>(null)

onMounted(async () => {
  try {
    userInfo.value = await getUserInfo()
  } catch {
    userInfo.value = null
  }
})

const doLogout = async () => {
  try {
    await logoutApi()
  } catch {
    // 拦截器已处理
  }
  userInfo.value = null
  router.push('/')
}
</script>

<style scoped>
.layout-page {
  min-height: 100vh;
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

.nav-link.router-link-active {
  color: #409eff;
  font-weight: 500;
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
  
  color: white;
  font-weight: 600;
}

.user-avatar:not(.has-avator) {
  background: orange;
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
</style>
