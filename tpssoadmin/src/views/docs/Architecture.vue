<template>
  <div class="doc-page">
    <h1>架构设计</h1>
    <p class="doc-desc">TPSSO 的系统架构与核心设计决策</p>

    <h2>服务职责</h2>
    <p>系统由两个独立后端服务和两个前端组成：</p>
    <el-table :data="services" stripe border size="small">
      <el-table-column prop="service" label="服务" width="160" />
      <el-table-column prop="role" label="职责" />
      <el-table-column prop="auth" label="认证方式" width="200" />
      <el-table-column prop="oidc" label="OpenIddict 角色" width="160" />
    </el-table>

    <h2>架构图</h2>
    <el-card shadow="never" class="code-card">
      <pre>  浏览器
  ├── auth.taipi.top  → authweb (Nginx → authapi)
  │                     tpssoauth → TPSSO.Auth (Cookie 认证)
  │                                OpenIddict Server
  │
  └── admin.taipi.top → adminweb (Nginx → adminapi)
                        tpssoadmin → TPSSO.Admin (Bearer 认证)
                                     OpenIddict Validation
                                     │
                         ┌───────────┘
                         ▼
                   MySQL + Redis</pre>
    </el-card>

    <h2>为什么是两个后端服务？</h2>
    <el-table :data="decisions" stripe border size="small">
      <el-table-column prop="question" label="设计决策" />
      <el-table-column prop="choice" label="选择" width="240" />
      <el-table-column prop="reason" label="理由" />
    </el-table>

    <h2>认证流程</h2>

    <h3>SSO 登录流程（tpssoauth）</h3>
    <el-card shadow="never" class="code-card">
      <pre>1. 用户访问 tpssoauth /login
2. POST /api/account/login → 设置认证 Cookie
3. 携带 Cookie 访问 /connect/authorize → 颁发授权码
4. 重定向回客户端 /callback?code=xxx</pre>
    </el-card>

    <h3>OAuth 授权码 + PKCE 流程（tpssoadmin / 第三方应用）</h3>
    <el-card shadow="never" class="code-card">
      <pre>1. 用户访问 tpssoadmin → 检测无 Token
2. 生成 PKCE 参数，重定向到 /connect/authorize
3. 未登录 → 跳转 tpssoauth 登录页
4. 登录成功 → 授权确认 → 颁发授权码
5. 回调 /callback?code=xxx
6. 前端用 code + code_verifier 换 Token
7. 后续请求带 Bearer Token</pre>
    </el-card>

    <h2>后端分层架构</h2>
    <el-card shadow="never" class="code-card">
      <pre>Auth/Admin (API 层)           → Controller：HTTP 请求处理
    ↓
TPSSO.Application (应用层)    → 接口定义、DTO、业务编排
    ↓
TPSSO.Infrastructure (基础设施层) → EF Core、服务实现、加密
    ↓
TPSSO.Domain (领域层)         → 实体、枚举、业务方法（零依赖）

外部依赖：
  TaiPi.Core (NuGet)          → ResponseResult、SearchPager、分页扩展等通用模型</pre>
    </el-card>
    <p>依赖方向只能从外向内，内层不知道外层的存在。TaiPi.Core 是外部 NuGet 包，被 Auth、Admin、Infrastructure 三层引用。</p>

    <h2>TaiPi.Core 基础类库</h2>
    <p>
      <el-link type="primary" href="https://github.com/koeltp/TPCore" target="_blank">GitHub 仓库</el-link>
      &nbsp;|&nbsp; NuGet：Taipi.Core v1.0.3 &nbsp;|&nbsp; MIT 协议
    </p>
    <p>TaiPi.Core 是项目的基础类库，提供通用响应模型和扩展方法，被 Auth、Admin、Infrastructure 三层引用。</p>
    <el-table :data="taipiCoreModules" stripe border size="small">
      <el-table-column prop="module" label="模块" width="200" />
      <el-table-column prop="classes" label="核心类" />
      <el-table-column prop="usage" label="TPSSO 中的使用" />
    </el-table>

    <h2>ClientApplication 独立实体设计</h2>
    <p>客户端实体没有继承 OpenIddictApplication，而是独立设计 + 审核通过后同步到 OpenIddict：</p>
    <el-table :data="clientDesign" stripe border size="small">
      <el-table-column prop="aspect" label="方面" width="160" />
      <el-table-column prop="independent" label="独立实体方案（已采用）" />
      <el-table-column prop="inherit" label="继承 OpenIddict 方案" />
    </el-table>

    <h2>字典配置系统</h2>
    <p>系统配置通过数据库字典管理，支持敏感项 AES 加密：</p>
    <el-table :data="dictCategories" stripe border size="small">
      <el-table-column prop="code" label="分类 Code" width="160" />
      <el-table-column prop="name" label="说明" width="120" />
      <el-table-column prop="items" label="包含项" />
    </el-table>
    <p style="margin-top: 12px">
      <el-tag type="info" size="small">ConfigService</el-tag>
      读取配置时优先从数据库获取，缺失时回退到 appsettings.json 默认值。
    </p>
  </div>
