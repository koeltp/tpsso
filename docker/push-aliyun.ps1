param(
    [string]$Target = "all",
    [switch]$SkipLogin
)

$Registry = "registry.cn-shenzhen.aliyuncs.com/tmd/sso"

if (-not $SkipLogin) {
    Write-Host "登录阿里云容器镜像服务 ..." -ForegroundColor Cyan
    docker login $Registry
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

# 镜像标签（与 docker-compose.yml 一致）
$Tags = @{
    "authapi"  = "authapi"
    "adminapi" = "adminapi"
    "authweb"  = "authweb"
    "adminweb" = "adminweb"
}

if ($Target -eq "all") {
    $targets = @("authapi", "adminapi", "authweb", "adminweb")
} else {
    $targets = $Target -split ","
}

foreach ($name in $targets) {
    if (-not $Tags.ContainsKey($name)) {
        Write-Host "未知目标: $name（可选: authapi, adminapi, authweb, adminweb）" -ForegroundColor Red
        exit 1
    }

    $tag = $Tags[$name]
    $image = "${Registry}:${tag}"

    Write-Host "推送 $image ..." -ForegroundColor Cyan
    docker push $image
    if ($LASTEXITCODE -ne 0) {
        Write-Host "推送 $image 失败" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n所有镜像推送完成" -ForegroundColor Green
