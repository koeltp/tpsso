FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# 创建非 root 用户（需要 root 权限）
RUN adduser --disabled-password --gecos "" appuser

# 安装 curl（用于健康检查）
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# 创建必要目录并设置权限
RUN mkdir -p /app/wwwroot/uploads && chown -R appuser:appuser /app

# 复制本地编译产物（由 push-images.ps1 提前 dotnet publish 到 publish/）
COPY publish/ ./

# 切换到非 root 用户
USER appuser

# 声明容器运行时监听的端口
EXPOSE 80 443

# 健康检查：每 30 秒检查一次，超时 5 秒，连续失败 3 次标记为不健康
HEALTHCHECK --interval=30s --timeout=5s --retries=3 --start-period=10s \
  CMD curl -f http://localhost/health || exit 1

# 启动应用
ENTRYPOINT ["dotnet", "TPSSO.Api.dll"]