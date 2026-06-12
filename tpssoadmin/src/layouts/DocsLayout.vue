<template>
  <div class="docs-layout">
    <nav class="navbar">
      <div class="navbar-inner">
        <router-link to="/" class="navbar-brand">
          <img :src="logoSrc" alt="TPSSO" class="navbar-logo" />
          <span class="navbar-title">TPSSO</span>
        </router-link>
        <div class="navbar-right">
          <router-link to="/docs" class="nav-link" :class="{ active: isDocsPage }">文档</router-link>
          <template v-if="userStore.isAuthenticated">
            <span class="nav-user">{{ userStore.userInfo?.nickName }}</span>
            <router-link :to="userStore.isAdmin ? '/admin/dashboard' : '/dashboard'" class="nav-link">进入控制台</router-link>
            <a class="nav-link" @click="handleLogout">退出</a>
          </template>
          <template v-else>
            <a class="nav-link" @click="handleLogin">登录</a>
            <a class="nav-link" @click="handleRegister">注册</a>
          </template>
        </div>
      </div>
    </nav>
    <div class="docs-body">
      <aside class="docs-sidebar">
        <el-menu :default-active="activeMenu" router class="docs-menu">
          <el-menu-item index="/docs/quick-start">
            <el-icon><Flag /></el-icon>
            <span>快速开始</span>
          </el-menu-item>
          <el-menu-item index="/docs/architecture">
            <el-icon><Grid /></el-icon>
            <span>架构设计</span>
          </el-menu-item>
          <el-menu-item index="/docs/deployment">
            <el-icon><Upload /></el-icon>
            <span>部署运维</span>
          </el-menu-item>
          <el-menu-item index="/docs/conventions">
            <el-icon><Document /></el-icon>
            <span>开发规范</span>
          </el-menu-item>
          <el-menu-item index="/docs/api">
            <el-icon><Connection /></el-icon>
            <span>API 参考</span>
          </el-menu-item>
        </el-menu>
      </aside>
      <main class="docs-content">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { Flag, Grid, Upload, Document, Connection } from '@element-plus/icons-vue'
import { startOAuthLogin } from '@/utils/oauth'
import { useUserStore } from '@/stores/user'
import logoSrc from '@/assets/logo-icon.png'

const route = useRoute()
const userStore = useUserStore()
const activeMenu = computed(() => route.path)
const isDocsPage = computed(() => route.path.startsWith('/docs'))

const handleLogin = () => {
  startOAuthLogin()
}

const handleRegister = () => {
  const ssoUrl = import.meta.env.VITE_SSO_URL || 'http://localhost:3010'
  window.location.href = ssoUrl + '/register'
}

const handleLogout = () => {
  userStore.logout()
  window.location.href = '/'
}
</script>

<style scoped>
.docs-layout {
  min-height: 100vh;
  background: #fff;
}

/* 与 Home.vue 保持一致的导航栏样式 */
.navbar {
  display: flex;
  align-items: center;
  height: 60px;
  border-bottom: 1px solid #f0f0f0;
  position: sticky;
  top: 0;
  background: white;
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

.navbar-logo {
  height: 32px;
}

.navbar-title {
  font-size: 18px;
  font-weight: 600;
  color: #333;
}

.navbar-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.nav-link {
  font-size: 14px;
  text-decoration: none;
  color: #606266;
  cursor: pointer;
  transition: color 0.2s;
}

.nav-link:hover {
  color: #409eff;
}

.nav-link.active {
  color: #409eff;
  font-weight: 500;
}

.nav-user {
  font-size: 14px;
  color: #333;
  font-weight: 500;
}

.docs-body {
  display: flex;
  max-width: 1200px;
  margin: 0 auto;
  min-height: calc(100vh - 60px);
  padding: 0 20px;
}

.docs-sidebar {
  width: 220px;
  border-right: 1px solid #f0f0f0;
  padding: 16px 0;
  flex-shrink: 0;
}

.docs-menu {
  border-right: none;
}

.docs-menu .el-menu-item {
  height: 44px;
  line-height: 44px;
  font-size: 14px;
}

.docs-content {
  flex: 1;
  padding: 32px 48px;
  max-width: 960px;
  min-width: 0;
}
</style>

<!-- 文档页面通用样式（非 scoped，子页面继承） -->
<style>
.doc-page h1 {
  font-size: 28px;
  font-weight: 700;
  color: #1a1a2e;
  margin-bottom: 8px;
}

.doc-page .doc-desc {
  font-size: 16px;
  color: #888;
  margin-bottom: 32px;
}

.doc-page h2 {
  font-size: 20px;
  font-weight: 600;
  color: #1a1a2e;
  margin-top: 36px;
  margin-bottom: 16px;
  padding-bottom: 8px;
  border-bottom: 1px solid #f0f0f0;
}

.doc-page h3 {
  font-size: 16px;
  font-weight: 600;
  color: #333;
  margin-top: 24px;
  margin-bottom: 12px;
}

.doc-page p {
  font-size: 14px;
  color: #555;
  line-height: 1.8;
  margin-bottom: 12px;
}

.doc-page code {
  background: #f5f7fa;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 13px;
  color: #c7254e;
}

.doc-page .code-card {
  margin-bottom: 16px;
}

.doc-page .code-card pre {
  font-family: 'Cascadia Code', 'Fira Code', Consolas, monospace;
  font-size: 13px;
  line-height: 1.6;
  color: #333;
  white-space: pre;
  overflow-x: auto;
  margin: 0;
}

.doc-page .el-table {
  margin-bottom: 16px;
}

.doc-page .el-steps {
  margin-bottom: 16px;
}

.doc-page .el-tag {
  margin-right: 4px;
}
</style>
