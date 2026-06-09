<template>
  <div class="all-clients">
    <h2 class="page-title">客户端管理</h2>

    <!-- 筛选栏 -->
    <div class="filter-bar">
      <el-radio-group v-model="statusFilter" size="small">
        <el-radio-button value="">全部</el-radio-button>
        <el-radio-button value="Draft">草稿</el-radio-button>
        <el-radio-button value="Pending">待审核</el-radio-button>
        <el-radio-button value="Approved">已通过</el-radio-button>
        <el-radio-button value="Rejected">已拒绝</el-radio-button>
      </el-radio-group>
    </div>

    <el-table :data="filteredClients" stripe style="width: 100%" v-loading="loading">
      <el-table-column prop="name" label="应用名称" min-width="140" />
      <el-table-column prop="clientId" label="Client ID" min-width="180">
        <template #default="{ row }">
          <code>{{ row.clientId }}</code>
        </template>
      </el-table-column>
      <el-table-column label="类型" width="100">
        <template #default="{ row }">
          {{ row.isPublic ? '公开' : '机密' }}
        </template>
      </el-table-column>
      <el-table-column label="状态" width="90">
        <template #default="{ row }">
          <el-tag :type="statusTagType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="创建时间" width="170">
        <template #default="{ row }">
          {{ formatDate(row.createdAt) }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="200" fixed="right">
        <template #default="{ row }">
          <template v-if="row.status === 'Pending'">
            <el-button type="primary" size="small" text @click="handleApprove(row.id)">通过</el-button>
            <el-button type="warning" size="small" text @click="handleReject(row)">拒绝</el-button>
          </template>
          <el-button type="danger" size="small" text @click="handleDelete(row.id)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 拒绝对话框 -->
    <el-dialog v-model="rejectDialogVisible" title="拒绝审核" width="400px">
      <el-form ref="rejectFormRef" :model="rejectForm" :rules="rejectRules">
        <el-form-item label="拒绝原因" prop="reason">
          <el-input v-model="rejectForm.reason" type="textarea" :rows="3" placeholder="请输入拒绝原因" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="rejectDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="rejectLoading" @click="confirmReject">确认拒绝</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { getAllClients, deleteClient, approveClient, rejectClient, type ClientResult } from '@/api/client'

const route = useRoute()
const loading = ref(false)
const clients = ref<ClientResult[]>([])

// 从 URL 参数读取初始筛选状态（Dashboard 跳转时传入）
const statusFilter = ref((route.query.status as string) || '')

const filteredClients = computed(() => {
  if (!statusFilter.value) return clients.value
  return clients.value.filter(c => c.status === statusFilter.value)
})

const fetchClients = async () => {
  loading.value = true
  try {
    clients.value = await getAllClients()
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}

const handleApprove = async (id: string) => {
  try {
    await ElMessageBox.confirm('确认通过此客户端的审核？', '审核确认', { type: 'success' })
    await approveClient(id)
    ElMessage.success('审核通过')
    fetchClients()
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
  } catch {
    // 拦截器已处理
  } finally {
    rejectLoading.value = false
  }
}

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

const statusTagType = (status: string) => {
  switch (status) {
    case 'Draft': return 'info'
    case 'Pending': return 'warning'
    case 'Approved': return 'success'
    case 'Rejected': return 'danger'
    default: return 'info'
  }
}

const statusLabel = (status: string) => {
  switch (status) {
    case 'Draft': return '草稿'
    case 'Pending': return '待审核'
    case 'Approved': return '已通过'
    case 'Rejected': return '已拒绝'
    default: return status
  }
}

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('zh-CN')
}

onMounted(fetchClients)
</script>

<style scoped>
.page-title {
  font-size: 18px;
  font-weight: 600;
  color: #333;
  margin-bottom: 20px;
}

.filter-bar {
  margin-bottom: 16px;
}

code {
  background: #f5f5f5;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 13px;
  color: #1890ff;
}
</style>
