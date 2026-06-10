<template>
  <div class="users-page">
    <el-card class="page-card">
      <template #header>
        <div class="card-header">
          <span class="page-title">用户管理</span>
        </div>
      </template>

      <!-- 搜索区域 -->
      <div class="search-area">
        <el-form :inline="true" :model="searchForm" class="search-form">
          <el-form-item label="关键字">
            <el-input
              v-model="searchForm.keyword"
              placeholder="用户名、邮箱或昵称"
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
              <el-radio-button value="Active">正常</el-radio-button>
              <el-radio-button value="Locked">已禁用</el-radio-button>
            </el-radio-group>
          </el-form-item>
          <el-form-item label="角色">
            <el-select v-model="searchForm.role" placeholder="全部角色" clearable style="width: 140px" @change="handleSearch">
              <el-option v-for="role in roleList" :key="role.name" :label="role.description || role.name" :value="role.name" />
            </el-select>
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
      <el-table :data="users" v-loading="loading" stripe>
        <el-table-column type="index" label="序号" width="70" align="center" />
        <el-table-column label="用户" min-width="180">
          <template #default="{ row }">
            <div class="user-cell">
              <el-avatar :size="28" :src="row.avatarUrl || undefined" class="user-avatar"
                :class="{ 'has-avatar': row.avatarUrl }">
                {{ !row.avatarUrl ? row.username?.charAt(0).toUpperCase() : '' }}
              </el-avatar>
              <div class="user-info">
                <span class="user-name">{{ row.username }}</span>
                <span v-if="row.nickName" class="user-nick">{{ row.nickName }}</span>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="email" label="邮箱" min-width="180" />
        <el-table-column label="角色" width="120" align="center">
          <template #default="{ row }">
            <el-tag v-for="role in row.roles" :key="role" :type="roleTagType(role)" size="small"
              style="margin-right: 4px">
              {{ roleLabel(role) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isLockedOut ? 'danger' : 'success'" size="small">
              {{ row.isLockedOut ? '已禁用' : '正常' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="注册时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" align="center" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleEditRoles(row)">角色</el-button>
            <el-button v-if="row.isLockedOut" type="success" link size="small" @click="handleUnlock(row.id)">启用</el-button>
            <el-button v-else type="warning" link size="small" @click="handleLock(row.id)">禁用</el-button>
            <el-button type="primary" link size="small" @click="handleResetPassword(row)">重置密码</el-button>
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
          @size-change="fetchUsers"
          @current-change="fetchUsers"
        />
      </div>
    </el-card>

    <!-- 修改角色对话框 -->
    <el-dialog v-model="roleDialogVisible" title="修改角色" width="450px">
      <el-form label-width="80px">
        <el-form-item label="用户">
          <span>{{ editingUser?.username }}</span>
        </el-form-item>
        <el-form-item label="角色">
          <el-checkbox-group v-model="selectedRoles">
            <el-checkbox v-for="role in roleList" :key="role.name" :value="role.name">
              {{ role.description || role.name }}
            </el-checkbox>
          </el-checkbox-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="roleDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="roleLoading" @click="confirmRoles">确认</el-button>
      </template>
    </el-dialog>

    <!-- 重置密码对话框 -->
    <el-dialog v-model="resetPwdDialogVisible" title="重置密码" width="450px">
      <el-form ref="resetPwdFormRef" :model="resetPwdForm" :rules="resetPwdRules" label-width="80px">
        <el-form-item label="用户">
          <span>{{ editingUser?.username }}</span>
        </el-form-item>
        <el-form-item label="新密码" prop="newPassword">
          <el-input v-model="resetPwdForm.newPassword" type="password" show-password placeholder="请输入新密码" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="resetPwdDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="resetPwdLoading" @click="confirmResetPassword">确认重置</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { Search } from '@element-plus/icons-vue'
import { lockUser, unlockUser, updateUserRoles, resetUserPassword, getRoles, type UserResult, type RoleResult } from '@/api/user'
import { roleLabel, roleTagType } from '@/utils/client'
import { useUserManageStore } from '@/stores/userManage'

const userManageStore = useUserManageStore()
const loading = ref(false)
const users = ref<UserResult[]>([])
const pageIndex = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const searchForm = reactive({
  keyword: '',
  status: '',
  role: ''
})

/** 角色列表（动态加载） */
const roleList = ref<RoleResult[]>([])

const fetchRoles = async () => {
  try {
    roleList.value = await getRoles()
  } catch {
    // 拦截器已处理
  }
}

/** 格式化日期 */
const formatDate = (dateStr: string) => {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  return d.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}

/** 搜索时重置到第一页 */
const handleSearch = () => {
  pageIndex.value = 1
  fetchUsers()
}

/** 重置搜索条件 */
const handleReset = () => {
  searchForm.keyword = ''
  searchForm.status = ''
  searchForm.role = ''
  handleSearch()
}

const fetchUsers = async () => {
  loading.value = true
  try {
    const result = await userManageStore.search({
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
      condition: {
        ...(searchForm.keyword ? { keyword: searchForm.keyword } : {}),
        ...(searchForm.status ? { status: searchForm.status } : {}),
        ...(searchForm.role ? { role: searchForm.role } : {})
      }
    })
    users.value = result.items
    totalCount.value = result.totalCount
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}

// ──────── 角色编辑 ────────
const editingUser = ref<UserResult | null>(null)
const roleDialogVisible = ref(false)
const roleLoading = ref(false)
const selectedRoles = ref<string[]>([])

const handleEditRoles = (user: UserResult) => {
  editingUser.value = user
  selectedRoles.value = [...user.roles]
  roleDialogVisible.value = true
}

const confirmRoles = async () => {
  if (!editingUser.value) return
  roleLoading.value = true
  try {
    await updateUserRoles(editingUser.value.id, selectedRoles.value)
    ElMessage.success('角色已更新')
    roleDialogVisible.value = false
    fetchUsers()
  } catch {
    // 拦截器已处理
  } finally {
    roleLoading.value = false
  }
}

// ──────── 禁用/启用 ────────
const handleLock = async (id: string) => {
  try {
    await ElMessageBox.confirm('确定禁用此用户？禁用后用户将无法登录。', '确认禁用', { type: 'warning' })
    await lockUser(id)
    ElMessage.success('已禁用')
    fetchUsers()
  } catch {
    // 拦截器已处理或用户取消
  }
}

const handleUnlock = async (id: string) => {
  try {
    await unlockUser(id)
    ElMessage.success('已启用')
    fetchUsers()
  } catch {
    // 拦截器已处理
  }
}

// ──────── 重置密码 ────────
const resetPwdDialogVisible = ref(false)
const resetPwdLoading = ref(false)
const resetPwdFormRef = ref<FormInstance>()
const resetPwdForm = reactive({ newPassword: '' })
const resetPwdRules: FormRules = {
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码至少6位', trigger: 'blur' }
  ]
}

const handleResetPassword = (user: UserResult) => {
  editingUser.value = user
  resetPwdForm.newPassword = ''
  resetPwdDialogVisible.value = true
}

const confirmResetPassword = async () => {
  if (!resetPwdFormRef.value) return
  const valid = await resetPwdFormRef.value.validate().catch(() => false)
  if (!valid) return

  if (!editingUser.value) return
  resetPwdLoading.value = true
  try {
    await resetUserPassword(editingUser.value.id, resetPwdForm.newPassword)
    ElMessage.success('密码已重置')
    resetPwdDialogVisible.value = false
  } catch {
    // 拦截器已处理
  } finally {
    resetPwdLoading.value = false
  }
}

onMounted(() => {
  fetchRoles()
  fetchUsers()
})
</script>

<style scoped>
.users-page {
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

.user-cell {
  display: flex;
  align-items: center;
  gap: 10px;
}

.user-avatar {
  color: white;
  font-size: 13px;
  font-weight: 600;
  flex-shrink: 0;
}

.user-avatar:not(.has-avatar) {
  background: #409eff;
}

.user-info {
  display: flex;
  flex-direction: column;
}

.user-name {
  font-size: 14px;
  color: #333;
}

.user-nick {
  font-size: 12px;
  color: #999;
}
</style>
