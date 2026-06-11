FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# 创建非 root 用户
RUN adduser --disabled-password --gecos "" appuser

# 安装 curl（健康检查用）
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# 设置权限
RUN mkdir -p /app/wwwroot && chown -R appuser:appuser /app

# 复制编译产物
COPY publish/ ./

USER appuser

EXPOSE 80 443

HEALTHCHECK --interval=30s --timeout=5s --retries=3 --start-period=10s \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "TPSSO.Auth.dll"]
