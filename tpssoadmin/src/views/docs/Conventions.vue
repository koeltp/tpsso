<template>
  <div class="doc-page">
    <h1>开发规范</h1>
    <p class="doc-desc">TPSSO 项目的编码约定与开发流程</p>

    <h2>分层架构</h2>
    <el-card shadow="never" class="code-card">
      <pre>Auth/Admin (API 层)           → Controller：HTTP 请求处理
    ↓
TPSSO.Application (应用层)    → 接口定义、DTO、业务编排
    ↓
TPSSO.Infrastructure (基础设施层) → EF Core、服务实现、加密
    ↓
TPSSO.Domain (领域层)         → 实体、枚举、业务方法

外部 NuGet：
  TaiPi.Core                  → ResponseResult、SearchPager 等通用模型
  （被 Auth、Admin、Infrastructure 引用，Domain 不引用）</pre>
    </el-card>
    <p>依赖只能从外向内，内层不知道外层的存在。Domain 层零外部依赖，不引用 TaiPi.Core。</p>

    <h2>各层放什么</h2>
    <el-table :data="layerRules" stripe border size="small">
      <el-table-column prop="layer" label="层" width="180" />
      <el-table-column prop="content" label="放什么" />
      <el-table-column prop="example" label="示例" />
    </el-table>

    <h2>新增 API 端点步骤</h2>
    <el-steps direction="vertical" :active="5" finish-status="success">
      <el-step title="Domain 层" description="在 Entities/ 定义实体（含行为方法）" />
      <el-step title="Application 层" description="在 Interfaces/ 定义服务接口，Models/ 定义 DTO" />
      <el-step title="Infrastructure 层" description="在 Services/ 实现服务，Data/ 配置 DbContext" />
      <el-step title="API 层" description="在 Controllers/ 添加 Controller" />
      <el-step title="DI 注册" description="在 Program.cs 注册 builder.Services.AddScoped&lt;IXxxService, XxxService&gt;()" />
    </el-steps>

    <h2>Controller 规范</h2>
    <el-card shadow="never" class="code-card">
      <pre>[HttpPost("search")]
public async Task&lt;IActionResult&gt; Search([FromBody] ClientSearchCondition condition)
{
    var result = await _clientService.SearchAsync(condition);
    return Ok(result);
}</pre>
    </el-card>
    <el-table :data="controllerRules" stripe border size="small" style="margin-top: 12px">
      <el-table-column prop="rule" label="规则" />
      <el-table-column prop="detail" label="说明" />
    </el-table>

    <h2>DI 注册顺序</h2>
    <el-card shadow="never" class="code-card">
      <pre>// 1. Options 配置
builder.Services.Configure&lt;SsoOptions&gt;(
    builder.Configuration.GetSection(SsoOptions.SectionName));

// 2. 基础设施
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// 3. 应用服务
builder.Services.AddScoped&lt;IConfigService, ConfigService&gt;();
builder.Services.AddScoped&lt;IEmailService, EmailService&gt;();
builder.Services.AddScoped&lt;IAccountService, AccountService&gt;();</pre>
    </el-card>

    <h2>前端代理配置</h2>
    <h3>tpssoauth — 代理到 Auth 服务</h3>
    <el-card shadow="never" class="code-card">
      <pre>proxy: {
  '/api': { target: 'https://localhost:7044' },
  '/connect': { target: 'https://localhost:7044' }
}</pre>
    </el-card>
    <h3>tpssoadmin — 代理到 Admin 服务</h3>
    <el-card shadow="never" class="code-card">
      <pre>proxy: {
  '/api': { target: 'https://localhost:7045' },
  '/avatars': { target: 'https://localhost:7045' }
}</pre>
    </el-card>
    <p>tpssoadmin 不需要代理 <code>/connect</code>，OAuth 流程通过整页跳转完成。</p>

    <h2>环境变量</h2>
    <h3>tpssoauth</h3>
    <el-table :data="authEnvVars" stripe border size="small">
      <el-table-column prop="var" label="变量" width="200" />
      <el-table-column prop="dev" label="开发" width="200" />
      <el-table-column prop="prod" label="生产" />
    </el-table>
    <h3>tpssoadmin</h3>
    <el-table :data="adminEnvVars" stripe border size="small">
      <el-table-column prop="var" label="变量" width="220" />
      <el-table-column prop="dev" label="开发" width="200" />
      <el-table-column prop="prod" label="生产" />
    </el-table>

    <h2>代码风格</h2>
    <el-table :data="codeStyle" stripe border size="small">
      <el-table-column prop="item" label="项目" width="200" />
      <el-table-column prop="rule" label="规则" />
    </el-table>
  </div>
</template>

<script setup lang="ts">
const layerRules = [
  { layer: 'Domain', content: '实体、枚举、常量', example: 'ClientApplication, DictItem, ClientStatus' },
  { layer: 'Application', content: '服务接口、DTO、Options', example: 'IClientService, LoginModel, SsoOptions' },
  { layer: 'Infrastructure', content: '服务实现、DbContext、种子数据、工具', example: 'ClientService, ApplicationDbContext, AesEncryption' },
  { layer: 'Auth / Admin', content: 'Controller、Program.cs、appsettings', example: 'AccountController, AuthorizationController' }
]

const controllerRules = [
  { rule: '只注入 Service 接口', detail: '不直接注入 UserManager/SignInManager（AuthorizationController 除外）' },
  { rule: '请求参数用 [FromBody] Model', detail: '接口方法接收 Model 对象，不拆成单个参数' },
  { rule: 'Service 返回 ResponseResult<T>', detail: 'Controller 根据 result.Code 决定 HTTP 状态码' },
  { rule: 'AuthorizationController 不抽 Service', detail: 'OpenIddict 协议深度绑定 HttpContext' },
  { rule: '路由前缀', detail: '业务 API 用 api/[controller]，OIDC 端点用 connect/' }
]

const authEnvVars = [
  { var: 'VITE_API_BASE_URL', dev: '""', prod: '""' },
  { var: 'VITE_API_TARGET', dev: 'https://localhost:7044', prod: '不配置' },
  { var: 'VITE_ADMIN_URL', dev: 'http://localhost:3009', prod: 'https://admin.taipi.top' }
]

const adminEnvVars = [
  { var: 'VITE_API_BASE_URL', dev: '""', prod: '""' },
  { var: 'VITE_API_TARGET', dev: 'https://localhost:7045', prod: '不配置' },
  { var: 'VITE_SSO_URL', dev: 'http://localhost:3010', prod: 'https://auth.taipi.top' },
  { var: 'VITE_OAUTH_CLIENT_ID', dev: 'tpsso_admin_client', prod: 'tpsso_admin_client' },
  { var: 'VITE_OAUTH_SCOPE', dev: 'openid profile email roles', prod: 'openid profile email roles' }
]

const codeStyle = [
  { item: '中文注释', rule: '所有公开类/方法/属性必须有中文注释，说明"为什么"而非"做什么"' },
  { item: '命名空间', rule: '与目录一致，文件放在什么目录，命名空间就是什么' },
  { item: '实体属性', rule: '用 public set，不过度封装；字符串用 = string.Empty! 初始化' },
  { item: 'Options 类', rule: '必须定义 const string SectionName 用于 DI 注册' },
  { item: 'DbContext', rule: '字符串字段必须设 HasMaxLength（MySQL 索引长度限制）' },
  { item: '代码清理', rule: '每次改动后主动删除未使用的变量、导入、文件' }
]
</script>
