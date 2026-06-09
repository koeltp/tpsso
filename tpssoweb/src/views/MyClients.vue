<template>
  <div class="my-clients-container">
    <div class="page-header">
      <h1>我的客户端</h1>
      <el-button type="primary" @click="router.push('/client/register')">
        <el-icon><Plus /></el-icon>
        创建客户端
      </el-button>
    </div>

    <el-empty v-if="!loading && clients.length === 0" description="暂无客户端，点击上方按钮创建" />

    <div v-else class="client-list">
      <el-card v-for="client in clients" :key="client.id" class="client-card" shadow="hover">
        <div class="client-header">
          <div class="client-info">
            <h3>{{ client.name }}</h3>
            <el-tag :type="statusTagType(client.status)" size="small">{{ statusLabel(client.status) }}</el-tag>
          </div>
          <div class="client-actions">
            <template v-if="client.status === 'Draft'">
              <el-button size="small" @click="handleSubmit(client.id)">提交审核</el-button>
              <el-button size="small" @click="handleEdit(client)">编辑</el-button>
              <el-button size="small" type="danger" @click="handleDelete(client.id)">删除</el-button>
            </template>
            <template v-if="client.status === 'Pending'">
              <el-button size="small" @click="handleWithdraw(client.id)">撤回</el-button>
            </template>
            <template v-if="client.status === 'Rejected'">
              <el-button size="small" @click="handleWithdraw(client.id)">重新编辑</el-button>
            </template>
          </div>
        </div>

        <el-descriptions :column="2" size="small" style="margin-top: 12px">
          <el-descriptions-item label="Client ID">
            <code>{{ client.clientId }}</code>
          </el-descriptions-item>
          <el-descriptions-item label="类型">
            {{ client.isPublic ? '公开客户端' : '机密客户端' }}
          </el-descriptions-item>
          <el-descriptions-item label="回调地址" :span="2">
            <div v-for="uri in client.redirectUris.split('\n')" :key="uri" class="redirect-uri">
              {{ uri }}
            </div>
          </el-descriptions-item>
          <el-descriptions-item label="授权范围">
            <el-tag v-for="scope in client.allowedScopes.split(' ')" :key="scope" size="small" style="margin-right: 4px">
              {{ scope }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="创建时间">
            {{ formatDate(client.createdAt) }}
          </el-descriptions-item>
          <el-descriptions-item v-if="client.reviewRemark" label="审核备注" :span="2">
            <span style="color: #f56c6c">{{ client.reviewRemark }}</span>
          </el-descriptions-item>
        </el-descriptions>
      </el-card>
    </div>

    <!-- 编辑对话框 -->
    <el-dialog v-model="editDialogVisible" title="编辑客户端" width="500px">
      <el-form ref="editFormRef" :model="editForm" :rules="editRules" label-position="top">
        <el-form-item label="应用名称" prop="name">
          <el-input v-model="editForm.name" />
        </el-form-item>
        <el-form-item label="应用描述" prop="description">
          <el-input v-model="editForm.description" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item label="回调地址" prop="redirectUris">
          <el-input v-model="editForm.redirectUris" type="textarea" :rows="3" placeholder="每行一个回调地址" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="editLoading" @click="handleUpdate">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import {
  getMyClients,
  submitClient,
  withdrawClient,
  deleteClient,
  updateClient,
  type ClientResult
} from '@/api/client'

const router = useRouter()
const loading = ref(false)
const clients = ref<ClientResult[]>([])

// 编辑相关
const editDialogVisible = ref(false)
const editLoading = ref(false)
const editFormRef = ref<FormInstance>()
const editingId = ref('')
const editForm = reactive({
  name: '',
  description: '',
  redirectUris: ''
})

const editRules: FormRules = {
  name: [
    { required: true, message: '请输入应用名称', trigger: 'blur' },
    { min: 2, max: 100, message: '名称长度在 2 到 100 个字符', trigger: 'blur' }
  ],
  redirectUris: [{ required: true, message: '请输入回调地址', trigger: 'blur' }]
}

const fetchClients = async () => {
  loading.value = true
  try {
    clients.value = await getMyClients()
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}

const handleSubmit = async (id: string) => {
  try {
    await submitClient(id)
    ElMessage.success('已提交审核')
    fetchClients()
  } catch {
    // 拦截器已处理
  }
}

const handleWithdraw = async (id: string) => {
  try {
    await withdrawClient(id)
    ElMessage.success('已撤回')
    fetchClients()
  } catch {
    // 拦截器已处理
  }
}

const handleDelete = async (id: string) => {
  try {
    await ElMessageBox.confirm('确定删除此客户端？删除后不可恢复。', '确认删除', {
      type: 'warning'
    })
    await deleteClient(id)
    ElMessage.success('已删除')
    fetchClients()
  } catch {
    // 拦截器已处理或用户取消
  }
}

const handleEdit = (client: ClientResult) => {
  editingId.value = client.id
  editForm.name = client.name
  editForm.description = client.description || ''
  editForm.redirectUris = client.redirectUris
  editDialogVisible.value = true
}

const handleUpdate = async () => {
  if (!editFormRef.value) return
  const valid = await editFormRef.value.validate().catch(() => false)
  if (!valid) return

  editLoading.value = true
  try {
    await updateClient(editingId.value, {
      name: editForm.name,
      description: editForm.description || undefined,
      redirectUris: editForm.redirectUris
    })
    ElMessage.success('更新成功')
    editDialogVisible.value = false
    fetchClients()
  } catch {
    // 拦截器已处理
  } finally {
    editLoading.value = false
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
.my-clients-container {
  max-width: 900px;
  margin: 0 auto;
  padding: 30px 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.page-header h1 {
  font-size: 22px;
  font-weight: 600;
  color: #333;
}

.client-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.client-card {
  border-radius: 12px;
}

.client-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.client-info {
  display: flex;
  align-items: center;
  gap: 10px;
}

.client-info h3 {
  margin: 0;
  font-size: 16px;
  color: #333;
}

.redirect-uri {
  font-size: 13px;
  color: #666;
  line-height: 1.6;
}

code {
  background: #f5f7fa;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 13px;
  color: #409eff;
}
</style>
