<template>
  <div class="client-users-page">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <div class="header-left">
            <el-button link @click="router.push('/my-clients')">
              <el-icon><ArrowLeft /></el-icon>返回我的客户端
            </el-button>
            <span class="page-title">{{ clientName }} - 授权用户</span>
          </div>
        </div>
      </template>

      <!-- 搜索 -->
      <div class="search-area">
        <el-form :inline="true" :model="searchForm" class="search-form">
          <el-form-item label="用户名">
            <el-input v-model="searchForm.keyword" placeholder="搜索用户名" clearable style="width: 200px"
              @keyup.enter="handleSearch" @clear="handleSearch">
              <template #prefix><el-icon><Search /></el-icon></template>
            </el-input>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleSearch">
              <el-icon><Search /></el-icon>搜索
            </el-button>
          </el-form-item>
        </el-form>
      </div>

      <!-- 表格 -->
      <el-table :data="users" v-loading="loading" stripe>
        <el-table-column type="index" label="序号" width="70" align="center" />
        <el-table-column prop="username" label="用户名" min-width="180" />
        <el-table-column prop="nickName" label="昵称" min-width="140">
          <template #default="{ row }">{{ row.nickName || '-' }}</template>
        </el-table-column>
        <el-table-column label="授权时间" width="170">
          <template #default="{ row }">{{ formatDate(row.authorizedAt) }}</template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-area">
        <el-pagination v-model:current-page="pageIndex" v-model:page-size="pageSize" :total="totalCount"
          :page-sizes="[10, 20, 50]" layout="total, sizes, prev, pager, next, jumper" background
          @size-change="loadData" @current-change="loadData" />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { Search, ArrowLeft } from '@element-plus/icons-vue'
import { getClientAuthorizedUsers, getClientById, type AuthorizedUserResult } from '@/api/client'
import { formatDate } from '@/utils/client'

const router = useRouter()
const route = useRoute()
const clientId = route.params.id as string

const loading = ref(false)
const users = ref<AuthorizedUserResult[]>([])
const pageIndex = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const clientName = ref('')
const searchForm = reactive({ keyword: '' })

const handleSearch = () => { pageIndex.value = 1; loadData() }

const loadData = async () => {
  loading.value = true
  try {
    const result = await getClientAuthorizedUsers(clientId, {
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
      keyword: searchForm.keyword || undefined
    })
    users.value = result.items
    totalCount.value = result.totalCount
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  try {
    const client = await getClientById(clientId)
    clientName.value = client.name
  } catch { /* 拦截器已处理 */ }
  loadData()
})
</script>

<style scoped>
.client-users-page { padding: 20px; }

.card-header { display: flex; justify-content: space-between; align-items: center; }
.header-left { display: flex; align-items: center; gap: 12px; }
.page-title { font-size: 16px; font-weight: 600; color: #333; }

.search-area { margin-bottom: 16px; }
.search-form .el-form-item { margin-bottom: 0; }

.pagination-area { display: flex; justify-content: flex-end; margin-top: 20px; }
</style>
