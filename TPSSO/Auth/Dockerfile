FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# 安装 tzdata（虽然 base 已有，但显式安装更安全）
RUN apt-get update && apt-get install -y tzdata curl && \
    rm -rf /var/lib/apt/lists/*

# 设置时区（方式一：软链接 + 配置文件）
ENV TZ=Asia/Shanghai
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# 创建非 root 用户（无需重复安装 curl，已装过）
RUN useradd -m -s /bin/bash appuser

# 设置目录权限
RUN mkdir -p /app/certbot && chown -R appuser:appuser /app

# 复制编译产物
COPY publish/ ./

USER appuser
EXPOSE 80 443

HEALTHCHECK --interval=30s --timeout=5s --retries=3 --start-period=10s \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "TPSSO.Auth.dll"]