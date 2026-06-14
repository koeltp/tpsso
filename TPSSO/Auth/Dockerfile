FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# 创建非 root 用户
RUN useradd -m -s /bin/bash appuser

# 安装 curl（健康检查用）
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# 设置权限
RUN mkdir -p /app/wwwroot && chown -R appuser:appuser /app

# 复制编译产物
COPY publish/ ./
RUN mkdir -p /app/certbot && chown appuser:appuser /app/certbot
USER appuser

EXPOSE 80 443

HEALTHCHECK --interval=30s --timeout=5s --retries=3 --start-period=10s \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "TPSSO.Auth.dll"]
