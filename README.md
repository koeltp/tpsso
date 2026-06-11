# TPSSO - 统一单点登录系统

基于 **OpenIddict** + **ASP.NET Core 9** 的统一认证平台，支持 OAuth 2.0 / OpenID Connect 协议。

采用**微服务架构**：Auth 服务专注 OIDC 协议与用户认证，Admin 服务负责后台管理，两个独立前端分别承担登录授权和管理后台的职责。

## 项目结构

```
TPSSO/
├── TPSSO/                        # 后端解决方案
│   ├── Auth/                     # 认证授权服务（OpenIddict Server + Cookie 认证）
│   ├── Admin/                    # 管理后台服务（OpenIddict Validation + Bearer 认证）
│   ├── TPSSO.Domain/             # 领域层：实体、枚举
│   ├── TPSSO.Application/        # 应用层：接口、DTO、Options
│   ├── TPSSO.Infrastructure/     # 基础设施层：EF Core、服务实现、种子数据
│   └── TPSSO.slnx
├── tpssoauth/                    # 认证前端（登录、授权、注册）
├── tpssoadmin/                   # 管理后台前端（用户/客户端/配置管理）
├── tpsso-app/                    # 第三方 SPA 应用示例（oidc-client-ts）
├── docker/                       # Docker 部署配置
│   ├── docker-compose.yml
│   ├── push-images.ps1
│   └── config/                   # 生产环境配置文件
└── README.md
```

## 技术栈

| 层 | 技术 |
|---|---|
| **后端** | ASP.NET Core 9, OpenIddict, Entity Framework Core, MySQL |
| **认证前端** | Vue 3, Vue Router, Pinia, Element Plus |
| **管理前端** | Vue 3, Vue Router, Pinia, Element Plus |
| **示例应用** | Vue 3, oidc-client-ts |

## 架构设计

### 服务职责

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           浏览器（用户）                                  │
│                                                                         │
│  http://localhost:3010          http://localhost:3009                    │
│  ┌──────────────────┐          ┌─────────────────────────┐              │
│  │   tpssoauth       │          │   tpssoadmin             │              │
│  │   (认证前端)       │          │   (管理后台前端)          │              │
│  │                   │          │                         │              │
│  │ 登录/授权/注册    │          │ 用户/客户端/配置管理      │              │
│  └────────┬─────────┘          └────────────┬────────────┘              │
│           │ Cookie + API                     │ Bearer Token + API        │
└───────────┼──────────────────────────────────┼───────────────────────────┘
            │                                  │
            ▼                                  ▼
   ┌─────────────────────┐          ┌──────────────────────┐
   │   TPSSO.Auth         │          │   TPSSO.Admin         │
   │   (认证授权服务)      │  OIDC    │   (管理后台服务)      │
   │                      │◄────────►│                      │
   │ OpenIddict Server    │ Discovery│ OpenIddict Validation│
   │ Cookie 认证          │          │ Bearer 认证          │
   │ :7044                │          │ :7045                │
   └──────────┬───────────┘          └──────────┬───────────┘
              │                                 │
              └────────────┬────────────────────┘
                           ▼
                    ┌──────────────┐
                    │    MySQL      │
                    │ TPSSO_Auth   │
                    └──────────────┘
```

### 服务对比

| | TPSSO.Auth | TPSSO.Admin |
|---|---|---|
| **职责** | OIDC 协议、用户登录认证、Token 签发 | 用户/客户端/配置管理、API 服务 |
| **认证方式** | Cookie（自身登录） | Bearer Token（OpenIddict Validation） |
| **OpenIddict 角色** | Server（签发 Token） | Validation（验证 Token） |
| **端口** | 7044 (HTTPS) | 7045 (HTTPS) |
| **对接前端** | tpssoauth | tpssoadmin |

### 后端分层架构

```
Auth/Admin (API 层)          → Controller：HTTP 请求处理、验证、响应
    ↓
TPSSO.Application (应用层)   → 接口定义、DTO、业务编排
    ↓
TPSSO.Infrastructure (基础设施层) → EF Core、服务实现、种子数据、加密
    ↓
TPSSO.Domain (领域层)        → 实体、枚举、业务方法
```

## 快速开始

### 环境要求

- .NET 9 SDK
- Node.js 18+
- MySQL 8.0+
- Docker（生产部署）

### 1. 数据库准备

创建 MySQL 数据库：

```sql
CREATE DATABASE TPSSO_Auth CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 2. 后端配置

