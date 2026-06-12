# TPSSO 部署

## 正常发布流程

### 1. 修改生产配置

编辑 `docker/config/tpssoapi/appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=数据库地址;Database=TPSSO_Auth;User=用户名;Password=密码;"
  },
  "SsoOptions": {
    "LoginBaseUrl": "https://tpssoweb.taipi.top",
    "LoginPath": "/login"
  }
}
```

### 2. 编译 + 构建 + 推送镜像

在本地开发机执行（需要 .NET SDK 10 + Node.js 20）：

```powershell
# 首次需登录阿里云镜像仓库（登录一次后永久有效）
docker login registry.cn-shenzhen.aliyuncs.com/tmd/sso

cd docker
.\push-images.ps1
```

脚本自动完成：
- 编译 API (`dotnet publish`)
- 编译前端 (`npm run build`)
- 构建 3 个 Docker 镜像
- 推送到阿里云 `registry.cn-shenzhen.aliyuncs.com/tmd/sso`

### 3. 服务器部署

```bash
# 把 docker-compose.yml 和 config/ 目录上传到服务器后
docker compose up -d     # 自动拉取镜像 + 启动（首次自动创建网络）
```

### 4. 配置 Nginx 反向代理（仅首次）

在服务器上安装 Nginx，为三个域名配置 HTTPS，参考下方完整 Nginx 配置。

---

## 镜像信息

| 镜像 | 说明 |
|------|------|
| `registry.cn-shenzhen.aliyuncs.com/tmd/sso:tpssoapi` | API 后端 |
| `registry.cn-shenzhen.aliyuncs.com/tmd/sso:tpssoweb` | 用户前端 |
| `registry.cn-shenzhen.aliyuncs.com/tmd/sso:tpssoapp` | 演示客户端 |

```powershell
docker login registry.cn-shenzhen.aliyuncs.com/tmd/sso
```

## 服务架构

```
用户 → Nginx（服务器 80/443）
         ├── tpssoapi.taipi.top → 服务器 :7086 → tpssoapi 容器 :80
         ├── tpssoweb.taipi.top → 服务器 :7087 → tpssoweb 容器 :80
         │                                        ├── /api/*     → tpssoapi
         │                                        ├── /connect/* → tpssoapi
         │                                        └── /uploads/* → tpssoapi
         └── tpssoapp.taipi.top → 服务器 :7085 → tpssoapp 容器 :80
```

## Nginx 配置参考

```nginx
# /etc/nginx/conf.d/tpsso.conf
server {
    listen 443 ssl http2;
    server_name tpssoapi.taipi.top;
    ssl_certificate     /etc/nginx/ssl/tpssoapi.taipi.top.pem;
    ssl_certificate_key /etc/nginx/ssl/tpssoapi.taipi.top.key;
    location / { proxy_pass http://127.0.0.1:7086; proxy_set_header Host $host; proxy_set_header X-Real-IP $remote_addr; proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for; proxy_set_header X-Forwarded-Proto $scheme; }
}
server {
    listen 443 ssl http2;
    server_name tpssoweb.taipi.top;
    ssl_certificate     /etc/nginx/ssl/tpssoweb.taipi.top.pem;
    ssl_certificate_key /etc/nginx/ssl/tpssoweb.taipi.top.key;
    location / { proxy_pass http://127.0.0.1:7087; proxy_set_header Host $host; proxy_set_header X-Real-IP $remote_addr; proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for; proxy_set_header X-Forwarded-Proto $scheme; }
}
server {
    listen 443 ssl http2;
    server_name tpssoapp.taipi.top;
    ssl_certificate     /etc/nginx/ssl/tpssoapp.taipi.top.pem;
    ssl_certificate_key /etc/nginx/ssl/tpssoapp.taipi.top.key;
    location / { proxy_pass http://127.0.0.1:7085; proxy_set_header Host $host; proxy_set_header X-Real-IP $remote_addr; proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for; proxy_set_header X-Forwarded-Proto $scheme; }
}
server {
    listen 80;
    server_name tpssoapi.taipi.top tpssoweb.taipi.top tpssoapp.taipi.top;
    return 301 https://$host$request_uri;
}
```

## 其他命令

```bash
# 查看状态
docker compose ps

# 查看日志
docker compose logs -f tpssoapi

# 更新服务（重新拉取最新镜像）
docker compose up -d

# 停止
docker compose down
```
```bash
docker compose up -d authapi adminapi authweb adminweb

```
## 文件结构

```
docker/
├── config/tpssoapi/appsettings.Production.json   # 生产配置（唯一需要改的）
├── docker-compose.yml                              # 容器编排
├── push-images.ps1                                 # 一键推送脚本
└── README.md
```

其他 Dockerfile 和 nginx.conf 在各自项目目录下，构建脚本自动引用，一般无需手动修改。
# 加密证书
openssl req -x509 -newkey rsa:2048 -nodes \
  -keyout encryption.key \
  -out encryption.crt \
  -days 3650 \
  -subj "//CN=TPSSO-Encryption" \
  -addext "keyUsage = keyEncipherment, digitalSignature" \
  -addext "extendedKeyUsage = clientAuth, serverAuth"
# 导出加密证书
openssl pkcs12 -export -out encryption.pfx -inkey encryption.key -in encryption.crt -passout pass:xxx  
# 签名证书
openssl req -x509 -newkey rsa:2048 -nodes -keyout signing.key -out signing.crt -days 3650 -subj "//CN=TPSSO-Signing" 
# 导出签名证书
openssl pkcs12 -export -out signing.pfx -inkey signing.key -in signing.crt -passout pass:xxx