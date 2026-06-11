<template>
  <div class="user-layout">
    <!-- 顶部导航栏 -->
    <header class="top-nav">
      <div class="top-nav-inner">
        <div class="nav-left">
          <router-link to="/dashboard" class="nav-brand">
            <img :src="logoSrc" alt="TPSSO" class="nav-logo" />
            <span class="nav-title">TPSSO</span>
          </router-link>
          <nav class="nav-menu">
            <router-link
              v-for="item in menuItems"
              :key="item.path"
              :to="item.path"
              :class="['nav-menu-item', { active: isActive(item.path) }]"
            >
              <el-icon><component :is="item.icon" /></el-icon>
              <span>{{ item.name }}</span>
            </router-link>
          </nav>
        </div>
        <div class="nav-right">
          <el-dropdown trigger="click">
            <span class="user-dropdown">
              <el-avatar :size="28" :src="userStore.userInfo?.avatarUrl || undefined" class="user-avatar"
                :class="{ 'has-avatar': userStore.userInfo?.avatarUrl }">
                {{ !userStore.userInfo?.avatarUrl ? userStore.userInfo?.username?.charAt(0).toUpperCase() : '' }}
              </el-avatar>
              <span class="user-name">{{ userStore.userInfo?.username }}</span>
              <el-icon class="dropdown-icon"><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click="router.push('/profile')">
                  <el-icon><User /></el-icon>个人中心
                </el-dropdown-item>
                <el-dropdown-item divided @click="doLogout">
                  <el-icon><SwitchButton /></el-icon>退出登录
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>
    </header>

    <!-- 内容区 -->
    <main class="user-content">
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { markRaw, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { DataBoard, Monitor, Key, ArrowDown, SwitchButton, User } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import logoSrc from '@/assets/logo-icon.png'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

const menuItems = [
  { name: '仪表盘', path: '/dashboard', icon: markRaw(DataBoard) },
  { name: '我的客户端', path: '/my-clients', icon: markRaw(Monitor) },
  { name: '我的授权', path: '/my-apps', icon: markRaw(Key) }
]

const isActive = (path: string) => {
  if (path === '/dashboard') return route.path === '/dashboard'
  return route.path.startsWith(path)
}

onMounted(async () => {
  if (!userStore.userInfo) {
    await userStore.fetchUserInfo()
  }
})

const doLogout = () => {
  userStore.logout()
}
</script>

<style scoped>
.user-layout {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: #f0f2f5;
}

/* 顶部导航栏 */
.top-nav {
  height: 56px;
  background: #fff;
  border-bottom: 1px solid #e8e8e8;
  flex-shrink: 0;
  z-index: 10;
}

.top-nav-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 100%;
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 24px;
}

.nav-left {
  display: flex;
  align-items: center;
  gap: 32px;
}

.nav-brand {
  display: flex;
  align-items: center;
  gap: 8px;
  text-decoration: none;
}

.nav-logo {
  height: 28px;
}

.nav-title {
  font-size: 16px;
  font-weight: 600;
  color: #1a1a2e;
}

.nav-menu {
  display: flex;
  align-items: center;
  gap: 4px;
}

.nav-menu-item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  font-size: 14px;
  color: #666;
  text-decoration: none;
  border-radius: 6px;
  transition: all 0.2s;
}

.nav-menu-item:hover {
  color: #409eff;
  background: #f0f5ff;
}

.nav-menu-item.active {
  color: #409eff;
  background: #ecf5ff;
  font-weight: 500;
}

.nav-right {
  display: flex;
  align-items: center;
}

.user-dropdown {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 6px;
  transition: background 0.2s;
}

.user-dropdown:hover {
  background: #f5f5f5;
}

.user-avatar {
  color: white;
  font-size: 13px;
  font-weight: 600;
}

.user-avatar:not(.has-avatar) {
  background: orange;
}

.user-name {
  font-size: 14px;
  color: #333;
}

.dropdown-icon {
  font-size: 12px;
  color: #999;
}

/* 内容区 */
.user-content {
  flex: 1;
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
  padding: 24px;
  box-sizing: border-box;
}
</style>
