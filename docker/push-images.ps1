param(
    [switch]$SkipLogin
)

$Registry = "registry.cn-shenzhen.aliyuncs.com/tmd/sso"
$ProjectRoot = Split-Path -Parent $PSScriptRoot

if (-not $SkipLogin) {
    Write-Host "登录阿里云容器镜像服务 ..." -ForegroundColor Cyan
    docker login $Registry
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

# ============================================================
# 1. API 后端
# ============================================================
Write-Host "`n========== 构建 API ==========" -ForegroundColor Cyan
$apiDir = Join-Path $ProjectRoot "TPSSO\TPSSO.Api"
$publishDir = Join-Path $apiDir "publish"

# 清理上次 publish 目录
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }

# clean + 本地编译 .NET
dotnet clean (Join-Path $ProjectRoot "TPSSO\TPSSO.Api\TPSSO.Api.csproj") -c Release --nologo
dotnet publish (Join-Path $ProjectRoot "TPSSO\TPSSO.Api\TPSSO.Api.csproj") -c Release -o $publishDir --nologo
if ($LASTEXITCODE -ne 0) { Write-Host "API 编译失败" -ForegroundColor Red; exit 1 }

# 打 Docker 镜像并推送
docker build -t "${Registry}:tpssoapi" $apiDir
docker push "${Registry}:tpssoapi"
if ($LASTEXITCODE -ne 0) { exit 1 }

# 清理 publish 目录
Remove-Item -Recurse -Force $publishDir

# ============================================================
# 2. 用户前端（tpssoweb）
# ============================================================
Write-Host "`n========== 构建 Web ==========" -ForegroundColor Cyan
$webDir = Join-Path $ProjectRoot "tpssoweb"

Push-Location $webDir
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; Write-Host "Web 编译失败" -ForegroundColor Red; exit 1 }
Pop-Location

docker build -t "${Registry}:tpssoweb" $webDir
docker push "${Registry}:tpssoweb"
if ($LASTEXITCODE -ne 0) { exit 1 }

# ============================================================
# 3. 演示客户端（tpsso-app）
# ============================================================
Write-Host "`n========== 构建 App ==========" -ForegroundColor Cyan
$appDir = Join-Path $ProjectRoot "tpsso-app"

Push-Location $appDir
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; Write-Host "App 编译失败" -ForegroundColor Red; exit 1 }
Pop-Location

docker build -t "${Registry}:tpssoapp" $appDir
docker push "${Registry}:tpssoapp"
if ($LASTEXITCODE -ne 0) { exit 1 }

# ============================================================
# 4. 管理后台（tpssoadmin）
# ============================================================
Write-Host "`n========== 构建 Admin ==========" -ForegroundColor Cyan
$adminDir = Join-Path $ProjectRoot "tpssoadmin"

Push-Location $adminDir
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; Write-Host "Admin 编译失败" -ForegroundColor Red; exit 1 }
Pop-Location

docker build -t "${Registry}:tpssoadmin" $adminDir
docker push "${Registry}:tpssoadmin"
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "`n所有镜像推送成功" -ForegroundColor Green