<template>
  <div class="dashboard">
    <h2 class="page-title">仪表盘</h2>
    <div class="stats-grid">
      <el-card shadow="hover" class="stat-card">
        <div class="stat-content">
          <div class="stat-icon-wrap" style="background: #fff7e6">
            <el-icon class="stat-icon" color="#fa8c16"><Document /></el-icon>
          </div>
          <div>
            <div class="stat-value">{{ pendingCount }}</div>
            <div class="stat-label">待审核客户端</div>
          </div>
        </div>
        <el-button v-if="pendingCount > 0" type="warning" size="small" text @click="router.push('/clients?status=Pending')">
          去审核 →
        </el-button>
      </el-card>
      <el-card shadow="hover" class="stat-card">
        <div class="stat-content">
          <div class="stat-icon-wrap" style="background: #e6f7ff">
            <el-icon class="stat-icon" color="#1890ff"><Monitor /></el-icon>
          </div>
          <div>
            <div class="stat-value">{{ totalClients }}</div>
            <div class="stat-label">客户端总数</div>
          </div>
        </div>
      </el-card>
      <el-card shadow="hover" class="stat-card">
        <div class="stat-content">
          <div class="stat-icon-wrap" style="background: #f6ffed">
            <el-icon class="stat-icon" color="#52c41a"><CircleCheck /></el-icon>
          </div>
          <div>
            <div class="stat-value">{{ approvedCount }}</div>
            <div class="stat-label">已通过</div>
          </div>
        </div>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Document, Monitor, CircleCheck } from '@element-plus/icons-vue'
import { getPendingClients, getAllClients } from '@/api/client'

const router = useRouter()
const pendingCount = ref(0)
const totalClients = ref(0)
const approvedCount = ref(0)

onMounted(async () => {
  try {
    const pending = await getPendingClients()
    pendingCount.value = pending.length
  } catch {
    // 拦截器已处理
  }
  try {
    const all = await getAllClients()
    totalClients.value = all.length
    approvedCount.value = all.filter(c => c.status === 'Approved').length
  } catch {
    // 拦截器已处理
  }
})
</script>

<style scoped>
.page-title {
  font-size: 18px;
  font-weight: 600;
  color: #333;
  margin-bottom: 20px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}

.stat-card {
  border-radius: 8px;
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon-wrap {
  width: 48px;
  height: 48px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-icon {
  font-size: 24px;
}

.stat-value {
  font-size: 24px;
  font-weight: 700;
  color: #1a1a2e;
}

.stat-label {
  font-size: 13px;
  color: #999;
  margin-top: 2px;
}
</style>
