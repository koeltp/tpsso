<template>
  <div class="admin-layout">
    <!-- 顶部导航栏 -->
    <header class="top-bar">
      <div class="top-bar-left">
        <img :src="logoSrc" alt="TPSSO" width="24" height="24" />
        <span class="top-bar-title">TPSSO 管理后台</span>
      </div>
      <div class="top-bar-right">
        <el-dropdown trigger="click">
          <span class="user-dropdown">
            <el-avatar :size="28" :src="userStore.userInfo?.avatarUrl || undefined" class="user-avatar">{{ !userStore.userInfo?.avatarUrl ? userStore.userInfo?.username?.charAt(0).toUpperCase() : '' }}</el-avatar>
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
    </header>

    <div class="main-wrapper">
      <!-- 侧边栏 -->
      <aside class="sidebar">
        <el-menu :default-active="activeMenu" router class="sidebar-menu">
          <el-menu-item index="/">
            <el-icon><DataBoard /></el-icon>
            <span>仪表盘</span>
          </el-menu-item>
          <el-menu-item index="/clients">
            <el-icon><Monitor /></el-icon>
            <span>客户端管理</span>
            <el-badge v-if="pendingCount > 0" :value="pendingCount" class="menu-badge" />
          </el-menu-item>
        </el-menu>
      </aside>

      <!-- 内容区 -->
      <main class="content-area">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { DataBoard, Monitor, ArrowDown, SwitchButton, User } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import { getPendingClients } from '@/api/client'
import logoSrc from '@/assets/logo-icon.png'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

const pendingCount = ref(0)

const activeMenu = computed(() => {
  if (route.path === '/clients') return '/clients'
  return route.path
})

onMounted(async () => {
  await userStore.fetchUserInfo()
  try {
    const pending = await getPendingClients()
    pendingCount.value = pending.length
  } catch {
    // 拦截器已处理
  }
})

const doLogout = () => {
  userStore.logout()
}
</script>

<style scoped>
.admin-layout {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: #f0f2f5;
}

/* 顶部导航栏 */
.top-bar {
  height: 48px;
  background: #fff;
  border-bottom: 1px solid #e8e8e8;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  flex-shrink: 0;
  z-index: 10;
}

.top-bar-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.top-bar-title {
  font-size: 15px;
  font-weight: 600;
  color: #1a1a2e;
}

.top-bar-right {
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
  background: #1890ff;
  color: white;
  font-size: 13px;
  font-weight: 600;
}

.user-name {
  font-size: 14px;
  color: #333;
}

.dropdown-icon {
  font-size: 12px;
  color: #999;
}

/* 主体区域 */
.main-wrapper {
  display: flex;
  flex: 1;
  min-height: 0;
}

/* 侧边栏 - Casdoor 浅色风格 */
.sidebar {
  width: 200px;
  background: #fff;
  border-right: 1px solid #e8e8e8;
  flex-shrink: 0;
  padding-top: 8px;
}

.sidebar-menu {
  border-right: none;
}

.sidebar-menu .el-menu-item {
  height: 44px;
  line-height: 44px;
  color: #666;
  font-size: 14px;
  margin: 2px 8px;
  border-radius: 6px;
}

.sidebar-menu .el-menu-item:hover {
  color: #1890ff;
  background: #e6f7ff;
}

.sidebar-menu .el-menu-item.is-active {
  color: #1890ff;
  background: #e6f7ff;
  font-weight: 500;
}

.menu-badge {
  margin-left: auto;
}

/* 内容区 */
.content-area {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
}
</style>
