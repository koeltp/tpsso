<template>
  <div class="home-container">
    <nav class="navbar">
      <div class="navbar-inner">
        <router-link to="/" class="navbar-brand">
          <img :src="logoSrc" alt="TPSSO" class="navbar-logo" />
          <span class="navbar-title">TPSSO</span>
        </router-link>
        <div class="navbar-right">
          <template v-if="userStore.isAuthenticated">
            <span class="nav-user">{{ userStore.userInfo?.userName }}</span>
            <router-link :to="userStore.isAdmin ? '/admin/dashboard' : '/dashboard'" class="nav-link">进入控制台</router-link>
            <a class="nav-link" @click="handleLogout">退出</a>
          </template>
          <template v-else>
            <a class="nav-link" @click="handleLogin">登录</a>
            <router-link to="/register" class="nav-link">注册</router-link>
          </template>
        </div>
      </div>
    </nav>

    <div class="hero-section">
      <div class="hero-content">
        <h1 class="hero-title">TPSSO 统一身份认证</h1>
        <p class="hero-subtitle">安全、可靠的统一身份认证与授权平台</p>
      </div>
      <div class="hero-features">
        <div class="feature-item">
          <el-icon class="feature-icon"><Lock /></el-icon>
          <h3>安全认证</h3>
          <p>基于 OAuth 2.0 / OpenID Connect 标准协议</p>
        </div>
        <div class="feature-item">
          <el-icon class="feature-icon"><Connection /></el-icon>
          <h3>统一登录</h3>
          <p>一次登录，访问所有接入应用</p>
        </div>
        <div class="feature-item">
          <el-icon class="feature-icon"><Key /></el-icon>
          <h3>授权管理</h3>
          <p>自主管理已授权的第三方应用</p>
        </div>
      </div>
    </div>

    <div class="info-section">
      <el-row :gutter="30">
        <el-col :span="12">
          <el-card shadow="hover" class="info-card">
            <template #header>
              <div class="card-header">
                <el-icon><User /></el-icon>
                <span>用户功能</span>
              </div>
            </template>
            <ul class="feature-list">
              <li>统一登录，访问所有应用</li>
              <li>管理已授权的第三方应用</li>
              <li>查看个人信息与安全设置</li>
              <li>一键撤销应用授权</li>
            </ul>
          </el-card>
        </el-col>
        <el-col :span="12">
          <el-card shadow="hover" class="info-card">
            <template #header>
              <div class="card-header">
                <el-icon><Setting /></el-icon>
                <span>开发者功能</span>
              </div>
            </template>
            <ul class="feature-list">
              <li>注册 OAuth 2.0 客户端</li>
              <li>获取 Client ID / Secret</li>
              <li>配置回调地址和权限范围</li>
              <li>支持 Authorization Code + PKCE</li>
            </ul>
          </el-card>
        </el-col>
      </el-row>
    </div>

    <div class="footer-section">
      <p>&copy; 2026 TPSSO 统一身份认证平台</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Lock, Connection, Key, User, Setting } from '@element-plus/icons-vue'
import { startOAuthLogin } from '@/utils/oauth'
import { useUserStore } from '@/stores/user'
import logoSrc from '@/assets/logo-icon.png'

const userStore = useUserStore()

const handleLogin = () => {
  startOAuthLogin()
}

const handleLogout = () => {
  userStore.logout()
  window.location.href = '/'
}
</script>

<style scoped>
.home-container {
  min-height: 100vh;
  background: white;
}

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

.nav-user {
  font-size: 14px;
  color: #333;
  font-weight: 500;
}

.hero-section {
  padding: 100px 20px 80px;
  text-align: center;
  background: linear-gradient(135deg, #f0f5ff 0%, #e8f0fe 50%, #f5f0ff 100%);
  position: relative;
  overflow: hidden;
}

.hero-section::before {
  content: '';
  position: absolute;
  top: -200px;
  right: -200px;
  width: 500px;
  height: 500px;
  background: radial-gradient(circle, rgba(64,158,255,0.08) 0%, transparent 70%);
  border-radius: 50%;
}

.hero-section::after {
  content: '';
  position: absolute;
  bottom: -150px;
  left: -150px;
  width: 400px;
  height: 400px;
  background: radial-gradient(circle, rgba(103,194,58,0.06) 0%, transparent 70%);
  border-radius: 50%;
}

.hero-content {
  max-width: 800px;
  margin: 0 auto 60px;
  position: relative;
  z-index: 1;
}

.hero-title {
  font-size: 48px;
  font-weight: 800;
  margin-bottom: 16px;
  background: linear-gradient(135deg, #2b5fd9 0%, #7c3aed 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -1px;
}

.hero-subtitle {
  font-size: 20px;
  color: #666;
  font-weight: 400;
}

.hero-features {
  display: flex;
  gap: 24px;
  justify-content: center;
  max-width: 1200px;
  margin: 0 auto;
  position: relative;
  z-index: 1;
}

.feature-item {
  flex: 1;
  padding: 36px 28px;
  background: white;
  border-radius: 16px;
  border: 1px solid #eef2f6;
  transition: all 0.3s ease;
}

.feature-item:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.08);
  border-color: transparent;
}

.feature-icon {
  font-size: 40px;
  margin-bottom: 16px;
  color: #409eff;
}

.feature-item h3 {
  font-size: 18px;
  margin-bottom: 8px;
  color: #1a1a2e;
}

.feature-item p {
  font-size: 14px;
  color: #888;
  line-height: 1.6;
}

.info-section {
  max-width: 1200px;
  margin: 0 auto;
  padding: 60px 20px;
}

.info-card {
  height: 100%;
}

.card-header {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 18px;
  font-weight: 600;
}

.card-header .el-icon {
  font-size: 24px;
  color: #409eff;
}

.feature-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.feature-list li {
  padding: 10px 0;
  padding-left: 20px;
  position: relative;
  color: #666;
  border-bottom: 1px solid #f0f0f0;
}

.feature-list li:last-child {
  border-bottom: none;
}

.feature-list li::before {
  content: '✓';
  position: absolute;
  left: 0;
  color: #67c23a;
  font-weight: bold;
}

.footer-section {
  text-align: center;
  padding: 40px 20px;
  color: #999;
  font-size: 14px;
  border-top: 1px solid #f0f0f0;
}
</style>
