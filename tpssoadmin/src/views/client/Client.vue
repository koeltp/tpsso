<template>
  <div class="clients-page">
    <el-card class="page-card">
      <template #header>
        <div class="card-header">
          <span class="page-title">客户端管理</span>
        </div>
      </template>

      <!-- 搜索区域 -->
      <div class="search-area">
        <el-form :inline="true" :model="searchForm" class="search-form">
          <el-form-item label="关键字">
            <el-input
              v-model="searchForm.keyword"
              placeholder="名称或 Client ID"
              clearable
              style="width: 200px"
              @clear="handleSearch"
              @keyup.enter="handleSearch"
            >
              <template #prefix>
                <el-icon><Search /></el-icon>
              </template>
            </el-input>
          </el-form-item>
          <el-form-item label="状态">
            <el-radio-group v-model="searchForm.status" @change="handleSearch">
              <el-radio-button value="">全部</el-radio-button>
              <el-radio-button value="Draft">草稿</el-radio-button>
              <el-radio-button value="Pending">待审核</el-radio-button>
              <el-radio-button value="Approved">已通过</el-radio-button>
              <el-radio-button value="Rejected">已拒绝</el-radio-button>
            </el-radio-group>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleSearch">
              <el-icon><Search /></el-icon>
              搜索
            </el-button>
            <el-button @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
      </div>

      <!-- 表格区域 -->
      <el-table :data="clients" v-loading="loading" stripe>
        <el-table-column type="index" label="序号" width="70" align="center" />
        <el-table-column label="Logo" width="70" align="center">
          <template #default="{ row }">
            <el-avatar v-if="row.logo" :src="getFullUrl(row.logo)" :size="32" shape="square" />
            <el-avatar v-else :size="32" shape="square" class="logo-placeholder">{{ row.name?.charAt(0) || '?' }}</el-avatar>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="应用名称" min-width="140" />
        <el-table-column prop="clientId" label="Client ID" min-width="180">
          <template #default="{ row }">
            <code>{{ row.clientId }}</code>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="应用描述" show-overflow-tooltip />
        <el-table-column prop="reviewRemark" label="审核意见" show-overflow-tooltip />
        <el-table-column label="类型" width="90" align="center">
          <template #default="{ row }">
            {{ row.isPublic ? '公开' : '机密' }}
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="statusTagType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="创建时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="280" align="left" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleDetail(row)">详情</el-button>
            <el-button v-if="row.status === 'Pending'" type="success" link size="small" @click="handleApprove(row.id)">通过</el-button>
            <el-button v-if="row.status === 'Pending'" type="danger" link size="small" @click="handleReject(row)">拒绝</el-button>
            <el-button v-if="row.status !== 'Approved'" type="danger" link size="small" @click="handleDelete(row.id)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页区域 -->
      <div class="pagination-area">
        <el-pagination
          v-model:current-page="pageIndex"
          v-model:page-size="pageSize"
          :total="totalCount"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next, jumper"
          background
          @size-change="fetchClients"
          @current-change="fetchClients"
        />
      </div>
    </el-card>

    <!-- 详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="客户端详情" width="600px">
      <template v-if="detail">
        <el-descriptions :column="1" border>
          <el-descriptions-item label="Logo">
            <el-avatar v-if="detail.logo" :src="getFullUrl(detail.logo)" :size="48" shape="square" />
            <span v-else class="text-muted">未设置</span>
          </el-descriptions-item>
          <el-descriptions-item label="应用名称">{{ detail.name }}</el-descriptions-item>
          <el-descriptions-item label="Client ID">
            <code>{{ detail.clientId }}</code>
          </el-descriptions-item>
          <el-descriptions-item label="客户端类型">{{ detail.isPublic ? '公开（SPA/移动端）' : '机密（服务端）' }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="statusTagType(detail.status)" size="small">{{ statusLabel(detail.status) }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item v-if="detail.description" label="描述">{{ detail.description }}</el-descriptions-item>
          <el-descriptions-item label="回调地址">
            <div v-for="uri in detail.redirectUris.split('\n')" :key="uri" class="redirect-uri">{{ uri }}</div>
          </el-descriptions-item>
          <el-descriptions-item label="授权范围">{{ detail.allowedScopes }}</el-descriptions-item>
          <el-descriptions-item label="授权类型">
            <el-tag v-for="gt in (detail.grantTypes || '').split(' ').filter(Boolean)" :key="gt" size="small" class="grant-type-tag">{{ grantTypeLabel(gt) }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="确认类型">{{ detail.consentType === 'implicit' ? '自动确认' : '需用户确认' }}</el-descriptions-item>
          <el-descriptions-item v-if="detail.reviewRemark" label="审核备注">{{ detail.reviewRemark }}</el-descriptions-item>
          <el-descriptions-item label="创建时间">{{ formatDate(detail.createdAt) }}</el-descriptions-item>
          <el-descriptions-item v-if="detail.updatedAt" label="更新时间">{{ formatDate(detail.updatedAt) }}</el-descriptions-item>
        </el-descriptions>
      </template>
    </el-dialog>

    <!-- 拒绝对话框 -->
    <el-dialog v-model="rejectDialogVisible" title="拒绝审核" width="450px">
      <el-form ref="rejectFormRef" :model="rejectForm" :rules="rejectRules" label-width="80px">
        <el-form-item label="原因" prop="reason">
          <el-input v-model="rejectForm.reason" type="textarea" :rows="4" placeholder="请输入拒绝原因" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="rejectDialogVisible = false">取消</el-button>
        <el-button type="danger" :loading="rejectLoading" @click="confirmReject">确认拒绝</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { Search } from '@element-plus/icons-vue'
import {
  approveClient, rejectClient, deleteClient, getClientById,
  type ClientResult
} from '@/api/client'
import { statusTagType, statusLabel, formatDate } from '@/utils/client'
import { useClientStore } from '@/stores/client'

/** 授权类型中文标签 */
const grantTypeLabel = (gt: string) => {
  const map: Record<string, string> = {
    authorization_code: '授权码模式',
    refresh_token: '刷新令牌',
    client_credentials: '客户端凭证',
    device_code: '设备码'
  }
  return map[gt] || gt
}

const route = useRoute()
const clientStore = useClientStore()
const loading = ref(false)
const clients = ref<ClientResult[]>([])
const pageIndex = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const searchForm = reactive({
  keyword: '',
  status: (route.query.status as string) || ''
})

/** 搜索时重置到第一页 */
const handleSearch = () => {
  pageIndex.value = 1
  fetchClients()
}

/** 重置搜索条件 */
const handleReset = () => {
  searchForm.keyword = ''
  searchForm.status = ''
  handleSearch()
}

const fetchClients = async () => {
  loading.value = true
  try {
    const result = await clientStore.search({
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
      condition: {
        ...(searchForm.keyword ? { keyword: searchForm.keyword } : {}),
        ...(searchForm.status ? { status: searchForm.status } : {})
      }
    })
    clients.value = result.items
    totalCount.value = result.totalCount
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}

// ──────── 详情 ────────

const detailDialogVisible = ref(false)
const detail = ref<ClientResult | null>(null)

const handleDetail = async (row: ClientResult) => {
  try {
    detail.value = await getClientById(row.id)
    detailDialogVisible.value = true
  } catch {
    // 拦截器已处理
  }
}

// ──────── 审核通过/拒绝 ────────

const handleApprove = async (id: string) => {
  try {
    await ElMessageBox.confirm('确认通过此客户端的审核？', '审核确认', { type: 'success' })
    await approveClient(id)
    ElMessage.success('审核通过')
    fetchClients()
    clientStore.fetchPendingCount()
  } catch {
    // 拦截器已处理或用户取消
  }
}

const rejectingId = ref('')
const rejectDialogVisible = ref(false)
const rejectLoading = ref(false)
const rejectFormRef = ref<FormInstance>()
const rejectForm = reactive({ reason: '' })
const rejectRules: FormRules = {
  reason: [{ required: true, message: '请输入拒绝原因', trigger: 'blur' }]
}

const handleReject = (client: ClientResult) => {
  rejectingId.value = client.id
  rejectForm.reason = ''
  rejectDialogVisible.value = true
}

const confirmReject = async () => {
  if (!rejectFormRef.value) return
  const valid = await rejectFormRef.value.validate().catch(() => false)
  if (!valid) return

  rejectLoading.value = true
  try {
    await rejectClient(rejectingId.value, { reason: rejectForm.reason })
    ElMessage.success('已拒绝')
    rejectDialogVisible.value = false
    fetchClients()
    clientStore.fetchPendingCount()
  } catch {
    // 拦截器已处理
  } finally {
    rejectLoading.value = false
  }
}

// ──────── 删除 ────────

const handleDelete = async (id: string) => {
  try {
    await ElMessageBox.confirm('确定删除此客户端？删除后不可恢复。', '确认删除', { type: 'warning' })
    await deleteClient(id)
    ElMessage.success('已删除')
    fetchClients()
  } catch {
    // 拦截器已处理或用户取消
  }
}

onMounted(fetchClients)

/** 拼接完整 URL（Logo 可能是相对路径） */
const getFullUrl = (url: string) => {
  if (!url) return ''
  if (url.startsWith('http')) return url
  const base = import.meta.env.VITE_API_BASE_URL || ''
  return base + url
}
</script>

<style scoped>
.clients-page {
  padding: 20px;
}

.page-card {
  border-radius: 4px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.page-title {
  font-size: 16px;
  font-weight: 600;
  color: #333;
}

.search-area {
  padding: 0;
  margin-bottom: 20px;
}

.search-form {
  margin: 0;
}

.search-form .el-form-item {
  margin-bottom: 0;
}

.pagination-area {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}

code {
  background: #f5f5f5;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 13px;
  color: #1890ff;
}

.redirect-uri {
  padding: 2px 0;
}

.grant-type-tag {
  margin-right: 4px;
}

.secret-area {
  display: flex;
  align-items: center;
  gap: 8px;
}

.secret-area code {
  color: #e6a23c;
  font-weight: 600;
}

.logo-placeholder { background: #e6f7ff; color: #1890ff; font-size: 14px; font-weight: 600; }
.text-muted { color: #c0c4cc; }
</style>
