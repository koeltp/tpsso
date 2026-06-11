<template>
  <div class="my-clients-page">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span class="page-title">我的客户端</span>
          <el-button type="primary" @click="openCreateDialog">
            <el-icon><Plus /></el-icon>新增客户端
          </el-button>
        </div>
      </template>

      <!-- 搜索区 -->
      <div class="search-area">
        <el-form :inline="true" :model="searchForm" class="search-form">
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="搜索客户端名称" clearable style="width: 200px"
              @keyup.enter="handleSearch" @clear="handleSearch">
              <template #prefix><el-icon><Search /></el-icon></template>
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
              <el-icon><Search /></el-icon>搜索
            </el-button>
            <el-button @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
      </div>

      <!-- 列表 -->
      <el-table :data="clients" v-loading="loading" stripe>
        <el-table-column type="index" label="序号" width="70" align="center" />
        <el-table-column prop="name" label="名称" min-width="140" />
        <el-table-column prop="clientId" label="Client ID" min-width="180">
          <template #default="{ row }"><code>{{ row.clientId }}</code></template>
        </el-table-column>
        <el-table-column label="类型" width="90" align="center">
          <template #default="{ row }">{{ row.isPublic ? '公开' : '机密' }}</template>
        </el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="statusTagType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="创建时间" width="170">
          <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="280" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="viewDetail(row)">详情</el-button>
            <el-button v-if="row.status === 'Approved'" link type="primary" size="small" @click="viewUsers(row)">授权用户</el-button>
            <el-button v-if="row.status === 'Draft'" link type="primary" size="small" @click="openEditDialog(row)">编辑</el-button>
            <el-button v-if="row.status === 'Draft'" link type="warning" size="small" @click="handleSubmit(row.id)">提交审核</el-button>
            <el-button v-if="row.status === 'Pending'" link type="info" size="small" @click="handleWithdraw(row.id)">撤回</el-button>
            <el-button v-if="row.status === 'Rejected'" link type="primary" size="small" @click="openEditDialog(row)">编辑</el-button>
            <el-button v-if="row.status !== 'Approved'" link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-area">
        <el-pagination v-model:current-page="pageIndex" v-model:page-size="pageSize" :total="totalCount"
          :page-sizes="[10, 20, 50]" layout="total, sizes, prev, pager, next, jumper" background
          @size-change="loadData" @current-change="loadData" />
      </div>
    </el-card>

    <!-- 新增/编辑弹窗 -->
    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑客户端' : '新增客户端'" width="600px" destroy-on-close @closed="resetForm">
      <el-form :model="clientForm" :rules="formRules" ref="formRef" label-width="100px">
        <el-form-item label="应用名称" prop="name">
          <el-input v-model="clientForm.name" placeholder="请输入应用名称" />
        </el-form-item>
        <el-form-item label="应用描述" prop="description">
          <el-input v-model="clientForm.description" type="textarea" :rows="2" placeholder="请输入应用描述" />
        </el-form-item>
        <el-form-item v-if="!isEdit" label="客户端类型" prop="isPublic">
          <el-radio-group v-model="clientForm.isPublic">
            <el-radio :value="true">公开（SPA/移动端）</el-radio>
            <el-radio :value="false">机密（服务端）</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="回调地址" prop="redirectUris">
          <el-input v-model="clientForm.redirectUris" type="textarea" :rows="3"
            placeholder="每行一个回调地址，例如：&#10;http://localhost:3000/callback&#10;https://example.com/callback" />
        </el-form-item>
        <el-form-item label="授权范围">
          <el-checkbox-group v-model="selectedScopes">
            <el-checkbox label="openid" value="openid" />
            <el-checkbox label="profile" value="profile" />
            <el-checkbox label="email" value="email" />
            <el-checkbox label="roles" value="roles" />
          </el-checkbox-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="submitForm">确认</el-button>
      </template>
    </el-dialog>

    <!-- 详情弹窗 -->
    <el-dialog v-model="detailVisible" title="客户端详情" width="600px">
      <el-descriptions :column="1" border v-if="detailData">
        <el-descriptions-item label="应用名称">{{ detailData.name }}</el-descriptions-item>
        <el-descriptions-item label="Client ID"><code>{{ detailData.clientId }}</code></el-descriptions-item>
        <el-descriptions-item v-if="createdSecret" label="Client Secret">
          <div class="secret-area">
            <code>{{ createdSecret }}</code>
            <el-button type="primary" link size="small" @click="copySecret">复制</el-button>
            <span class="secret-hint">仅显示一次，请妥善保存</span>
          </div>
        </el-descriptions-item>
        <el-descriptions-item label="客户端类型">{{ detailData.isPublic ? '公开（SPA/移动端）' : '机密（服务端）' }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="statusTagType(detailData.status)" size="small">{{ statusLabel(detailData.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item v-if="detailData.description" label="描述">{{ detailData.description }}</el-descriptions-item>
        <el-descriptions-item label="回调地址">
          <div v-for="uri in detailData.redirectUris.split('\n')" :key="uri" class="redirect-uri">{{ uri }}</div>
        </el-descriptions-item>
        <el-descriptions-item label="授权范围">{{ detailData.allowedScopes }}</el-descriptions-item>
        <el-descriptions-item v-if="detailData.reviewRemark" label="审核备注">{{ detailData.reviewRemark }}</el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ formatDate(detailData.createdAt) }}</el-descriptions-item>
        <el-descriptions-item v-if="detailData.updatedAt" label="更新时间">{{ formatDate(detailData.updatedAt) }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import type { FormInstance, FormRules } from 'element-plus'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Plus } from '@element-plus/icons-vue'
import {
  searchMyClients, createClient, updateClient, deleteClient as deleteClientApi,
  submitClient, withdrawClient, getClientById,
  type ClientResult, type ClientCreatedResult
} from '@/api/client'
import { statusTagType, statusLabel, formatDate } from '@/utils/client'

const router = useRouter()
const loading = ref(false)
const clients = ref<ClientResult[]>([])
const pageIndex = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const searchForm = reactive({ keyword: '', status: '' })

const handleSearch = () => { pageIndex.value = 1; loadData() }
const handleReset = () => { searchForm.keyword = ''; searchForm.status = ''; handleSearch() }

const loadData = async () => {
  loading.value = true
  try {
    const result = await searchMyClients({
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

// ──────── 新增/编辑 ────────

const dialogVisible = ref(false)
const isEdit = ref(false)
const editId = ref('')
const submitting = ref(false)
const formRef = ref<FormInstance>()
const createdSecret = ref('')

const clientForm = reactive({
  name: '', description: '', redirectUris: '', allowedScopes: 'openid profile email', isPublic: true, rowVersion: ''
})

const selectedScopes = computed({
  get: () => clientForm.allowedScopes.split(' ').filter(s => s),
  set: (val: string[]) => { clientForm.allowedScopes = val.join(' ') }
})

const formRules: FormRules = {
  name: [{ required: true, message: '请输入应用名称', trigger: 'blur' }],
  redirectUris: [{ required: true, message: '请输入回调地址', trigger: 'blur' }]
}

const openCreateDialog = () => {
  isEdit.value = false; editId.value = ''; createdSecret.value = ''
  dialogVisible.value = true
}

const openEditDialog = async (row: ClientResult) => {
  isEdit.value = true; editId.value = row.id; createdSecret.value = ''
  const detail = await getClientById(row.id)
  Object.assign(clientForm, {
    name: detail.name, description: detail.description || '',
    redirectUris: detail.redirectUris, allowedScopes: detail.allowedScopes,
    isPublic: detail.isPublic, rowVersion: detail.rowVersion || ''
  })
  dialogVisible.value = true
}

const submitForm = async () => {
  if (!formRef.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    if (isEdit.value) {
      await updateClient(editId.value, {
        name: clientForm.name, description: clientForm.description || undefined,
        redirectUris: clientForm.redirectUris, allowedScopes: clientForm.allowedScopes,
        rowVersion: clientForm.rowVersion || undefined
      })
      ElMessage.success('更新成功')
    } else {
      const result = await createClient({
        name: clientForm.name, description: clientForm.description || undefined,
        redirectUris: clientForm.redirectUris, allowedScopes: clientForm.allowedScopes,
        isPublic: clientForm.isPublic
      })
      if ((result as ClientCreatedResult).plainSecret) {
        createdSecret.value = (result as ClientCreatedResult).plainSecret!
      }
      ElMessage.success('创建成功')
    }
    dialogVisible.value = !createdSecret.value
    loadData()
  } catch {
    // 拦截器已处理
  } finally {
    submitting.value = false
  }
}

const resetForm = () => {
  clientForm.name = ''; clientForm.description = ''; clientForm.redirectUris = ''
  clientForm.allowedScopes = 'openid profile email'; clientForm.isPublic = true; clientForm.rowVersion = ''
  createdSecret.value = ''
}

const copySecret = async () => {
  try {
    await navigator.clipboard.writeText(createdSecret.value)
    ElMessage.success('已复制到剪贴板')
  } catch { ElMessage.warning('复制失败，请手动复制') }
}

// ──────── 详情 ────────

const detailVisible = ref(false)
const detailData = ref<ClientResult | null>(null)

const viewDetail = async (row: ClientResult) => {
  try {
    detailData.value = await getClientById(row.id)
    detailVisible.value = true
  } catch { /* 拦截器已处理 */ }
}

// ──────── 授权用户 ────────

const viewUsers = (row: ClientResult) => {
  router.push(`/my-clients/${row.id}/users`)
}

// ──────── 提交/撤回/删除 ────────

const handleSubmit = async (id: string) => {
  try {
    await ElMessageBox.confirm('确认提交审核？提交后不可修改，需等待管理员审批。', '提交审核', { type: 'warning' })
    await submitClient(id)
    ElMessage.success('已提交审核')
    loadData()
  } catch { /* 取消或拦截器处理 */ }
}

const handleWithdraw = async (id: string) => {
  try {
    await ElMessageBox.confirm('确认撤回审核？撤回后可重新编辑。', '撤回审核', { type: 'warning' })
    await withdrawClient(id)
    ElMessage.success('已撤回')
    loadData()
  } catch { /* 取消或拦截器处理 */ }
}

const handleDelete = async (row: ClientResult) => {
  try {
    await ElMessageBox.confirm(`确定删除客户端「${row.name}」？删除后不可恢复。`, '确认删除', { type: 'warning' })
    await deleteClientApi(row.id)
    ElMessage.success('已删除')
    loadData()
  } catch { /* 取消或拦截器处理 */ }
}

onMounted(loadData)
</script>

<style scoped>
.my-clients-page { padding: 20px; }

.card-header { display: flex; justify-content: space-between; align-items: center; }
.page-title { font-size: 16px; font-weight: 600; color: #333; }

.search-area { margin-bottom: 16px; }
.search-form .el-form-item { margin-bottom: 0; }

.pagination-area { display: flex; justify-content: flex-end; margin-top: 20px; }

code { background: #f5f5f5; padding: 2px 6px; border-radius: 4px; font-size: 13px; color: #1890ff; }

.secret-area { display: flex; align-items: center; gap: 8px; }
.secret-area code { color: #e6a23c; font-weight: 600; }
.secret-hint { color: #e6a23c; font-size: 12px; }

.redirect-uri { padding: 2px 0; }
</style>
