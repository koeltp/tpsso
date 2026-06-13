<template>
  <div class="dashboard-page">
    <el-card shadow="never">
      <template #header>
        <span class="page-title">仪表盘</span>
      </template>
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card">
          <template #header>
            <div class="stat-card-header">
              <el-icon color="#1890ff"><Monitor /></el-icon>
              <span>我的客户端</span>
            </div>
          </template>
          <div class="stat-body">
            <div class="stat-value">{{ clientStore.myClientCount }}</div>
            <el-button type="primary" text @click="router.push('/my-clients')">
              查看详情 →
            </el-button>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card">
          <template #header>
            <div class="stat-card-header">
              <el-icon color="#52c41a"><Key /></el-icon>
              <span>我的授权</span>
            </div>
          </template>
          <div class="stat-body">
            <div class="stat-value">{{ clientStore.myAuthorizationCount }}</div>
            <el-button type="primary" text @click="router.push('/my-apps')">
              查看详情 →
            </el-button>
          </div>
        </el-card>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Monitor, Key } from '@element-plus/icons-vue'
import { useClientStore } from '@/stores/client'

const router = useRouter()
const clientStore = useClientStore()

onMounted(() => {
  clientStore.fetchMyStats()
})
</script>

<style scoped>
.page-title {
  font-size: 18px;
  font-weight: 600;
  color: #333;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16px;
}

.stat-card {
  border-radius: 8px;
}

.stat-card-header {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: #333;
}

.stat-body {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.stat-value {
  font-size: 28px;
  font-weight: 700;
  color: #1a1a2e;
}
</style>
