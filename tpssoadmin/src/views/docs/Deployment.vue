<template>
  <div class="doc-page">
    <h1>部署运维</h1>
    <p class="doc-desc">生产环境部署与运维指南</p>

    <h2>服务架构</h2>
    <el-card shadow="never" class="code-card">
      <pre>用户 → Cloudflare Tunnel
         ├── auth.taipi.top    → authweb (Nginx → authapi)
         └── admin.taipi.top   → adminweb (Nginx → adminapi)</pre>
    </el-card>
    <p>API 请求通过前端 Nginx 反向代理转发到后端，无需为 API 单独配置 Tunnel。</p>

    <h2>容器映射</h2>
    <el-table :data="containers" stripe border size="small">
      <el-table-column prop="name" label="容器名" width="140" />
      <el-table-column prop="port" label="端口" width="100" />
      <el-table-column prop="image" label="镜像标签" />
      <el-table-column prop="desc" label="说明" />
    </el-table>

    <h2>发布流程</h2>

    <h3>1. 修改生产配置</h3>
    <p>编辑 <code>docker/config/authapi/appsettings.Production.json</code>：</p>
    <el-card shadow="never" class="code-card">
      <pre>{
  "ConnectionStrings": {
    "DefaultConnection": "Server=mysql;Database=TPSSO_Auth;User=root;Password=密码;",
    "Redis": "redis:6379,password=密码"
  },
  "SsoOptions": {
    "LoginBaseUrl": "https://auth.taipi.top",
    "LoginPath": "/login",
    "ConsentPath": "/authorize"
  }
}</pre>
    </el-card>
    <p>编辑 <code>docker/config/adminapi/appsettings.Production.json</code>：</p>
    <el-card shadow="never" class="code-card">
      <pre>{
  "ConnectionStrings": {
    "DefaultConnection": "Server=mysql;Database=TPSSO_Auth;User=root;Password=密码;",
    "Redis": "redis:6379,password=密码"
  },
  "Auth": {
    "Issuer": "https://auth.taipi.top"
  }
}</pre>
    </el-card>

    <h3>2. 构建镜像</h3>
    <el-card shadow="never" class="code-card">
      <pre>cd docker

# 构建全部
.\build-images.ps1

# 只构建指定镜像
.\build-images.ps1 -Target authapi,adminapi</pre>
    </el-card>

    <h3>3. 推送到阿里云</h3>
    <el-card shadow="never" class="code-card">
      <pre># 首次登录
docker login registry.cn-shenzhen.aliyuncs.com

# 推送
.\push-aliyun.ps1

# 跳过登录直接推送
.\push-aliyun.ps1 -SkipLogin</pre>
    </el-card>

    <h3>4. 服务器部署</h3>
    <el-card shadow="never" class="code-card">
      <pre>docker compose up -d        # 首次部署
docker compose pull         # 拉取最新镜像
docker compose up -d        # 更新服务</pre>
    </el-card>

    <h2>Cloudflare Tunnel 部署</h2>
    <p>使用 Cloudflare Tunnel 暴露服务到公网，无需配置 Nginx + SSL：</p>
    <el-card shadow="never" class="code-card">
      <pre>cd docker\cloudflare
.\setup.ps1                # 首次部署（登录 + 创建 Tunnel + DNS + 启动）
.\setup.ps1 -SkipLogin     # 跳过登录</pre>
    </el-card>
    <el-table :data="tunnelRoutes" stripe border size="small" style="margin-top: 12px">
      <el-table-column prop="domain" label="域名" width="200" />
      <el-table-column prop="service" label="后端服务" width="140" />
    </el-table>

    <h2>OpenIddict 证书</h2>
    <p>生产环境需要加密证书和签名证书：</p>
    <el-card shadow="never" class="code-card">
      <pre># 加密证书
openssl req -x509 -newkey rsa:2048 -nodes \
  -keyout encryption.key -out encryption.crt -days 3650 \
  -subj "//CN=TPSSO-Encryption"
openssl pkcs12 -export -out encryption.pfx \
  -inkey encryption.key -in encryption.crt -passout pass:密码

# 签名证书
openssl req -x509 -newkey rsa:2048 -nodes \
  -keyout signing.key -out signing.crt -days 3650 \
  -subj "//CN=TPSSO-Signing"
openssl pkcs12 -export -out signing.pfx \
  -inkey signing.key -in signing.crt -passout pass:密码</pre>
    </el-card>
    <p>将 <code>encryption.pfx</code> 和 <code>signing.pfx</code> 放到 <code>config/certbot/</code> 目录。</p>

    <h2>Cloudflare 注意事项</h2>
    <el-table :data="cfNotes" stripe border size="small">
      <el-table-column prop="item" label="项目" width="200" />
      <el-table-column prop="value" label="配置" />
    </el-table>

    <h2>常用命令</h2>
    <el-card shadow="never" class="code-card">
      <pre>docker compose ps                    # 查看状态
docker compose logs -f authapi       # 查看日志
docker compose up -d authapi         # 重启单个服务
docker compose down                  # 停止所有服务
docker compose down -v               # 停止并删除数据卷</pre>
    </el-card>
  </div>
</template>

<script setup lang="ts">
const containers = [
  { name: 'authapi', port: '7086', image: 'tmd/sso:authapi', desc: 'Auth 认证服务' },
  { name: 'adminapi', port: '7089', image: 'tmd/sso:adminapi', desc: 'Admin 管理服务' },
  { name: 'authweb', port: '7087', image: 'tmd/sso:authweb', desc: '认证前端' },
  { name: 'adminweb', port: '7088', image: 'tmd/sso:adminweb', desc: '管理后台前端' }
]

const tunnelRoutes = [
  { domain: 'auth.taipi.top', service: 'authweb' },
  { domain: 'admin.taipi.top', service: 'adminweb' }
]

const cfNotes = [
  { item: 'SSL 模式', value: 'Full 或 Full (Strict)' },
  { item: 'Nginx X-Forwarded-Proto', value: '传递 $http_x_forwarded_proto，不使用 $scheme' },
  { item: '后端 KnownNetworks', value: '信任 Docker 网络段（172.16.0.0/12, 192.168.0.0/16）' },
  { item: '敏感文件', value: 'cert.pem 和 credentials.json 已被 .gitignore 忽略' }
]
</script>
