# TPSSO - 统一单点登录系统

基于 **OpenIddict** + **ASP.NET Core 9** 的统一认证平台，支持 OAuth 2.0 / OpenID Connect 协议。

## 快速开始

```bash
# 1. 克隆项目
git clone https://github.com/koeltp/tpsso.git && cd tpsso

# 2. 创建数据库
mysql -e "CREATE DATABASE TPSSO_Auth CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

# 3. 修改连接字符串（Auth 和 Admin 的 appsettings.json）
#    Server=localhost;Database=TPSSO_Auth;User=root;Password=你的密码;

# 4. 启动后端
cd TPSSO/Auth && dotnet run --launch-profile https   # :7044
cd TPSSO/Admin && dotnet run --launch-profile https   # :7045

# 5. 启动前端
cd tpssoauth && npm install && npm run dev            # :3010
cd tpssoadmin && npm install && npm run dev           # :3009
```

开发账号：`admin@taipi.top` / `Admin@123`（管理员）、`tp@taipi.top` / `Admin@123`（普通用户）

## 项目结构

```
TPSSO/
├── TPSSO/                        # 后端解决方案
│   ├── Auth/                     # 认证授权服务 (:7044)
│   ├── Admin/                    # 管理后台服务 (:7045)
│   ├── TPSSO.Domain/             # 领域层：实体、枚举
│   ├── TPSSO.Application/        # 应用层：接口、DTO
│   └── TPSSO.Infrastructure/     # 基础设施层：EF Core、服务实现
├── tpssoauth/                    # 认证前端 (:3010)
├── tpssoadmin/                   # 管理后台前端 (:3009)
└── docker/                       # Docker 部署配置
```

## 完整文档

启动 tpssoadmin 后访问 [http://localhost:3009/docs](http://localhost:3009/docs)，或部署后访问 `https://admin.taipi.top/docs`。

文档页面无需登录即可查看，包含：
- **快速开始** — 环境搭建、启动步骤
- **架构设计** — 服务职责、认证流程、数据模型
- **部署运维** — Docker、Cloudflare Tunnel、生产配置
- **开发规范** — 分层规则、DI 注册、代码风格
- **API 参考** — 全部 HTTP 端点一览
