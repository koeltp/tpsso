FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# ========= 添加时区设置 =========
ENV TZ=Asia/Shanghai
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
# ================================

# 创建非 root 用户
RUN useradd -m -s /bin/bash appuser

# 安装 curl（健康检查需要）
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# 将 /app 目录的 ownership 转移给 appuser
RUN chown -R appuser:appuser /app

# 复制已发布的应用程序文件
COPY publish/ ./

USER appuser

EXPOSE 80 443

HEALTHCHECK --interval=30s --timeout=5s --retries=3 --start-period=10s \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "TPSSO.Admin.dll"]