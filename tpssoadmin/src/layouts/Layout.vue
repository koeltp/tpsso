<template>
  <div class="admin-layout">
    <!-- 顶部导航栏 -->
    <header class="top-bar">
      <div class="top-bar-left">
        <img :src="logoSrc" alt="TPSSO" width="24" height="24" />
        <span class="top-bar-title">TPSSO 管理后台</span>
        <el-icon class="collapse-btn" @click="toggleCollapse">
          <Fold v-if="!isCollapsed" />
          <Expand v-else />
        </el-icon>
      </div>
      <div class="top-bar-right">
        <el-dropdown trigger="click">
          <span class="user-dropdown">
            <el-avatar :size="28" :src="userStore.userInfo?.avatarUrl || undefined" class="user-avatar"
              :class="{ 'has-avatar': userStore.userInfo?.avatarUrl }">{{ !userStore.userInfo?.avatarUrl ?
                userStore.userInfo?.username?.charAt(0).toUpperCase() : '' }}</el-avatar>
            <span class="user-name">{{ userStore.userInfo?.username }}</span>
            <el-icon class="dropdown-icon">
              <ArrowDown />
            </el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="router.push('/profile')">
                <el-icon>
                  <User />
                </el-icon>个人中心
              </el-dropdown-item>
              <el-dropdown-item divided @click="doLogout">
                <el-icon>
                  <SwitchButton />
                </el-icon>退出登录
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </header>

    <div class="main-wrapper">
      <!-- 侧边栏 -->
      <aside class="sidebar" :class="{ collapsed: isCollapsed }">
        <div class="menu-list">
          <div
            v-for="item in menuItems"
            :key="item.name"
            :class="['menu-item', { active: activeMenu === item.path }]"
            @click="router.push(item.path)"
          >
            <el-icon class="menu-icon"><component :is="item.icon" /></el-icon>
            <span v-show="!isCollapsed">{{ item.name }}</span>
            <span
              v-if="item.badge && item.badge() > 0 && !isCollapsed"
              class="menu-badge-dot"
            />
          </div>
        </div>
      </aside>

      <!-- 内容区 -->
      <main class="content-area">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, markRaw, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { DataBoard, ArrowDown, SwitchButton, User, Fold, Expand, UserFilled, Setting, Grid } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import { useClientStore } from '@/stores/client'
import logoSrc from '@/assets/logo-icon.png'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const clientStore = useClientStore()
const isCollapsed = ref(false)

const activeMenu = computed(() => {
  if (route.path.startsWith('/admin/clients')) return '/admin/clients'
  if (route.path.startsWith('/admin/users')) return '/admin/users'
  if (route.path.startsWith('/admin/dict')) return '/admin/dict'
  return route.path
})

// 管理员菜单（此布局仅 Admin 使用）
const menuItems = computed(() => {
  return [
    { name: '仪表盘', path: '/admin/dashboard', icon: markRaw(DataBoard) },
    { name: '客户端管理', path: '/admin/clients', icon: markRaw(Grid), badge: () => clientStore.pendingCount },
    { name: '用户管理', path: '/admin/users', icon: markRaw(UserFilled) },
    { name: '配置管理', path: '/admin/dict', icon: markRaw(Setting) }
  ]
})

const toggleCollapse = () => {
  isCollapsed.value = !isCollapsed.value
}

onMounted(async () => {
  if (!userStore.userInfo) {
    await userStore.fetchUserInfo()
  }
  // Admin 用户加载待审核数量
  if (userStore.isAdmin) {
    clientStore.fetchPendingCount()
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

.collapse-btn {
  font-size: 18px;
  color: #666;
  cursor: pointer;
  padding: 4px;
  border-radius: 4px;
  transition: all 0.2s;
}

.collapse-btn:hover {
  color: #1890ff;
  background: #e6f7ff;
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

/* 主体区域 */
.main-wrapper {
  display: flex;
  flex: 1;
  min-height: 0;
}

/* 侧边栏 - 深色风格 */
.sidebar {
  width: 200px;
  background-color: #304156;
  flex-shrink: 0;
  transition: width 0.3s;
  overflow: hidden;
}

.sidebar.collapsed {
  width: 64px;
}

.menu-list {
  padding: 10px 0;
}

.menu-list .menu-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 20px;
  color: #bfcbd9;
  cursor: pointer;
  transition: all 0.3s;
  white-space: nowrap;
}

.sidebar.collapsed .menu-item {
  justify-content: center;
  padding: 12px;
}

.menu-list .menu-item:hover {
  background-color: #2a3f5f;
  color: #fff;
}

.menu-list .menu-item.active {
  background-color: #2a3f5f;
  color: #409eff;
  border-left: 3px solid #409eff;
}

.menu-icon {
  width: 18px;
  height: 18px;
  color: #bfcbd9;
  flex-shrink: 0;
}

.menu-item:hover .menu-icon,
.menu-item.active .menu-icon {
  color: #409eff;
}

.menu-badge-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #f56c6c;
  margin-left: 6px;
  flex-shrink: 0;
}

/* 内容区 */
.content-area {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
}
</style>