修改 `TPSSO/Auth/appsettings.json` 和 `TPSSO/Admin/appsettings.json` 中的数据库连接字符串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TPSSO_Auth;User=root;Password=你的密码;"
  }
}
```

### 3. 启动后端服务

**Auth 认证服务**（必须先启动）：

```bash
cd TPSSO/Auth
dotnet run --launch-profile https
```

- HTTPS: `https://localhost:7044`
- HTTP: `http://localhost:5177`

**Admin 管理服务**：

```bash
cd TPSSO/Admin
dotnet run --launch-profile https
```

- HTTPS: `https://localhost:7045`
- HTTP: `http://localhost:5178`

> 开发模式下，Auth 服务启动时会自动重建数据库并写入种子数据（管理员账号、OAuth 客户端、字典配置等）。

### 4. 启动前端

**认证前端**（tpssoauth）：

```bash
cd tpssoauth
npm install
npm run dev
```

默认地址：`http://localhost:3010`

**管理后台前端**（tpssoadmin）：

```bash
cd tpssoadmin
npm install
npm run dev
```

默认地址：`http://localhost:3009`

**示例应用**（tpsso-app）：

```bash
cd tpsso-app
npm install
npm run dev
```

默认地址：`http://localhost:3007`

### 5. 开发账号

| 邮箱 | 密码 | 角色 |
|---|---|---|
| `admin@taipi.top` | `Admin@123` | 管理员 |
| `tp@taipi.top` | `Admin@123` | 普通用户 |

## 认证流程

### OAuth 2.0 授权码 + PKCE 流程

```
tpsso-app  ──→  Auth 服务  ──→  tpssoauth  ──→  Auth 服务  ──→  tpsso-app
（未登录）    （重定向）     （输入账号）    （颁发授权码）    （获取 token）
```

1. 用户访问第三方应用 `tpsso-app`，未登录
2. 自动重定向到 Auth 服务的 `/connect/authorize` 端点
3. 未认证 → 跳转到 `tpssoauth` 登录页
4. 用户输入凭据，POST 到 `/api/account/login`，设置认证 Cookie
5. 携带 Cookie 重定向回 `/connect/authorize` → 用户确认授权
6. 生成授权码，重定向回 `tpsso-app` 的 `/callback`
7. `oidc-client-ts` 用授权码交换 Access Token
8. 登录完成，跳转回应用首页

### 管理后台认证流程

```
tpssoadmin  ──→  Auth 服务  ──→  tpssoauth  ──→  Auth 服务  ──→  tpssoadmin
（未登录）     （重定向）     （输入账号）    （颁发授权码）    （获取 token）
```

tpssoadmin 本身也是一个 OAuth 客户端（`tpsso_admin_client`），使用授权码 + PKCE 流程获取 Token，后续 API 请求携带 Bearer Token 访问 Admin 服务。

## API 路由

### Auth 服务（:7044）

| 路由 | 方法 | 说明 |
|---|---|---|
| `/connect/authorize` | GET | OAuth 授权端点 |
| `/connect/token` | POST | Token 交换端点 |
| `/connect/userinfo` | GET | 用户信息端点 |
| `/connect/logout` | GET | 登出端点 |
| `/.well-known/openid-configuration` | GET | OIDC Discovery |
| `/.well-known/jwks` | GET | JWKS 公钥 |
| `/api/account/login` | POST | 表单登录 |
| `/api/account/logout` | POST | 表单登出 |
| `/api/account/me` | GET | 当前用户信息 |

### Admin 服务（:7045）

| 路由 | 方法 | 说明 |
|---|---|---|
| `/api/account/me` | GET | 当前用户信息 |
| `/api/client/search` | POST | 搜索客户端 |
| `/api/client/{id}` | GET | 客户端详情 |
| `/api/client` | POST | 创建客户端 |
| `/api/client/{id}` | PUT | 更新客户端 |
| `/api/client/{id}` | DELETE | 删除客户端 |
| `/api/client/{id}/submit` | POST | 提交审核 |
| `/api/client/{id}/approve` | POST | 审核通过 |
| `/api/client/{id}/reject` | POST | 审核拒绝 |
| `/api/user/search` | POST | 搜索用户 |
| `/api/user/{id}` | GET | 用户详情 |
| `/api/user/{id}/lock` | POST | 禁用用户 |
| `/api/user/{id}/unlock` | POST | 启用用户 |
| `/api/user/{id}/roles` | PUT | 修改用户角色 |
| `/api/user/{id}/reset-password` | POST | 重置密码 |
| `/api/user/roles` | GET | 角色列表 |
| `/api/dict` | GET | 获取字典配置（树形） |
| `/api/dict/types` | POST | 保存字典分类 |
| `/api/dict/types/{id}` | DELETE | 删除字典分类 |
| `/api/dict/types/{typeId}/items` | POST | 保存字典项 |
| `/api/dict/items/{id}` | DELETE | 删除字典项 |

