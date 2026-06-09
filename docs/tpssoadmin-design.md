# TPSSO 管理后台（tpssoadmin）设计文档

## 一、架构决策

### 1.1 独立前端 vs 同一前端

**决策**：tpssoadmin 作为独立前端项目，与 tpssoweb 分离部署。

**理由**：
- 管理代码不会泄露给普通用户，安全性更高
- 职责清晰：tpssoweb 面向用户，tpssoadmin 面向管理员
- 可独立部署、独立域名、独立扩展

**项目结构**：
```
TPSSO/
├── TPSSO/          # 后端 API
├── tpssoweb/       # 用户前端（sso.taipi.top）
├── tpssoadmin/     # 管理前端（ssoadmin.taipi.top）
└── docker/         # 部署配置
```

### 1.2 认证方案

**决策**：tpssoadmin 作为标准 OAuth 客户端接入 SSO，使用授权码 + PKCE 流程。

**对比过的方案**：

| 方案 | 原理 | 优点 | 缺点 |
|---|---|---|---|
| Cookie + localStorage 标记 | 登录后写标记，守卫读它 | 改动最小 | 标记与 Cookie 可能不同步 |
| JWT Bearer | 登录接口返回 JWT | 前端完全掌控 | 后端需改登录接口，认证方式不统一 |
| **OAuth 授权码 + PKCE** | 标准 OAuth 客户端流程 | 最规范，与第三方应用一致 | 实现复杂度较高 |

**选择 OAuth 的理由**：
- tpssoadmin 本身就是 SSO 的一个 OAuth 客户端，应该"吃自己的狗粮"
- 管理员和普通用户共享同一套账号体系，通过角色区分权限
- PKCE 保证授权码流程的安全性，无需客户端密钥

### 1.3 后端认证策略

**决策**：在 Program.cs 配置全局默认认证策略，同时接受 Bearer token 和 Cookie。

```csharp
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
            IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser()
        .Build();
});
```

**理由**：
- 避免在每个 Controller/Action 上重复写 `[Authorize(AuthenticationSchemes = "...")]`
- tpssoweb 带 Cookie 请求 → 通过
- tpssoadmin 带 Bearer token 请求 → 通过
- 管理接口的角色校验（`[Authorize(Roles = "Admin")]`）才是真正的访问控制

### 1.4 ClientApplication 实体设计

**决策**：独立业务实体 + 审核通过后同步到 OpenIddict，不继承 OpenIddict 实体。

**对比**：

| 方案 | 优点 | 缺点 |
|---|---|---|
| 继承 OpenIddictApplication | 零同步，少一次 JOIN | Domain 依赖 OpenIddict，违反零依赖原则；草稿客户端出现在协议表 |
| **独立实体 + 同步** | Domain 零依赖；草稿天然隔离；OpenIddict 升级不影响业务 | 数据存两份，需维护同步逻辑 |

**选择独立实体的理由**：
- 草稿/拒绝的客户端绝不能出现在 OAuth 协议层，继承方案做不到
- Domain 零依赖是项目硬约束
- 同步逻辑简单：审核通过时 `CreateAsync()`，撤回时 `DeleteAsync()`，更新时先删再建

---

## 二、OAuth 登录流程

```
用户访问 tpssoadmin
       ↓
检测无 token → 重定向到 SSO 授权端点
/connect/authorize?client_id=tpsso_admin_client
  &redirect_uri=http://localhost:3009/callback
  &response_type=code
  &scope=openid profile email roles
  &code_challenge=xxx        ← PKCE
  &code_challenge_method=S256
       ↓
用户在 SSO 登录页登录 → 授权确认
       ↓
回调到 tpssoadmin/callback?code=xxx
       ↓
前端用 code + code_verifier 调 /connect/token 换 token
       ↓
拿到 access_token → 存 localStorage
       ↓
后续 API 请求带 Authorization: Bearer {access_token}
       ↓
401 时清除 token → 重新跳转 SSO 登录
```

**关键实现**：
- PKCE：前端生成 `code_verifier`（64位随机字符串）和 `code_challenge`（SHA256 + Base64url），`code_verifier` 存 sessionStorage
- Token 存储：`access_token` 存 localStorage，`refresh_token` 存 localStorage（备用）
- 退出：清除本地 token → 跳转 SSO `/connect/logout` 端点

---

## 三、UI 风格

**决策**：Casdoor 风格，浅色系。

**布局**：
```
┌──────────────────────────────────────────────────────────┐
│ 🏠 TPSSO 管理后台                        管理员 ▾       │
├────────┬─────────────────────────────────────────────────┤
│        │                                                 │
│ 📊 仪表盘│  内容区                                        │
│        │                                                 │
│ 📋 待审核│  - 白色卡片                                    │
│        │  - 浅灰背景 (#f0f2f5)                            │
│ 🖥 客户端│  - 主色 #1890ff                                │
│        │                                                 │
└────────┴─────────────────────────────────────────────────┘
```

**配色**：
- 顶部导航栏：白色背景 + 底部 1px 边框
- 侧边栏：白色背景 + 右侧 1px 边框，选中项蓝色高亮背景
- 内容区：浅灰背景 `#f0f2f5`
- 主色：`#1890ff`（Ant Design Blue）
- 图标背景色块：橙色 `#fff7e6`、蓝色 `#e6f7ff`、绿色 `#f6ffed`

