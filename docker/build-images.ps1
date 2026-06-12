param(
    [string]$Target = "all",
    [switch]$SkipBuild
)

$Registry = "registry.cn-shenzhen.aliyuncs.com/tmd/sso"
$ProjectRoot = Split-Path -Parent $PSScriptRoot

# 镜像标签映射（与 docker-compose.yml 一致）
$Images = @{
    "authapi"  = "authapi"
    "adminapi" = "adminapi"
    "authweb"  = "authweb"
    "adminweb" = "adminweb"
}

# ────────────────────────────────────────────
# 构建函数
# ────────────────────────────────────────────

function Build-AuthApi {
    $srcDir = Join-Path $ProjectRoot "TPSSO\Auth"
    $publishDir = Join-Path $srcDir "publish"

    if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }

    Write-Host "  编译 TPSSO.Auth ..." -ForegroundColor Gray
    dotnet publish (Join-Path $srcDir "TPSSO.Auth.csproj") -c Release -o $publishDir --nologo
    if ($LASTEXITCODE -ne 0) { Write-Host "  Auth 编译失败" -ForegroundColor Red; return $false }

    Write-Host "  构建镜像 ${Registry}:authapi ..." -ForegroundColor Gray
    docker build -t "${Registry}:authapi" $srcDir
    if ($LASTEXITCODE -ne 0) { Write-Host "  Auth 镜像构建失败" -ForegroundColor Red; return $false }

    Remove-Item -Recurse -Force $publishDir
    return $true
}

function Build-AdminApi {
    $srcDir = Join-Path $ProjectRoot "TPSSO\Admin"
    $publishDir = Join-Path $srcDir "publish"

    if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }

    Write-Host "  编译 TPSSO.Admin ..." -ForegroundColor Gray
    dotnet publish (Join-Path $srcDir "TPSSO.Admin.csproj") -c Release -o $publishDir --nologo
    if ($LASTEXITCODE -ne 0) { Write-Host "  Admin 编译失败" -ForegroundColor Red; return $false }

    Write-Host "  构建镜像 ${Registry}:adminapi ..." -ForegroundColor Gray
    docker build -t "${Registry}:adminapi" $srcDir
    if ($LASTEXITCODE -ne 0) { Write-Host "  Admin 镜像构建失败" -ForegroundColor Red; return $false }

    Remove-Item -Recurse -Force $publishDir
    return $true
}

function Build-AuthWeb {
    $srcDir = Join-Path $ProjectRoot "tpssoauth"

    # tpssoauth 使用多阶段 Dockerfile（内含 npm ci + npm run build），无需本地构建
    Write-Host "  构建镜像 ${Registry}:authweb（多阶段构建）..." -ForegroundColor Gray
    docker build -t "${Registry}:authweb" $srcDir
    if ($LASTEXITCODE -ne 0) { Write-Host "  authweb 镜像构建失败" -ForegroundColor Red; return $false }

    return $true
}

function Build-AdminWeb {
    $srcDir = Join-Path $ProjectRoot "tpssoadmin"

    # tpssoadmin 需要先本地 npm run build，Dockerfile 只复制 dist
    Write-Host "  编译 tpssoadmin ..." -ForegroundColor Gray
    Push-Location $srcDir
    npm run build
    if ($LASTEXITCODE -ne 0) { Pop-Location; Write-Host "  tpssoadmin 编译失败" -ForegroundColor Red; return $false }
    Pop-Location

    Write-Host "  构建镜像 ${Registry}:adminweb ..." -ForegroundColor Gray
    docker build -t "${Registry}:adminweb" $srcDir
    if ($LASTEXITCODE -ne 0) { Write-Host "  adminweb 镜像构建失败" -ForegroundColor Red; return $false }

    return $true
}

# ────────────────────────────────────────────
# 主流程
# ────────────────────────────────────────────

$buildFuncs = @{
    "authapi"  = { Build-AuthApi }
    "adminapi" = { Build-AdminApi }
    "authweb"  = { Build-AuthWeb }
    "adminweb" = { Build-AdminWeb }
}

if ($Target -eq "all") {
    $targets = @("authapi", "adminapi", "authweb", "adminweb")
} else {
    $targets = $Target -split ","
}

foreach ($name in $targets) {
    if (-not $buildFuncs.ContainsKey($name)) {
        Write-Host "未知目标: $name（可选: authapi, adminapi, authweb, adminweb）" -ForegroundColor Red
        exit 1
    }

    Write-Host "`n========== 构建 $name ==========" -ForegroundColor Cyan
    $success = & $buildFuncs[$name]
    if (-not $success) { exit 1 }
}

Write-Host "`n所有镜像构建完成" -ForegroundColor Green
