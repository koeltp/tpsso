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
        <el-table-column label="Logo" width="70" align="center">
          <template #default="{ row }">
            <el-avatar v-if="row.logo" :src="getFullUrl(row.logo)" :size="32" shape="square" />
            <el-avatar v-else :size="32" shape="square" class="logo-placeholder">{{ row.name?.charAt(0) || '?' }}</el-avatar>
          </template>
        </el-table-column>
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
        <el-table-column label="操作" width="240" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="viewDetail(row)">详情</el-button>
            <el-button v-if="row.status === 'Approved'" link type="primary" size="small" @click="viewUsers(row)">授权用户</el-button>
            <el-button v-if="row.status === 'Draft'" link type="primary" size="small" @click="openEditDialog(row)">编辑</el-button>
            <el-button v-if="row.status === 'Draft'" link type="warning" size="small" @click="handleSubmit(row.id)">提交审核</el-button>
            <el-button v-if="row.status === 'Pending'" link type="info" size="small" @click="handleWithdraw(row.id)">撤回</el-button>
            <el-button v-if="row.status === 'Rejected'" link type="primary" size="small" @click="openEditDialog(row)">编辑</el-button>
            <el-button v-if="row.status === 'Rejected'" link type="warning" size="small" @click="handleSubmit(row.id)">重新提交</el-button>
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
        <el-form-item label="应用Logo">
          <div class="logo-upload-area">
            <el-upload
              class="logo-uploader"
              :show-file-list="false"
              :before-upload="beforeLogoUpload"
              :http-request="handleLogoUpload"
              accept="image/jpeg,image/png,image/gif,image/webp,image/svg+xml"
            >
              <el-avatar v-if="clientForm.logo" :src="getFullUrl(clientForm.logo)" :size="64" shape="square" />
              <div v-else class="logo-upload-placeholder">
                <el-icon :size="24"><Plus /></el-icon>
                <span>上传Logo</span>
              </div>
            </el-upload>
            <el-button v-if="clientForm.logo" link type="danger" size="small" @click="clientForm.logo = ''">移除</el-button>
          </div>
        </el-form-item>
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
        <el-form-item label="授权类型">
          <el-checkbox-group v-model="selectedGrantTypes">
            <el-checkbox label="授权码模式" value="authorization_code" />
            <el-checkbox label="刷新令牌" value="refresh_token" />
            <el-checkbox label="客户端凭证" value="client_credentials" />
            <el-checkbox label="设备码" value="device_code" />
          </el-checkbox-group>
          <div class="form-tip">授权码模式为标准 OAuth2 流程；客户端凭证适用于 M2M 场景；设备码适用于 IoT 设备</div>
        </el-form-item>
        <el-form-item label="确认类型">
          <el-radio-group v-model="clientForm.consentType">
            <el-radio value="explicit">需用户确认</el-radio>
            <el-radio value="implicit">自动确认</el-radio>
          </el-radio-group>
          <div class="form-tip">选择"需用户确认"时，用户首次授权会看到确认页面；选择"自动确认"则跳过确认</div>
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
        <el-descriptions-item label="Logo">
          <el-avatar v-if="detailData.logo" :src="getFullUrl(detailData.logo)" :size="48" shape="square" />
          <span v-else class="text-muted">未设置</span>
        </el-descriptions-item>
        <el-descriptions-item label="应用名称">{{ detailData.name }}</el-descriptions-item>
        <el-descriptions-item label="Client ID"><code>{{ detailData.clientId }}</code></el-descriptions-item>
        <el-descriptions-item v-if="!detailData.isPublic && detailData.status === 'Approved'" label="Client Secret">
          <div class="secret-area">
            <code v-if="detailPlainSecret">{{ detailPlainSecret }}</code>
            <code v-else>••••••••</code>
            <el-button v-if="!detailPlainSecret" type="primary" link size="small" @click="handleShowSecret"><el-icon><Lock /></el-icon>获取密钥</el-button>
            <el-button v-if="detailPlainSecret" type="primary" link size="small" @click="copyText(detailPlainSecret)"><el-icon><CopyDocument /></el-icon>复制</el-button>
            <el-button v-if="detailPlainSecret" type="primary" link size="small" @click="downloadSecret"><el-icon><Download /></el-icon>下载</el-button>
            <el-button type="warning" link size="small" @click="handleRegenerateSecret"><el-icon><Refresh /></el-icon>重置</el-button>
          </div>
          <el-alert v-if="detailPlainSecret" type="warning" :closable="false" show-icon style="margin-top: 8px">
            <template #title>请立即复制并妥善保存此密钥！关闭此弹窗后将无法再次查看，只能重置生成新密钥。</template>
          </el-alert>
        </el-descriptions-item>
        <el-descriptions-item label="客户端类型">{{ detailData.isPublic ? '公开（SPA/移动端）' : '机密（服务端）' }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="statusTagType(detailData.status)" size="small">{{ statusLabel(detailData.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item v-if="detailData.description" label="描述">{{ detailData.description }}</el-descriptions-item>
        <el-descriptions-item label="回调地址">
          <div v-for="uri in detailData.redirectUris.split('\n')" :key="uri" class="redirect-uri">{{ uri }}</div>
        </el-descriptions-item>
        <el-descriptions-item label="授权范围">
          <el-tag v-for="scope in detailData.allowedScopes.split(' ')" :key="scope" size="small" class="grant-type-tag">{{ grantTypeLabel(scope) }}</el-tag>
          </el-descriptions-item>
        <el-descriptions-item label="授权类型">
          <el-tag v-for="gt in detailData.grantTypes.split(' ')" :key="gt" size="small" class="grant-type-tag">{{ grantTypeLabel(gt) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="确认类型">{{ detailData.consentType === 'implicit' ? '自动确认' : '需用户确认' }}</el-descriptions-item>
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
import { Search, Plus, Download, CopyDocument,Refresh,Lock } from '@element-plus/icons-vue'
import {
  searchMyClients, createClient, updateClient, deleteClient as deleteClientApi,
  submitClient, withdrawClient, getClientById, regenerateClientSecret, uploadClientLogo,
  type ClientResult
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
const detailPlainSecret = ref('')

const clientForm = reactive({
  name: '', description: '', logo: '', redirectUris: '', allowedScopes: 'openid profile email',
  grantTypes: 'authorization_code refresh_token', isPublic: true, consentType: 'explicit', rowVersion: ''
})

const selectedScopes = computed({
  get: () => clientForm.allowedScopes.split(' ').filter(s => s),
  set: (val: string[]) => { clientForm.allowedScopes = val.join(' ') }
})

const selectedGrantTypes = computed({
  get: () => clientForm.grantTypes.split(' ').filter(s => s),
  set: (val: string[]) => { clientForm.grantTypes = val.join(' ') }
})

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

/** 验证回调地址：每行必须是合法的 http/https URL */
const validateRedirectUris = (_rule: any, value: string, callback: any) => {
  if (!value) return callback(new Error('请输入回调地址'))
  const lines = value.split('\n').map(s => s.trim()).filter(Boolean)
  const invalid = lines.filter(u => {
    try { const url = new URL(u); return url.protocol !== 'http:' && url.protocol !== 'https:' }
    catch { return true }
  })
  if (invalid.length) return callback(new Error(`回调地址格式错误：${invalid[0]}`))
  callback()
}

const formRules: FormRules = {
  name: [{ required: true, message: '请输入应用名称', trigger: 'blur' }],
  redirectUris: [{ required: true, validator: validateRedirectUris, trigger: 'blur' }]
}

const openCreateDialog = () => {
  isEdit.value = false; editId.value = ''; detailPlainSecret.value = ''
  dialogVisible.value = true
}

const openEditDialog = async (row: ClientResult) => {
  isEdit.value = true; editId.value = row.id; detailPlainSecret.value = ''
  const detail = await getClientById(row.id)
  Object.assign(clientForm, {
    name: detail.name, description: detail.description || '',
    logo: detail.logo || '',
    redirectUris: detail.redirectUris, allowedScopes: detail.allowedScopes,
    grantTypes: detail.grantTypes || 'authorization_code refresh_token',
    isPublic: detail.isPublic, consentType: detail.consentType || 'explicit',
    rowVersion: detail.rowVersion || ''
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
        logo: clientForm.logo || undefined,
        redirectUris: clientForm.redirectUris, allowedScopes: clientForm.allowedScopes,
        grantTypes: clientForm.grantTypes, consentType: clientForm.consentType,
        rowVersion: clientForm.rowVersion || undefined
      })
      // 编辑模式下如果有新 Logo 文件则上传
      if (pendingLogoFile.value) {
        await uploadClientLogo(editId.value, pendingLogoFile.value)
        pendingLogoFile.value = null
      }
      ElMessage.success('更新成功')
    } else {
      const created = await createClient({
        name: clientForm.name, description: clientForm.description || undefined,
        redirectUris: clientForm.redirectUris, allowedScopes: clientForm.allowedScopes,
        grantTypes: clientForm.grantTypes, isPublic: clientForm.isPublic,
        consentType: clientForm.consentType
      })
      // 创建成功后上传 Logo
      if (pendingLogoFile.value && created.id) {
        await uploadClientLogo(created.id, pendingLogoFile.value)
        pendingLogoFile.value = null
      }
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch {
    // 拦截器已处理
  } finally {
    submitting.value = false
  }
}

/** 拼接完整 URL（Logo 可能是相对路径） */
const getFullUrl = (url: string) => {
  if (!url) return ''
  if (url.startsWith('http')) return url
  // 相对路径拼接 Admin API 基地址
  const base = import.meta.env.VITE_API_BASE_URL || ''
  return base + url
}

/** Logo 上传前校验 */
const beforeLogoUpload = (file: File) => {
  const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/svg+xml']
  if (!allowedTypes.includes(file.type)) {
    ElMessage.error('仅支持 JPG、PNG、GIF、WebP、SVG 格式')
    return false
  }
  if (file.size > 2 * 1024 * 1024) {
    ElMessage.error('Logo 文件大小不能超过 2MB')
    return false
  }
  return true
}

/** 自定义 Logo 上传请求 */
const handleLogoUpload = async (options: any) => {
  // 新增时还没有 id，先保存文件，创建后上传
  if (!isEdit.value && !editId.value) {
    pendingLogoFile.value = options.file
    clientForm.logo = URL.createObjectURL(options.file)
    return
  }
  try {
    const url = await uploadClientLogo(editId.value, options.file)
    clientForm.logo = url
    ElMessage.success('Logo 上传成功')
  } catch {
    // 拦截器已处理
  }
}

const pendingLogoFile = ref<File | null>(null)

const resetForm = () => {
  clientForm.name = ''; clientForm.description = ''; clientForm.logo = ''; clientForm.redirectUris = ''
  clientForm.allowedScopes = 'openid profile email'; clientForm.grantTypes = 'authorization_code refresh_token'
  clientForm.isPublic = true; clientForm.consentType = 'explicit'; clientForm.rowVersion = ''
  detailPlainSecret.value = ''; pendingLogoFile.value = null
}

/** 复制文本到剪贴板 */
const copyText = async (text: string) => {
  try {
    await navigator.clipboard.writeText(text)
    ElMessage.success('已复制到剪贴板')
  } catch { ElMessage.warning('复制失败，请手动复制') }
}

/** 下载密钥文件 */
const downloadSecret = () => {
  if (!detailPlainSecret.value || !detailData.value) return
  const content = [
    `# ${detailData.value.name} - 客户端凭据`,
    `# 生成时间：${new Date().toLocaleString()}`,
    `# 警告：请妥善保管此文件，切勿泄露！`,
    '',
    `CLIENT_ID=${detailData.value.clientId}`,
    `CLIENT_SECRET=${detailPlainSecret.value}`
  ].join('\n')
  const blob = new Blob([content], { type: 'text/plain;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${detailData.value.clientId}_credentials.txt`
  a.click()
  URL.revokeObjectURL(url)
}

/** 获取密钥：生成新密钥并返回明文（后端不存储明文，只能重新生成） */
const handleShowSecret = async () => {
  if (!detailData.value) return
  try {
    await ElMessageBox.confirm('将生成新的密钥，旧密钥（如有）将立即失效。确定获取？', '获取密钥', { type: 'warning' })
    const result = await regenerateClientSecret(detailData.value.id)
    detailPlainSecret.value = result.plainSecret || ''
    ElMessage.success('密钥已生成，请妥善保存')
  } catch {
    // 用户取消或拦截器处理
  }
}

/** 重置密钥 */
const handleRegenerateSecret = async () => {
  if (!detailData.value) return
  try {
    await ElMessageBox.confirm('重置后旧密钥立即失效，确定重置？', '重置密钥', { type: 'warning' })
    const result = await regenerateClientSecret(detailData.value.id)
    detailPlainSecret.value = result.plainSecret || ''
    ElMessage.success('密钥已重置，请妥善保存')
  } catch {
    // 用户取消或拦截器处理
  }
}

// ──────── 详情 ────────

const detailVisible = ref(false)
const detailData = ref<ClientResult | null>(null)

const viewDetail = async (row: ClientResult) => {
  try {
    detailData.value = await getClientById(row.id)
    detailPlainSecret.value = ''
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

.redirect-uri { padding: 2px 0; }

.form-tip { font-size: 12px; color: #909399; line-height: 1.4; margin-top: 4px; }

.grant-type-tag { margin-right: 4px; }

.logo-placeholder { background: #e6f7ff; color: #1890ff; font-size: 14px; font-weight: 600; }

.logo-upload-area { display: flex; align-items: flex-end; gap: 8px; }

.logo-upload-placeholder {
  width: 64px; height: 64px; border: 1px dashed #d9d9d9; border-radius: 8px;
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  color: #999; font-size: 12px; cursor: pointer; transition: border-color 0.2s;
}
.logo-upload-placeholder:hover { border-color: #1890ff; color: #1890ff; }

.text-muted { color: #c0c4cc; }
</style>
