FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

RUN useradd -m -s /bin/bash appuser

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /app/wwwroot/avatars && chown -R appuser:appuser /app

COPY publish/ ./

USER appuser

EXPOSE 80 443

HEALTHCHECK --interval=30s --timeout=5s --retries=3 --start-period=10s \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "TPSSO.Admin.dll"]
