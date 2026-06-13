<template>
  <div class="doc-page">
    <h1>第三方登录</h1>
    <p class="doc-desc">接入 GitHub、Google、微信等第三方 OAuth 登录</p>

    <h2>架构设计</h2>
    <p>TPSSO 采用 ASP.NET Core Identity 的外部登录体系，通过 <code>UserLogins</code> 表关联第三方账号，支持多 Provider 动态启用。</p>

    <h3>核心设计原则</h3>
    <el-table :data="principles" stripe border size="small">
      <el-table-column prop="principle" label="原则" width="200" />
      <el-table-column prop="description" label="说明" />
    </el-table>

    <h2>数据模型</h2>
    <p>Identity 自带的 <code>UserLogins</code> 表存储第三方关联关系：</p>
    <el-table :data="loginTableFields" stripe border size="small">
      <el-table-column prop="field" label="字段" width="200" />
      <el-table-column prop="description" label="说明" />
      <el-table-column prop="example" label="示例" width="200" />
    </el-table>
    <p style="margin-top: 12px">同一个用户可以关联多个第三方账号，通过 <code>LoginProvider + ProviderKey</code> 唯一定位。</p>

    <h2>配置管理</h2>
    <p>第三方登录配置存储在数据库字典表中，支持管理后台动态修改：</p>
    <el-card shadow="never" class="code-card">
      <pre>DictType: OAuth (父分类)
  ├── DictType: GitHub (子分类, Code="GitHub")
  │     ├── DictItem: ClientId       → GitHub OAuth App 的 Client ID
  │     ├── DictItem: ClientSecret   → GitHub OAuth App 的 Client Secret (加密存储)
  │     └── DictItem: IsEnabled      → 是否启用 (true/false)
  ├── DictType: Google (子分类, Code="Google")
  │     └── ...
  └── DictType: WeChat (子分类, Code="WeChat")
        └── ...</pre>
    </el-card>
    <p style="margin-top: 12px">
      <el-tag type="success" size="small">动态生效</el-tag>
      所有配置项（ClientId、ClientSecret、IsEnabled）均通过 <code>IPostConfigureOptions</code> 从数据库字典动态读取，管理后台修改后无需重启 Auth 服务即可生效。
    </p>

    <h2>登录流程</h2>
    <el-card shadow="never" class="code-card">
      <pre>  用户                    前端 tpssoauth              后端 Auth              GitHub
   │                          │                        │                     │
   │  点击"GitHub 登录"       │                        │                     │
   │ ──────────────────────►  │                        │                     │
   │                          │  GET /api/external-login/providers            │
   │                          │ ─────────────────────► │                     │
   │                          │  返回已启用 Provider    │                     │
   │                          │ ◄─────────────────────  │                     │
   │                          │                        │                     │
   │  整页跳转                 │                        │                     │
   │  GET /api/external-login/GitHub?returnUrl=xxx     │                     │
   │ ────────────────────────────────────────────────► │                     │
   │                          │                        │  Challenge          │
   │                          │                        │ ──────────────────► │
   │  ◄──────────────────────────────────────────────────────────────────  │
   │  302 重定向到 GitHub 授权页                          │                     │
   │                          │                        │                     │
   │  用户授权                 │                        │                     │
   │ ──────────────────────────────────────────────────────────────────► │
   │                          │                        │  回调 /signin-github│
   │                          │                        │ ◄────────────────── │
   │                          │                        │                     │
   │                          │                        │  /api/external-login/callback
   │                          │                        │  查找/创建用户       │
   │                          │                        │  签发 Cookie        │
   │  ◄─────────────────────────────────────────────── │                     │
   │  302 重定向回前端（带 returnUrl）                    │                     │</pre>
    </el-card>

    <h2>用户关联策略</h2>
    <p>回调时按以下优先级处理用户关联：</p>
    <el-table :data="strategies" stripe border size="small">
      <el-table-column prop="priority" label="优先级" width="80" />
      <el-table-column prop="condition" label="匹配条件" width="240" />
      <el-table-column prop="action" label="处理方式" />
    </el-table>

    <h2>API 接口</h2>
    <el-table :data="apis" stripe border size="small">
      <el-table-column prop="method" label="方法" width="80" />
      <el-table-column prop="path" label="路径" width="300" />
      <el-table-column prop="description" label="说明" />
    </el-table>

    <h2>配置 GitHub 登录</h2>

    <h3>1. 创建 GitHub OAuth App</h3>
    <p>前往 <el-link type="primary" href="https://github.com/settings/developers" target="_blank">GitHub Developer Settings</el-link> 创建 OAuth App：</p>
    <el-table :data="githubConfig" stripe border size="small">
      <el-table-column prop="field" label="配置项" width="200" />
      <el-table-column prop="value" label="值" />
    </el-table>

    <h3>2. 在管理后台配置</h3>
    <p>进入 <strong>系统管理 → 字典配置 → OAuth → GitHub</strong>，填入：</p>
    <el-table :data="githubDictItems" stripe border size="small">
      <el-table-column prop="key" label="Key" width="200" />
      <el-table-column prop="value" label="值" />
      <el-table-column prop="note" label="说明" />
    </el-table>
    <p style="margin-top: 12px">
      <el-tag type="success" size="small">无需重启</el-tag>
      所有配置（ClientId、ClientSecret、IsEnabled）均从数据库字典动态读取，管理后台修改后立即生效。
    </p>

    <h2>新增第三方登录</h2>
    <p>以 Google 为例，新增一个 Provider 只需以下步骤：</p>
    <el-table :data="addProviderSteps" stripe border size="small">
      <el-table-column prop="step" label="步骤" width="60" />
      <el-table-column prop="action" label="操作" />
      <el-table-column prop="needCode" label="是否改代码" width="100" />
    </el-table>
  </div>
