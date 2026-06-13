using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

namespace TPSSO.Auth.Extensions;

/// <summary>
/// 应用构建扩展方法，将 Program.cs 中的中间件管道按职责拆分
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 配置 ForwardedHeaders 中间件，正确获取反向代理的真实 IP 和协议
    /// </summary>
    public static IApplicationBuilder UseForwardedHeadersConfiguration(this IApplicationBuilder app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            KnownNetworks =
            {
                new IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12),
                new IPNetwork(System.Net.IPAddress.Parse("192.168.0.0"), 16),
                new IPNetwork(System.Net.IPAddress.Parse("127.0.0.0"), 8)
            }
        });
        return app;
    }

    /// <summary>
    /// 映射健康检查端点：/health（概览）、/health/db（数据库）、/health/cache（Redis）
    /// </summary>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/db", new HealthCheckOptions { Predicate = c => c.Tags.Contains("db") });
        app.MapHealthChecks("/health/cache", new HealthCheckOptions { Predicate = c => c.Tags.Contains("cache") });
        return app;
    }
}
