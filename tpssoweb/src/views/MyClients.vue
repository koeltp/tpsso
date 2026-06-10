<template>
  <div class="my-clients-page">
    <el-card class="page-card">
      <template #header>
        <div class="card-header">
          <span class="page-title">我的客户端</span>
          <el-button type="primary" @click="router.push('/client/register')">
            <el-icon><Plus /></el-icon>
            创建客户端
          </el-button>
        </div>
      </template>

      <!-- 搜索区域 -->
      <div class="search-area">
        <el-form :inline="true" class="search-form">
          <el-form-item label="关键字">
            <el-input
              v-model="keyword"
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
            <el-radio-group v-model="statusFilter" @change="handleSearch">
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
            <el-descriptions-item label="回调地址" :span="2">
              <div v-for="(uri, index) in client.redirectUris.split('\n')" :key="`${uri}-${index}`" class="redirect-uri">
                {{ uri }}
              </div>
            </el-descriptions-item>
          </el-descriptions>
        </el-card>

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
      </div>
    </el-card>

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
import { Plus, Search } from '@element-plus/icons-vue'
import {
  searchMyClients,
  submitClient,
  withdrawClient,
  deleteClient,
  updateClient,
  type ClientResult
} from '@/api/client'
import { statusTagType, statusLabel, formatDate } from '@/utils/client'

const router = useRouter()
const loading = ref(false)
const clients = ref<ClientResult[]>([])
const pageIndex = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const keyword = ref('')
const statusFilter = ref('')

/** 搜索时重置到第一页 */
const handleSearch = () => {
  pageIndex.value = 1
  fetchClients()
}

/** 重置搜索条件 */
const handleReset = () => {
  keyword.value = ''
  statusFilter.value = ''
  handleSearch()
}

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
    const result = await searchMyClients({
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
      condition: {
        ...(keyword.value ? { keyword: keyword.value } : {}),
        ...(statusFilter.value ? { status: statusFilter.value } : {})
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

onMounted(fetchClients)
</script>

<style scoped>
.my-clients-page {
  max-width: 1200px;
  margin: 0 auto;
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
}

.search-form {
  margin: 0;
}

.search-form .el-form-item {
  margin-bottom: 0;
}

.search-area {
  margin-bottom: 16px;
}

.client-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.client-card {
  border-radius: 8px;
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

.pagination-area {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
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