</template>

<script setup lang="ts">
const principles = [
  { principle: '多 Provider 支持', description: 'Identity 自带 UserLogins 表天然支持多第三方关联，同一用户可关联 GitHub、Google、微信等多个账号' },
  { principle: '静态注册 + 动态配置', description: 'Provider Handler 在 Program.cs 静态注册（加一行代码），ClientId/ClientSecret/IsEnabled 全部从数据库字典动态读取，管理后台修改后无需重启' },
  { principle: '统一回调处理', description: '所有 Provider 共用同一个 Callback 方法，查找/创建/关联用户的逻辑只写一次' },
  { principle: '前端动态渲染', description: '前端通过 /api/external-login/providers 获取已启用列表，动态渲染登录按钮，新增 Provider 不需要改前端' }
]

const loginTableFields = [
  { field: 'LoginProvider', description: '第三方标识（如 GitHub、Google）', example: 'GitHub' },
  { field: 'ProviderKey', description: '第三方用户唯一 ID', example: '12345678' },
  { field: 'ProviderDisplayName', description: '显示名称', example: 'GitHub' },
  { field: 'UserId', description: '关联的本地用户 ID', example: 'guid-xxx' }
]

const strategies = [
  { priority: '1', condition: 'UserLogins 表匹配', action: '直接登录（已关联的第三方账号）' },
  { priority: '2', condition: '邮箱匹配已有用户', action: '自动关联外部登录 + 登录（需第三方返回邮箱）' },
  { priority: '3', condition: '无匹配', action: '自动创建新用户 + 关联外部登录 + 登录' }
]

const apis = [
  { method: 'GET', path: '/api/external-login/providers', description: '获取已启用的第三方登录 Provider 列表' },
  { method: 'GET', path: '/api/external-login/{provider}', description: '发起第三方登录（302 重定向到第三方授权页）' },
  { method: 'GET', path: '/api/external-login/callback', description: '统一回调处理（查找/创建用户，签发 Cookie）' }
]

const githubConfig = [
  { field: 'Application name', value: 'TPSSO' },
  { field: 'Homepage URL', value: 'https://auth.taipi.top' },
  { field: 'Authorization callback URL', value: 'https://auth.taipi.top/signin-github' }
]

const githubDictItems = [
  { key: 'ClientId', value: 'GitHub OAuth App 的 Client ID', note: '从 GitHub OAuth App 页面获取' },
  { key: 'ClientSecret', value: 'GitHub OAuth App 的 Client Secret', note: '加密存储' },
  { key: 'IsEnabled', value: 'true', note: '启用 GitHub 登录' }
]

const addProviderSteps = [
  { step: '1', action: '管理后台添加 DictType/DictItem 配置（OAuth 父分类下新增子分类，含 ClientId/ClientSecret/IsEnabled）', needCode: '否' },
  { step: '2', action: '安装对应 NuGet 包（如 AspNet.Security.OAuth.Google）', needCode: '是' },
  { step: '3', action: 'Program.cs 添加 .AddGoogle() 注册代码 + 创建对应的 PostConfigureOptions 类', needCode: '是' },
  { step: '4', action: '重启 Auth 服务（仅首次注册 Handler 需要，后续修改配置无需重启）', needCode: '否' }
]
</script>
