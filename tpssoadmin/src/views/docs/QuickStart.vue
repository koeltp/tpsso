<template>
  <div class="doc-page">
    <h1>快速开始</h1>
    <p class="doc-desc">从零开始搭建 TPSSO 开发环境</p>

    <h2>环境要求</h2>
    <el-table :data="requirements" stripe border size="small">
      <el-table-column prop="name" label="工具" width="160" />
      <el-table-column prop="version" label="最低版本" width="120" />
      <el-table-column prop="note" label="说明" />
    </el-table>

    <h2>1. 克隆项目</h2>
    <el-card shadow="never" class="code-card">
      <pre>git clone https://github.com/koeltp/tpsso.git
cd tpsso</pre>
    </el-card>

    <h2>2. 创建数据库</h2>
    <el-card shadow="never" class="code-card">
      <pre>CREATE DATABASE TPSSO_Auth CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;</pre>
    </el-card>

    <h2>3. 配置后端</h2>
    <p>修改 <code>TPSSO/Auth/appsettings.json</code> 和 <code>TPSSO/Admin/appsettings.json</code> 中的连接字符串：</p>
    <el-card shadow="never" class="code-card">
      <pre>{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TPSSO_Auth;User=root;Password=你的密码;",
    "Redis": "localhost:6379"
  }
}</pre>
    </el-card>

    <h2>4. 数据库迁移</h2>
    <p>首次启动 Auth 服务会自动迁移，但开发中修改实体后需要手动生成迁移：</p>
    <el-card shadow="never" class="code-card">
      <pre># 生成迁移（在 TPSSO/ 目录下执行）
dotnet ef migrations add 迁移名称 --project TPSSO.Infrastructure --startup-project Auth --context ApplicationDbContext

# 更新数据库
dotnet ef database update --project TPSSO.Infrastructure --startup-project Auth --context ApplicationDbContext

# 删除最近一次迁移（未应用到数据库时）
dotnet ef migrations remove --project TPSSO.Infrastructure --startup-project Auth --context ApplicationDbContext

# 重置数据库（删除后重新创建，会丢失所有数据）
dotnet ef database drop --force --project TPSSO.Infrastructure --startup-project Auth --context ApplicationDbContext</pre>
    </el-card>

    <h2>5. 启动后端</h2>
    <p>先启动 Auth 服务：</p>
    <el-card shadow="never" class="code-card">
      <pre>cd TPSSO/Auth
dotnet run --launch-profile https        # https://localhost:7044</pre>
    </el-card>
    <p>再启动 Admin 服务：</p>
    <el-card shadow="never" class="code-card">
      <pre>cd TPSSO/Admin
dotnet run --launch-profile https        # https://localhost:7045</pre>
    </el-card>

    <h2>6. 启动前端</h2>
    <el-card shadow="never" class="code-card">
      <pre># 认证前端
cd tpssoauth
npm install && npm run dev               # http://localhost:3010

# 管理后台
cd tpssoadmin
npm install && npm run dev               # http://localhost:3009</pre>
    </el-card>

    <h2>7. 开发账号</h2>
    <el-table :data="accounts" stripe border size="small">
      <el-table-column prop="email" label="邮箱" width="200" />
      <el-table-column prop="password" label="密码" width="140" />
      <el-table-column prop="role" label="角色" />
    </el-table>

    <h2>项目结构</h2>
    <el-card shadow="never" class="code-card">
      <pre>TPSSO/
├── TPSSO/                        # 后端解决方案
│   ├── Auth/                     # 认证授权服务 (:7044)
│   ├── Admin/                    # 管理后台服务 (:7045)
│   ├── TPSSO.Domain/             # 领域层：实体、枚举
│   ├── TPSSO.Application/        # 应用层：接口、DTO
│   └── TPSSO.Infrastructure/     # 基础设施层：EF Core、服务实现
├── tpssoauth/                    # 认证前端 (:3010)
├── tpssoadmin/                   # 管理后台前端 (:3009)
└── docker/                       # Docker 部署配置</pre>
    </el-card>
  </div>
</template>

<script setup lang="ts">
const requirements = [
  { name: '.NET SDK', version: '9.0', note: '后端运行时' },
  { name: 'Node.js', version: '18', note: '前端构建' },
  { name: 'MySQL', version: '8.0', note: '数据库' },
  { name: 'Redis', version: '6.0', note: '缓存（验证码、Session）' }
]

const accounts = [
  { email: 'admin@taipi.top', password: 'Admin@123', role: '系统管理员' },
  { email: 'tp@taipi.top', password: 'Admin@123', role: '普通用户' }
]
</script>
