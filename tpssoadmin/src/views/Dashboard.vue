<template>
  <div class="dashboard">
    <h2 class="page-title">仪表盘</h2>

    <!-- Admin 视图 -->
    <template v-if="userStore.isAdmin">
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-icon-wrap" style="background: #fff7e6">
              <el-icon class="stat-icon" color="#fa8c16"><Document /></el-icon>
            </div>
            <div>
              <div class="stat-value">{{ clientStore.pendingCount }}</div>
              <div class="stat-label">待审核客户端</div>
            </div>
          </div>
          <el-button v-if="clientStore.pendingCount > 0" type="warning" text @click="router.push('/clients?status=Pending')">
            去审核 →
          </el-button>
        </el-card>
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-icon-wrap" style="background: #e6f7ff">
              <el-icon class="stat-icon" color="#1890ff"><Monitor /></el-icon>
            </div>
            <div>
              <div class="stat-value">{{ clientStore.totalClients }}</div>
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
              <div class="stat-value">{{ clientStore.approvedCount }}</div>
              <div class="stat-label">已通过</div>
            </div>
          </div>
        </el-card>
      </div>
    </template>

    <!-- 普通用户视图 -->
    <template v-else>
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-icon-wrap" style="background: #e6f7ff">
              <el-icon class="stat-icon" color="#1890ff"><Monitor /></el-icon>
            </div>
            <div>
              <div class="stat-value">{{ clientStore.myClientCount }}</div>
              <div class="stat-label">我的客户端</div>
            </div>
          </div>
          <el-button type="primary" text @click="router.push('/my-clients')">
            查看详情 →
          </el-button>
        </el-card>
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-icon-wrap" style="background: #f6ffed">
              <el-icon class="stat-icon" color="#52c41a"><Key /></el-icon>
            </div>
            <div>
              <div class="stat-value">{{ clientStore.myAuthorizationCount }}</div>
              <div class="stat-label">我的授权</div>
            </div>
          </div>
          <el-button type="primary" text @click="router.push('/my-apps')">
            查看详情 →
          </el-button>
        </el-card>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Document, Monitor, CircleCheck, Key } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import { useClientStore } from '@/stores/client'

const router = useRouter()
const userStore = useUserStore()
const clientStore = useClientStore()

onMounted(() => {
  if (userStore.isAdmin) {
    clientStore.fetchPendingCount()
    clientStore.fetchStats()
  } else {
    clientStore.fetchMyStats()
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

/* 普通用户只有2个卡片，用2列布局 */
.stats-grid:has(:nth-child(2):last-child) {
  grid-template-columns: repeat(2, 1fr);
}

.stat-card {
  border-radius: 8px;
}

.stat-card :deep(.el-card__body) {
  display: flex;
  align-items: center;
  justify-content: space-between;
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
