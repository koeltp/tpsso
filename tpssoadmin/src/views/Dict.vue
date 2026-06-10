<template>
  <div class="dict-page">
    <el-card class="page-card">
      <template #header>
        <div class="card-header">
          <span class="page-title">配置管理</span>
          <el-button type="primary" @click="handleAddType()">
            <el-icon><Plus /></el-icon>
            新增分类
          </el-button>
        </div>
      </template>

      <div class="dict-container" v-loading="loading">
        <!-- 左侧：树形分类 -->
        <div class="left-panel">
          <el-tree
            ref="treeRef"
            :data="dictTypes"
            :props="{ label: 'name', children: 'children' }"
            node-key="id"
            highlight-current
            default-expand-all
            @node-click="handleNodeClick"
          >
            <template #default="{ node, data }">
              <div class="tree-node">
                <span class="tree-label">{{ data.name }}</span>
                <span class="tree-actions">
                  <el-button type="primary" link size="small" @click.stop="handleAddType(data.id)">新增子分类</el-button>
                  <el-button type="primary" link size="small" @click.stop="handleEditType(data)">编辑</el-button>
                  <el-button type="danger" link size="small" @click.stop="handleDeleteType(data)">删除</el-button>
                </span>
              </div>
            </template>
          </el-tree>
          <div v-if="dictTypes.length === 0" class="empty-tip">暂无配置分类</div>
        </div>

        <!-- 右侧：配置项列表 -->
        <div class="right-panel">
          <template v-if="selectedType">
            <div class="panel-header">
              <span>{{ selectedType.name }} - 配置项</span>
              <el-button type="primary" size="small" @click="handleAddItem">
                <el-icon><Plus /></el-icon>
                新增
              </el-button>
            </div>
            <el-table :data="selectedType.items" stripe>
              <el-table-column prop="key" label="键" width="200" />
              <el-table-column label="值" min-width="200">
                <template #default="{ row }">
                  <span v-if="row.isSensitive" class="sensitive-value">******</span>
                  <span v-else>{{ row.value }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="description" label="描述" min-width="180" show-overflow-tooltip />
              <el-table-column label="敏感" width="70" align="center">
                <template #default="{ row }">
                  <el-icon v-if="row.isSensitive" color="#e6a23c"><Warning /></el-icon>
                </template>
              </el-table-column>
              <el-table-column label="状态" width="70" align="center">
                <template #default="{ row }">
                  <el-tag :type="row.isEnabled ? 'success' : 'info'" size="small">
                    {{ row.isEnabled ? '启用' : '禁用' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="120" align="center">
                <template #default="{ row }">
                  <el-button type="primary" link size="small" @click="handleEditItem(row)">编辑</el-button>
                  <el-button type="danger" link size="small" @click="handleDeleteItem(row)">删除</el-button>
                </template>
              </el-table-column>
            </el-table>
          </template>
          <div v-else class="empty-state">
            <el-icon size="48" color="#c0c4cc"><Setting /></el-icon>
            <p>请选择左侧配置分类</p>
          </div>
        </div>
      </div>
    </el-card>

    <!-- 字典类型弹窗 -->
    <el-dialog v-model="typeDialogVisible" :title="typeForm.id ? '编辑分类' : '新增分类'" width="500px">
      <el-form :model="typeForm" :rules="typeRules" ref="typeFormRef" label-width="80px">
        <el-form-item label="父分类">
          <el-tree-select
            v-model="typeForm.parentId"
            :data="typeTreeOptions"
            :props="{ label: 'name', children: 'children', value: 'id' }"
            placeholder="无（顶级分类）"
            clearable
            check-strictly
          />
        </el-form-item>
        <el-form-item label="编码" prop="code">
          <el-input v-model="typeForm.code" placeholder="如 OAuth、GitHub、JWT" :disabled="!!typeForm.id" />
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="typeForm.name" placeholder="如 第三方登录、GitHub" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="typeForm.description" type="textarea" placeholder="分类描述" />
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="typeForm.sort" :min="0" />
        </el-form-item>
        <el-form-item label="状态">
          <el-radio-group v-model="typeForm.isEnabled">
            <el-radio-button :value="true">启用</el-radio-button>
            <el-radio-button :value="false">禁用</el-radio-button>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="typeDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="typeSaving" @click="confirmType">确定</el-button>
      </template>
    </el-dialog>

    <!-- 字典项弹窗 -->
    <el-dialog v-model="itemDialogVisible" :title="itemForm.id ? '编辑配置项' : '新增配置项'" width="500px">
      <el-form :model="itemForm" :rules="itemRules" ref="itemFormRef" label-width="80px">
        <el-form-item label="键" prop="key">
          <el-input v-model="itemForm.key" placeholder="如 ClientId、AccessTokenExpireMinutes" :disabled="!!itemForm.id" />
        </el-form-item>
        <el-form-item label="值" prop="value">
          <el-input
            v-model="itemForm.value"
            :type="itemForm.isSensitive ? 'password' : 'text'"
            :placeholder="itemForm.isSensitive ? '输入敏感配置值' : '输入配置值'"
            show-password
          />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="itemForm.description" placeholder="配置项说明" />
        </el-form-item>
        <el-form-item label="敏感配置">
          <el-switch v-model="itemForm.isSensitive" active-text="加密存储" inactive-text="明文存储" />
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="itemForm.sort" :min="0" />
        </el-form-item>
        <el-form-item label="状态">
          <el-radio-group v-model="itemForm.isEnabled">
            <el-radio-button :value="true">启用</el-radio-button>
            <el-radio-button :value="false">禁用</el-radio-button>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="itemDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="itemSaving" @click="confirmItem">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { Plus, Setting, Warning } from '@element-plus/icons-vue'
import { getAllDict, saveDictType, deleteDictType, saveDictItem, deleteDictItem, type DictTypeResult, type DictItemResult } from '@/api/dict'

const loading = ref(false)
const dictTypes = ref<DictTypeResult[]>([])
const selectedType = ref<DictTypeResult | null>(null)

/** 从树中递归查找节点 */
const findNode = (tree: DictTypeResult[], id: string): DictTypeResult | null => {
  for (const node of tree) {
    if (node.id === id) return node
    const found = findNode(node.children, id)
    if (found) return found
  }
  return null
}

/** 收集所有叶子节点（有 items 的节点） */
const collectLeafNodes = (tree: DictTypeResult[]): DictTypeResult[] => {
  const result: DictTypeResult[] = []
  for (const node of tree) {
    if (node.items.length > 0) result.push(node)
    result.push(...collectLeafNodes(node.children))
  }
  return result
}

const fetchAll = async () => {
  loading.value = true
  try {
    dictTypes.value = await getAllDict()
    // 保持选中状态
    if (selectedType.value) {
      const updated = findNode(dictTypes.value, selectedType.value.id)
      selectedType.value = updated || null
    }
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}

const handleNodeClick = (data: DictTypeResult) => {
  selectedType.value = data
}

// ──────── 字典类型 CRUD ────────
const typeDialogVisible = ref(false)
const typeSaving = ref(false)
const typeFormRef = ref<FormInstance>()
const typeForm = reactive({
  id: '' as string,
  code: '',
  name: '',
  description: '',
  sort: 0,
  isEnabled: true,
  parentId: '' as string | undefined,
})
const typeRules: FormRules = {
  code: [{ required: true, message: '请输入编码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }],
}

/** 树形选择器数据（排除自身及子级） */
const typeTreeOptions = computed(() => {
  if (!typeForm.id) return dictTypes.value
  // 过滤掉自身及子级，防止循环引用
  const filterTree = (nodes: DictTypeResult[]): DictTypeResult[] => {
    return nodes
      .filter(n => n.id !== typeForm.id)
      .map(n => ({ ...n, children: filterTree(n.children) }))
  }
  return filterTree(dictTypes.value)
})

const handleAddType = (parentId?: string) => {
  typeForm.id = ''
  typeForm.code = ''
  typeForm.name = ''
  typeForm.description = ''
  typeForm.sort = 0
  typeForm.isEnabled = true
  typeForm.parentId = parentId || undefined
  typeDialogVisible.value = true
}

const handleEditType = (type: DictTypeResult) => {
  typeForm.id = type.id
  typeForm.code = type.code
  typeForm.name = type.name
  typeForm.description = type.description || ''
  typeForm.sort = type.sort
  typeForm.isEnabled = type.isEnabled
  typeForm.parentId = type.parentId || undefined
  typeDialogVisible.value = true
}

const confirmType = async () => {
  if (!typeFormRef.value) return
  const valid = await typeFormRef.value.validate().catch(() => false)
  if (!valid) return

  typeSaving.value = true
  try {
    await saveDictType({
      id: typeForm.id || undefined,
      code: typeForm.code,
      name: typeForm.name,
      description: typeForm.description || undefined,
      sort: typeForm.sort,
      isEnabled: typeForm.isEnabled,
      parentId: typeForm.parentId || undefined,
    })
    ElMessage.success(typeForm.id ? '更新成功' : '创建成功')
    typeDialogVisible.value = false
    fetchAll()
  } catch {
    // 拦截器已处理
  } finally {
    typeSaving.value = false
  }
}

const handleDeleteType = async (type: DictTypeResult) => {
  try {
    await ElMessageBox.confirm(`确定删除分类「${type.name}」及其所有子分类和配置项？`, '确认删除', { type: 'warning' })
    await deleteDictType(type.id)
    ElMessage.success('删除成功')
    if (selectedType.value?.id === type.id) selectedType.value = null
    fetchAll()
  } catch {
    // 用户取消或拦截器已处理
  }
}

// ──────── 字典项 CRUD ────────
const itemDialogVisible = ref(false)
const itemSaving = ref(false)
const itemFormRef = ref<FormInstance>()
const itemForm = reactive({
  id: '' as string,
  key: '',
  value: '',
  description: '',
  isSensitive: false,
  sort: 0,
  isEnabled: true,
})
const itemRules: FormRules = {
  key: [{ required: true, message: '请输入键', trigger: 'blur' }],
  value: [{ required: true, message: '请输入值', trigger: 'blur' }],
}

const handleAddItem = () => {
  itemForm.id = ''
  itemForm.key = ''
  itemForm.value = ''
  itemForm.description = ''
  itemForm.isSensitive = false
  itemForm.sort = (selectedType.value?.items.length ?? 0) + 1
  itemForm.isEnabled = true
  itemDialogVisible.value = true
}

const handleEditItem = (item: DictItemResult) => {
  itemForm.id = item.id
  itemForm.key = item.key
  itemForm.value = ''
  itemForm.description = item.description || ''
  itemForm.isSensitive = item.isSensitive
  itemForm.sort = item.sort
  itemForm.isEnabled = item.isEnabled
  itemDialogVisible.value = true
}

const confirmItem = async () => {
  if (!selectedType.value) return
  if (!itemFormRef.value) return
  const valid = await itemFormRef.value.validate().catch(() => false)
  if (!valid) return

  if (itemForm.id && !itemForm.value) {
    ElMessage.warning('请输入配置值')
    return
  }

  itemSaving.value = true
  try {
    await saveDictItem(selectedType.value.id, {
      id: itemForm.id || undefined,
      key: itemForm.key,
      value: itemForm.value,
      description: itemForm.description || undefined,
      isSensitive: itemForm.isSensitive,
      sort: itemForm.sort,
      isEnabled: itemForm.isEnabled,
    })
    ElMessage.success(itemForm.id ? '更新成功' : '创建成功')
    itemDialogVisible.value = false
    fetchAll()
  } catch {
    // 拦截器已处理
  } finally {
    itemSaving.value = false
  }
}

const handleDeleteItem = async (item: DictItemResult) => {
  try {
    await ElMessageBox.confirm(`确定删除配置项「${item.key}」？`, '确认删除', { type: 'warning' })
    await deleteDictItem(item.id)
    ElMessage.success('删除成功')
    fetchAll()
  } catch {
    // 用户取消或拦截器已处理
  }
}

onMounted(fetchAll)
</script>

<style scoped>
.dict-page {
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

.dict-container {
  display: flex;
  gap: 20px;
  min-height: 500px;
}

.left-panel {
  width: 300px;
  flex-shrink: 0;
  border-right: 1px solid #ebeef5;
  padding-right: 16px;
}

.tree-node {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex: 1;
  padding-right: 8px;
}

.tree-label {
  font-size: 14px;
}

.tree-actions {
  display: none;
}

.tree-node:hover .tree-actions {
  display: flex;
  gap: 2px;
}

.right-panel {
  flex: 1;
  overflow: hidden;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  font-weight: 600;
  font-size: 14px;
}

.sensitive-value {
  color: #e6a23c;
  letter-spacing: 2px;
}

.empty-tip {
  text-align: center;
  color: #999;
  padding: 40px 0;
}

.empty-state {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  height: 400px;
  color: #999;
}

.empty-state p {
  margin-top: 16px;
  font-size: 14px;
}
</style>
