<template>
  <div class="doc-page">
    <h1>API 参考</h1>
    <p class="doc-desc">TPSSO 所有 HTTP 端点一览</p>

    <h2>Auth 服务（:7044）</h2>

    <h3>OIDC 端点</h3>
    <el-table :data="authOidc" stripe border size="small">
      <el-table-column prop="route" label="路由" width="300" />
      <el-table-column prop="method" label="方法" width="80" />
      <el-table-column prop="desc" label="说明" />
    </el-table>

    <h3>账户接口</h3>
    <el-table :data="authAccount" stripe border size="small">
      <el-table-column prop="route" label="路由" width="280" />
      <el-table-column prop="method" label="方法" width="80" />
      <el-table-column prop="desc" label="说明" />
      <el-table-column prop="auth" label="认证" width="80" />
    </el-table>

    <h2>Admin 服务（:7045）</h2>

    <h3>账户接口</h3>
    <el-table :data="adminAccount" stripe border size="small">
      <el-table-column prop="route" label="路由" width="280" />
      <el-table-column prop="method" label="方法" width="80" />
      <el-table-column prop="desc" label="说明" />
      <el-table-column prop="auth" label="认证" width="80" />
    </el-table>

    <h3>客户端管理</h3>
    <el-table :data="adminClient" stripe border size="small">
      <el-table-column prop="route" label="路由" width="300" />
      <el-table-column prop="method" label="方法" width="80" />
      <el-table-column prop="desc" label="说明" />
      <el-table-column prop="auth" label="认证" width="80" />
    </el-table>

    <h3>用户管理</h3>
    <el-table :data="adminUser" stripe border size="small">
      <el-table-column prop="route" label="路由" width="280" />
      <el-table-column prop="method" label="方法" width="80" />
      <el-table-column prop="desc" label="说明" />
      <el-table-column prop="auth" label="认证" width="80" />
    </el-table>

    <h3>字典配置</h3>
    <el-table :data="adminDict" stripe border size="small">
      <el-table-column prop="route" label="路由" width="300" />
      <el-table-column prop="method" label="方法" width="80" />
      <el-table-column prop="desc" label="说明" />
      <el-table-column prop="auth" label="认证" width="80" />
    </el-table>

    <h2>统一响应格式</h2>
    <el-card shadow="never" class="code-card">
      <pre>{
  "code": 200,
  "message": "success",
  "data": { ... },
  "timestamp": 1718000000000
}</pre>
    </el-card>
  </div>
</template>

<script setup lang="ts">
const authOidc = [
  { route: '/connect/authorize', method: 'GET', desc: 'OAuth 授权端点' },
  { route: '/connect/token', method: 'POST', desc: 'Token 交换端点' },
  { route: '/connect/userinfo', method: 'GET', desc: '用户信息端点' },
  { route: '/connect/logout', method: 'GET', desc: '登出端点' },
  { route: '/.well-known/openid-configuration', method: 'GET', desc: 'OIDC Discovery' },
  { route: '/.well-known/jwks', method: 'GET', desc: 'JWKS 公钥' }
]

const authAccount = [
  { route: '/api/account/login', method: 'POST', desc: '表单登录', auth: '无' },
  { route: '/api/account/logout', method: 'POST', desc: '表单登出', auth: 'Cookie' },
  { route: '/api/account/me', method: 'GET', desc: '当前用户信息', auth: 'Cookie' },
  { route: '/api/account/register', method: 'POST', desc: '注册', auth: '无' },
  { route: '/api/account/send-code', method: 'POST', desc: '发送验证码', auth: '无' },
  { route: '/api/account/reset-password', method: 'POST', desc: '重置密码', auth: '无' },
  { route: '/api/account/github-login', method: 'POST', desc: 'GitHub OAuth 登录', auth: '无' },
  { route: '/api/account/upload-avatar', method: 'POST', desc: '上传头像', auth: 'Cookie' }
]

const adminAccount = [
  { route: '/api/account/me', method: 'GET', desc: '当前用户信息', auth: 'Bearer' },
  { route: '/api/account/upload-avatar', method: 'POST', desc: '上传头像', auth: 'Bearer' }
]

const adminClient = [
  { route: '/api/client/search', method: 'POST', desc: '搜索客户端', auth: 'Admin' },
  { route: '/api/client/my/search', method: 'POST', desc: '搜索我的客户端', auth: 'Bearer' },
  { route: '/api/client/{id}', method: 'GET', desc: '客户端详情', auth: 'Bearer' },
  { route: '/api/client', method: 'POST', desc: '创建客户端', auth: 'Bearer' },
  { route: '/api/client/{id}', method: 'PUT', desc: '更新客户端', auth: 'Bearer' },
  { route: '/api/client/{id}', method: 'DELETE', desc: '删除客户端', auth: 'Bearer' },
  { route: '/api/client/{id}/submit', method: 'POST', desc: '提交审核', auth: 'Bearer' },
  { route: '/api/client/{id}/approve', method: 'POST', desc: '审核通过', auth: 'Admin' },
  { route: '/api/client/{id}/reject', method: 'POST', desc: '审核拒绝', auth: 'Admin' },
  { route: '/api/client/{id}/regenerate-secret', method: 'POST', desc: '重置客户端密钥', auth: 'Bearer' }
]

const adminUser = [
  { route: '/api/user/search', method: 'POST', desc: '搜索用户', auth: 'Admin' },
  { route: '/api/user/{id}', method: 'GET', desc: '用户详情', auth: 'Admin' },
  { route: '/api/user/{id}/lock', method: 'POST', desc: '禁用用户', auth: 'Admin' },
  { route: '/api/user/{id}/unlock', method: 'POST', desc: '启用用户', auth: 'Admin' },
  { route: '/api/user/{id}/roles', method: 'PUT', desc: '修改用户角色', auth: 'Admin' },
  { route: '/api/user/{id}/reset-password', method: 'POST', desc: '重置密码', auth: 'Admin' },
  { route: '/api/user/roles', method: 'GET', desc: '角色列表', auth: 'Admin' }
]

const adminDict = [
  { route: '/api/dict', method: 'GET', desc: '获取字典配置（树形）', auth: 'Admin' },
  { route: '/api/dict/types', method: 'POST', desc: '保存字典分类', auth: 'Admin' },
  { route: '/api/dict/types/{id}', method: 'DELETE', desc: '删除字典分类', auth: 'Admin' },
  { route: '/api/dict/types/{typeId}/items', method: 'POST', desc: '保存字典项', auth: 'Admin' },
  { route: '/api/dict/items/{id}', method: 'DELETE', desc: '删除字典项', auth: 'Admin' }
]
</script>
