<template>
  <div class="client-register-container">
    <h1 class="page-title">创建客户端应用</h1>

    <el-card shadow="never" class="register-card">
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top" @submit.prevent="handleCreate">
        <el-form-item label="应用名称" prop="name">
          <el-input v-model="form.name" placeholder="如：我的博客系统" />
        </el-form-item>

        <el-form-item label="应用描述" prop="description">
          <el-input v-model="form.description" type="textarea" :rows="2" placeholder="简要描述应用用途" />
        </el-form-item>

        <el-form-item label="客户端类型" prop="isPublic">
          <el-radio-group v-model="form.isPublic">
            <el-radio :value="true">
              <div>
                <div>公开客户端</div>
                <div class="radio-desc">SPA / 移动 App，不需要 ClientSecret</div>
              </div>
            </el-radio>
            <el-radio :value="false">
              <div>
                <div>机密客户端</div>
                <div class="radio-desc">后端服务，需要 ClientSecret 认证</div>
              </div>
            </el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item label="回调地址" prop="redirectUris">
          <el-input
            v-model="form.redirectUris"
            type="textarea"
            :rows="3"
            placeholder="每行一个回调地址，如：&#10;https://app.example.com/callback"
          />
        </el-form-item>

        <el-form-item label="授权范围" prop="allowedScopes">
          <el-checkbox-group v-model="scopeList">
            <el-checkbox label="openid" value="openid" />
            <el-checkbox label="profile" value="profile" />
            <el-checkbox label="email" value="email" />
          </el-checkbox-group>
        </el-form-item>

        <el-form-item>
          <el-button type="primary" :loading="loading" native-type="submit">创建客户端</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 创建成功后显示 Secret -->
    <el-dialog v-model="showSecret" title="客户端创建成功" width="500px" :close-on-click-modal="false">
      <el-alert v-if="createdClient?.plainSecret" type="warning" :closable="false" show-icon style="margin-bottom: 16px">
        <template #title>请立即保存 Client Secret，关闭后将无法再次查看！</template>
      </el-alert>
      <el-descriptions :column="1" border>
        <el-descriptions-item label="Client ID">{{ createdClient?.clientId }}</el-descriptions-item>
        <el-descriptions-item v-if="createdClient?.plainSecret" label="Client Secret">
          <div style="display: flex; align-items: center; gap: 8px">
            <code style="flex: 1; word-break: break-all">{{ createdClient.plainSecret }}</code>
            <el-button size="small" @click="copySecret">复制</el-button>
            <el-button size="small" @click="downloadSecret">下载</el-button>
          </div>
        </el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag>草稿</el-tag>
        </el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button type="primary" @click="handleSecretSaved">我已保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { createClient, type ClientCreatedResult } from '@/api/client'

const router = useRouter()
const formRef = ref<FormInstance>()
const loading = ref(false)
const showSecret = ref(false)
const createdClient = ref<ClientCreatedResult | null>(null)

const scopeList = ref<string[]>(['openid', 'profile', 'email'])

const form = reactive({
  name: '',
  description: '',
  isPublic: true,
  redirectUris: ''
})

const rules: FormRules = {
  name: [
    { required: true, message: '请输入应用名称', trigger: 'blur' },
    { min: 2, max: 100, message: '名称长度在 2 到 100 个字符', trigger: 'blur' }
  ],
  redirectUris: [{ required: true, message: '请输入回调地址', trigger: 'blur' }]
}

const handleCreate = async () => {
  if (!formRef.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  loading.value = true
  try {
    const result = await createClient({
      name: form.name,
      description: form.description || undefined,
      redirectUris: form.redirectUris,
      allowedScopes: scopeList.value.join(' '),
      isPublic: form.isPublic
    })
    createdClient.value = result
    showSecret.value = true
  } catch {
    // 拦截器已处理
  } finally {
    loading.value = false
  }
}

const copySecret = () => {
  if (createdClient.value?.plainSecret) {
    navigator.clipboard.writeText(createdClient.value.plainSecret)
    ElMessage.success('已复制到剪贴板')
  }
}

const downloadSecret = () => {
  if (!createdClient.value?.plainSecret) return
  const content = `Client ID: ${createdClient.value.clientId}\nClient Secret: ${createdClient.value.plainSecret}`
  const blob = new Blob([content], { type: 'text/plain' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${createdClient.value.clientId}_secret.txt`
  a.click()
  URL.revokeObjectURL(url)
  ElMessage.success('已下载')
}

const handleSecretSaved = () => {
  showSecret.value = false
  router.push('/my-clients')
}
</script>

<style scoped>
.client-register-container {
  max-width: 640px;
  margin: 0 auto;
  padding: 30px 20px;
}

.page-title {
  font-size: 22px;
  font-weight: 600;
  color: #333;
  margin-bottom: 24px;
}

.register-card {
  border-radius: 12px;
}

.radio-desc {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}
</style>
