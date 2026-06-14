FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# 创建非 root 用户（UID 默认为 1000）
RUN useradd -m -s /bin/bash appuser

# 安装 curl（健康检查需要）
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# 将 /app 目录及其所有内容的 ownership 转移给 appuser
# 这样即使不预创建 avatars/logos 子目录，appuser 也能在挂载点上写入（挂载会覆盖权限，但挂载后权限由宿主机目录决定）
RUN chown -R appuser:appuser /app

# 复制已发布的应用程序文件
COPY publish/ ./

# 切换到非 root 用户运行应用
USER appuser

EXPOSE 80 443

HEALTHCHECK --interval=30s --timeout=5s --retries=3 --start-period=10s \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "TPSSO.Admin.dll"]