</template>

<script setup lang="ts">
const services = [
  { service: 'TPSSO.Auth', role: 'OIDC 协议、用户登录认证、Token 签发', auth: 'Cookie（自身登录）', oidc: 'Server' },
  { service: 'TPSSO.Admin', role: '用户/客户端/配置管理、API 服务', auth: 'Bearer Token（OpenIddict Validation）', oidc: 'Validation' },
  { service: 'tpssoauth', role: '登录、授权、注册页面', auth: 'Cookie + API', oidc: '-' },
  { service: 'tpssoadmin', role: '管理后台（OAuth 客户端）', auth: 'Bearer Token + OAuth PKCE', oidc: 'Client' }
]

const decisions = [
  { question: '后端拆分', choice: 'Auth + Admin 两个服务', reason: 'Auth 专注 OIDC 协议，Admin 专注业务管理，职责清晰' },
  { question: 'tpssoadmin 认证', choice: 'OAuth 授权码 + PKCE', reason: '作为自己的 OAuth 客户端，"吃自己的狗粮"' },
  { question: 'Admin 后端认证', choice: 'OpenIddict Validation', reason: '统一 Token 验证，不自行签发 JWT' },
  { question: '配置管理', choice: '数据库字典 + AES 加密', reason: '敏感配置加密存储，运行时可修改，无需重启' }
]

const taipiCoreModules = [
  { module: 'RQRS 响应模型', classes: 'ResponseResult<T>、PagerResponseResult<T>、StatusResponseResult', usage: '所有 API 返回值统一使用 ResponseResult<T>，分页使用 PagerResponseResult<T>' },
  { module: 'RQRS 请求模型', classes: 'SearchPager<T>、Pager、OrderByRQ', usage: '搜索接口入参使用 SearchPager<Condition>，支持分页和排序' },
  { module: 'Linq 扩展', classes: 'IEnumerableEx（Page、WhereIf、ParallelForEachAsync）', usage: 'WhereIf 简化条件查询，Page 简化内存分页' }
]

const clientDesign = [
  { aspect: 'Domain 依赖', independent: '零依赖，符合 Clean Architecture', inherit: '依赖 OpenIddict 包' },
  { aspect: '草稿/拒绝客户端', independent: '天然隔离，不出现在协议层', inherit: '会出现在 OAuth 协议表' },
  { aspect: 'OpenIddict 升级', independent: '不影响业务实体', inherit: '可能破坏继承关系' },
  { aspect: '数据同步', independent: '审核通过时同步到 OpenIddict', inherit: '零同步' }
]

const dictCategories = [
  { code: 'ThirdPartyLogin', name: '第三方登录', items: 'GitHub ClientId/ClientSecret/CallbackUrl' },
  { code: 'SmtpServer', name: 'SMTP 配置', items: 'Host/Port/Username/Password/SenderEmail/SenderName/UseSsl' },
  { code: 'Security', name: '安全配置', items: 'AccessTokenExpireMinutes/RefreshTokenExpireDays' },
  { code: 'PasswordPolicy', name: '密码策略', items: 'MinLength/RequireUppercase/Lowercase/Digit/NonAlphanumeric' },
  { code: 'Upload', name: '上传配置', items: 'MaxAvatarSizeKB/AllowedAvatarTypes' },
  { code: 'System', name: '系统配置', items: 'SiteName/AllowRegistration' }
]
</script>
