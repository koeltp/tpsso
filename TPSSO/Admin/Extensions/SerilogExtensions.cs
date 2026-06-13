using Serilog;

namespace TPSSO.Admin.Extensions;

/// <summary>
/// Serilog 日志配置扩展方法
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// 创建 Serilog 引导日志，用于捕获 Host 构建前的启动错误
    /// </summary>
    public static void CreateBootstrapLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                .Build())
            .CreateBootstrapLogger();
    }

    /// <summary>
    /// 配置 Host 使用 Serilog，从 appsettings 读取完整配置
    /// </summary>
    public static IHostBuilder UseSerilogFromConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfig) =>
        {
            loggerConfig.ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services);
        });
    }
}
