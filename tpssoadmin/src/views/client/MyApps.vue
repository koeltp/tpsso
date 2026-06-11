<template>
  <div class="my-apps-page">
    <el-card shadow="never">
      <template #header>
        <span class="page-title">我的授权</span>
      </template>

      <el-alert type="info" :closable="false" show-icon style="margin-bottom: 16px">
        <template #title>此处展示您已授权的第三方应用。撤销授权后，应用将无法继续访问您的信息，直到您重新授权。</template>
      </el-alert>

      <el-table :data="authorizations" v-loading="loading" stripe>
        <el-table-column type="index" label="序号" width="70" align="center" />
        <el-table-column prop="clientName" label="应用名称" min-width="160" />
        <el-table-column label="授权范围" min-width="200">
          <template #default="{ row }">
            <el-tag v-for="scope in row.scopes" :key="scope" size="small" style="margin-right: 4px">{{ scope }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="授权时间" width="170">
          <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button link type="danger" size="small" @click="revokeAuth(row)">撤销</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-empty v-if="!loading && authorizations.length === 0" description="暂无授权记录" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getMyAuthorizations, revokeAuthorization, type AuthorizationResult } from '@/api/client'
import { formatDate } from '@/utils/client'

const loading = ref(false)
const authorizations = ref<AuthorizationResult[]>([])

const loadData = async () => {
  loading.value = true
  try {
    authorizations.value = await getMyAuthorizations() || []
  } catch {
    authorizations.value = []
  } finally {
    loading.value = false
  }
}

const revokeAuth = async (row: AuthorizationResult) => {
  try {
    await ElMessageBox.confirm(`确定撤销对「${row.clientName}」的授权？`, '确认', { type: 'warning' })
    await revokeAuthorization(row.id)
    ElMessage.success('已撤销授权')
    loadData()
  } catch {
    // 取消或拦截器处理
  }
}

onMounted(loadData)
</script>

<style scoped>
.my-apps-page { padding: 20px; }
.page-title { font-size: 16px; font-weight: 600; color: #333; }
</style>
