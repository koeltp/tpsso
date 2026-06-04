# TPSSO - 统一单点登录系统

基于 **OpenIddict** + **ASP.NET Core 9** 的统一认证平台，支持 OAuth 2.0 / OpenID Connect 协议。

采用**前后端分离架构**：后端认证服务负责 OIDC 协议处理和用户认证，两个独立的前端 SPA 应用分别承担统一登录页和第三方应用接入的职责。

## 项目结构

```
TPSSO/
├── TPSSO.Api/          # 后端认证服务（ASP.NET Core 9 + OpenIddict）
├── tpsso-app/          # 第三方 SPA 应用示例（Vue 3 + oidc-client-ts）
├── tpssoweb/           # 统一登录页面（Vue 3）
└── .gitignore
```

## 技术栈

| 层             | 技术                                                           |
| ------------- | ------------------------------------------------------------ |
| **后端**        | **ASP.NET Core 9, OpenIddict, Entity Framework Core, M**ySQL |
| **前端（统一登录页）** | Vue 3, Vue Router, Axios                                     |
| **前端（应用示例）**  | Vue 3, Vue Router, oidc-client-ts, Axios                     |

## 架构设计

### 前后端分离

```
┌─────────────────────────────────────────────────────────────────────┐
│                        浏览器（用户）                                 │
│                                                                     │
│   http://localhost:3007              http://localhost:3008           │
│   ┌──────────────────┐             ┌─────────────────────────┐      │
│   │   tpsso-app       │             │   tpssoweb               │      │
│   │   (第三方应用)     │             │   (统一登录页)            │      │
│   │                   │             │                         │      │
│   │ oidc-client-ts    │             │ Vue 3 + Axios           │      │
│   │ 处理 OIDC 流程    │             │ 纯登录 UI，无业务逻辑    │      │
│   └────────┬─────────┘             └────────────┬────────────┘      │
│            │                                    │                   │
└────────────┼────────────────────────────────────┼───────────────────┘
             │  OIDC 协议                          │  Cookie + API
             │  (authorize/token)                  │  (login/logout)
             ▼                                    ▼
      ┌──────────────────────────────────────────────────┐
      │           TPSSO.Api（后端认证服务）                 │
      │                                                   │
      │  OpenIddict（OAuth 2.0 / OIDC 协议实现）            │
      │  ASP.NET Core Identity（用户认证 & Cookie 管理）    │
      │  MySQL（用户 & 客户端 & 令牌存储）                   │
      │                                                   │
      │  端点：                                            │
      │  GET  /connect/authorize    ─ 授权码颁发            │
      │  POST /connect/token        ─ 令牌交换              │
      │  GET  /connect/userinfo     ─ 用户信息              │
      │  POST /connect/logout       ─ 登出                  │
      │  POST /api/account/login    ─ 表单登录              │
      │  POST /api/account/logout   ─ 表单登出              │
      └──────────────────────────────────────────────────┘
```

### 认证流程分层

系统将认证流程拆分为三个独立服务，职责清晰：

| 服务            | 职责                  | 与其他服务的关系              |
| ------------- | ------------------- | --------------------- |
| **TPSSO.Api** | OIDC 协议处理、用户认证、令牌管理 | 不依赖任何前端服务             |
| **tpssoweb**  | 统一的登录/登出 UI 页面      | 通过 API + Cookie 与后端交互 |
| **tpsso-app** | 第三方应用示例，演示 OIDC 集成  | 通过 OIDC 协议与后端交互       |

这种设计的优势：

- **后端与前端完全解耦**：认证服务可独立部署和扩展
- **统一登录页可独立迭代**：登录 UI 变更不影响认证协议逻辑
- **第三方应用无需知道登录页实现**：只需遵循标准 OIDC 协议接入

## 启动方式

### 1. 后端认证服务

```bash
cd TPSSO/TPSSO.Api
dotnet run --launch-profile https
```

默认地址：

- HTTPS: `https://localhost:7044`
- HTTP:  `http://localhost:5177`

### 2. 统一登录页面（tpssoweb）

```bash
cd tpssoweb
npm install
npm run dev
```

默认地址：`http://localhost:3008`

### 3. 第三方应用示例（tpsso-app）

```bash
cd tpsso-app
npm install
npm run dev
```

默认地址：`http://localhost:3007`

## 服务映射

| 域名（生产）              | 地址                       | 说明    |
| ------------------- | ------------------------ | ----- |
| `ssoapi.taipi.top`  | `https://localhost:7044` | 认证服务器 |
| `sso.taipi.top`     | `http://localhost:3008`  | 统一登录页 |
| `ssotest.taipi.top` | `http://localhost:3007`  | 第三方应用 |

## 登录流程

```
tpsso-app  ──→  认证服务器  ──→  统一登录页  ──→  认证服务器  ──→  tpsso-app
（未登录）    （重定向）     （输入账号）    （颁发授权码）    （获取 token）
```

1. 用户访问 `tpsso-app`，未登录
2. 自动重定向到认证服务器的 `/connect/authorize` 端点
3. 未认证 → 跳转到 `tpssoweb` 统一登录页
4. 用户输入凭据，POST 到 `/api/account/login`，设置认证 Cookie
5. 携带 Cookie 重定向回 `/connect/authorize` → 认证成功
6. 生成授权码，重定向回 `tpsso-app` 的 `/callback`
7. `oidc-client-ts` 用授权码交换 Access Token
8. 登录完成，跳转回应用首页

## 开发账号

| 邮箱             | 密码          |
| -------------- | ----------- |
| `tp@taipi.top` | `Admin@123` |

## 数据迁移

```bash
cd TPSSO/TPSSO.Api
# 数据迁移：添加初始迁移
dotnet ef migrations add InitialCreate
# 数据迁移：更新数据库
dotnet ef database update

# 删除数据库
dotnet ef database drop -f
```

开发模式下，重启服务时会自动删除并重建数据库。