---

## 四、种子数据

### 4.1 角色
| 角色名 | 描述 |
|--------|------|
| Admin | 系统管理员 |

### 4.2 用户
| 邮箱 | 密码 | 角色 |
|------|------|------|
| admin@taipi.top | Admin@123 | Admin |
| tp@taipi.top | Admin@123 | 普通用户 |

### 4.3 OAuth 客户端
| Client ID | 类型 | 回调地址 | 权限 |
|-----------|------|---------|------|
| tpsso_spa_client | 公开（SPA） | localhost:3007/callback | authorization_code, email, profile |
| tpsso_admin_client | 公开（SPA） | localhost:3009/callback | authorization_code, email, profile, **roles** |

**注意**：tpsso_admin_client 必须包含 `roles` scope，否则令牌中不包含角色声明，`[Authorize(Roles = "Admin")]` 无法生效。

---

## 五、乐观并发控制

### 5.1 问题
更新客户端时，导航属性集合替换（`client.RedirectUris = newList`）导致 EF Core 变更跟踪冲突，抛出 `DbUpdateConcurrencyException`。

### 5.2 解决方案
1. **导航属性操作**：改为直接通过 DbContext 操作子实体（先 `RemoveRange` 旧记录，再 `AddRange` 新记录）
2. **并发控制**：添加 `RowVersion` 字段（`byte[]?`），使用 EF Core `IsRowVersion()` 配置

### 5.3 实现细节

| 层 | 文件 | 修改 |
|----|------|------|
| Domain | `ClientApplication.cs` | 添加 `RowVersion` 属性（`byte[]?`） |
| Infrastructure | `ApplicationDbContext.cs` | Fluent API 配置 `IsRowVersion()` |
| Application | `ClientResult.cs` | 添加 `RowVersion`（`string?`，Base64 编码） |
| Application | `UpdateClientModel.cs` | 添加 `RowVersion` 字段 |
| Infrastructure | `ClientService.cs` | UpdateAsync 中还原 RowVersion + 捕获并发异常 |

**工作流程**：
- 查询客户端 → 返回 `RowVersion`（Base64 字符串）
- 前端编辑提交 → 将 `RowVersion` 原样传回
- 后端更新时 → 将 RowVersion 还原到实体，EF Core 自动在 WHERE 子句中比对
- 并发冲突 → 返回"数据已被其他操作修改，请刷新后重试"

---

## 六、管理功能

### 6.1 第一期功能

| 功能 | API | 权限 |
|------|-----|------|
| 待审核列表 | `GET /api/client/pending` | Admin |
| 所有客户端 | `GET /api/client` | Admin |
| 审核通过 | `POST /api/client/{id}/approve` | Admin |
| 审核拒绝 | `POST /api/client/{id}/reject` | Admin |
| 删除客户端 | `DELETE /api/client/{id}` | 登录用户（所有者） |

### 6.2 未来扩展
- 用户管理（查看用户列表、分配角色、禁用账号）
- 系统配置
- 审计日志

---

## 七、部署

| 服务 | 容器名 | 端口 | 域名 |
|------|--------|------|------|
| tpssoapi | tpssoapi | 7044 | api.taipi.top |
| tpssoweb | tpssoweb | 3007 | sso.taipi.top |
| tpssoadmin | tpssoadmin | 3009 | ssoadmin.taipi.top |

tpssoadmin 的 Vite 代理配置：
```typescript
proxy: {
  '/api': { target: 'https://localhost:7044' },
  '/connect': { target: 'https://localhost:7044' }
}
```

---

## 八、文件清单

### 后端修改
- `TPSSO.Api/Program.cs` — 全局认证策略（Cookie + Bearer）
- `TPSSO.Api/Controllers/AccountController.cs` — `/me` 加 `[Authorize]`
- `TPSSO.Api/Controllers/ClientController.cs` — 管理接口加 `[Authorize(Roles = "Admin")]`
- `TPSSO.Application/Interfaces/IClientService.cs` — 新增 `GetAllAsync()`
- `TPSSO.Application/Models/ClientResult.cs` — 添加 `RowVersion`
- `TPSSO.Application/Models/UpdateClientModel.cs` — 添加 `RowVersion`
- `TPSSO.Domain/Entities/ClientApplication.cs` — 添加 `RowVersion`
- `TPSSO.Infrastructure/Data/ApplicationDbContext.cs` — `IsRowVersion()` 配置
- `TPSSO.Infrastructure/Services/ClientService.cs` — 并发控制 + `GetAllAsync()`
- `TPSSO.Infrastructure/Seeding/ClientSeeder.cs` — 管理员用户 + Admin 角色 + tpsso_admin_client

### 前端新增（tpssoadmin）
- `src/utils/oauth.ts` — OAuth 2.0 + PKCE 工具
- `src/views/Callback.vue` — OAuth 回调处理
- `src/views/Login.vue` — SSO 登录页
- `src/views/Dashboard.vue` — 仪表盘
- `src/views/PendingClients.vue` — 待审核列表
- `src/views/AllClients.vue` — 客户端管理
- `src/layouts/AdminLayout.vue` — Casdoor 风格布局
- `src/api/auth.ts` — 用户信息 API
- `src/api/client.ts` — 客户端管理 API
- `src/router/index.ts` — 路由 + OAuth 守卫
- `src/utils/api.ts` — Bearer token 拦截器