## 开发指南

### 前端代理配置

开发时前端通过 Vite proxy 转发 API 请求，避免跨域问题：

**tpssoauth** — 所有请求代理到 Auth 服务：

```typescript
proxy: {
  '/api': { target: 'https://localhost:7044' },
  '/connect': { target: 'https://localhost:7044' }
}
```

**tpssoadmin** — API 请求代理到 Admin 服务，OAuth 请求代理到 Auth 服务：

```typescript
proxy: {
  '/api': { target: 'https://localhost:7045' },
  '/connect': { target: 'https://localhost:7044' }
}
```

### 环境变量

前端使用 `.env.development`（开发）和 `.env.production`（生产）管理环境配置：

| 变量 | 说明 | 示例 |
|---|---|---|
| `VITE_API_BASE_URL` | API 基础路径（走代理时留空） | `""` |
| `VITE_API_TARGET` | Admin 服务地址 | `https://localhost:7045` |
| `VITE_AUTH_TARGET` | Auth 服务地址 | `https://localhost:7044` |
| `VITE_OAUTH_CLIENT_ID` | OAuth 客户端 ID | `tpsso_admin_client` |
| `VITE_OAUTH_SCOPE` | OAuth Scope | `openid profile email roles` |

### 数据迁移

```bash
# 添加迁移
dotnet ef migrations add <MigrationName> \
  --project TPSSO/TPSSO.Infrastructure \
  --startup-project TPSSO/Auth \
  --output-dir Migrations

# 更新数据库
dotnet ef database update \
  --project TPSSO/TPSSO.Infrastructure \
  --startup-project TPSSO/Auth

# 删除数据库
dotnet ef database drop --force \
  --project TPSSO/TPSSO.Infrastructure \
  --startup-project TPSSO/Auth
```

> 开发模式下，Auth 服务启动时会自动重建数据库（`EnsureDeletedAsync` + `EnsureCreatedAsync`），无需手动迁移。

### 添加新的 API 端点

1. **Domain 层**：在 `TPSSO.Domain/Entities/` 定义实体
2. **Application 层**：在 `TPSSO.Application/Interfaces/` 定义服务接口，`Models/` 定义 DTO
3. **Infrastructure 层**：在 `TPSSO.Infrastructure/Services/` 实现服务
4. **API 层**：在 `Auth/Controllers/` 或 `Admin/Controllers/` 添加 Controller
5. **注册服务**：在对应项目的 `Program.cs` 中注册 `builder.Services.AddScoped<IXxxService, XxxService>()`

### 新增 OAuth 客户端

在 `TPSSO.Infrastructure/Seeding/ClientSeeder.cs` 中添加种子数据，或通过管理后台在线创建。

## 生产部署

### Docker Compose 部署

1. **构建并推送镜像**：

```bash
cd docker
.\push-images.ps1
```

2. **配置生产环境**：

修改 `docker/config/` 下的 `appsettings.Production.json`：

- 数据库连接字符串
- Auth Issuer 地址
- AES 加密密钥（`TPSSO__AES_KEY`、`TPSSO__AES_IV`）

3. **启动服务**：

```bash
docker compose up -d
```

### 服务映射

| 容器名 | 端口 | 说明 |
|---|---|---|
| `tpssoauth-api` | 7086 | Auth 认证服务 |
| `tpssoadmin-api` | 7089 | Admin 管理服务 |
| `tpssoauth` | 7087 | 认证前端 |
| `tpssoadmin` | 7088 | 管理后台前端 |

### 域名规划

| 域名 | 指向 | 说明 |
|---|---|---|
| `sso.taipi.top` | tpssoauth | 认证登录页 |
| `ssoapi.taipi.top` | tpssoauth-api | Auth API |
| `admin.taipi.top` | tpssoadmin | 管理后台 |
| `adminapi.taipi.top` | tpssoadmin-api | Admin API |

### Cloudflare + Nginx 注意事项

- Cloudflare SSL 模式设为 `Full` 或 `Full (Strict)`
- Nginx 配置需传递 `X-Forwarded-Proto` 头
- 后端需配置 `KnownNetworks` 信任 Docker 网络段的转发头
