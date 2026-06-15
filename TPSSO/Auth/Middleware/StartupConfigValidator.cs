using Microsoft.EntityFrameworkCore;
using TPSSO.Application.Interfaces;
using TPSSO.Infrastructure.Data;

namespace TPSSO.Auth.Middleware;

/// <summary>
/// 启动时配置校验：检查关键配置是否就绪，未配置的项记录 Error 日志
/// 避免运行时才发现配置缺失导致 500
/// </summary>
public static class StartupConfigValidator
{
    public static async Task ValidateAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<IConfigService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 检查第三方登录配置
        await ValidateOAuthProviders(dbContext, configService, logger);

        // 检查邮件配置
        await ValidateSmtpConfig(configService, logger);

        logger.LogInformation("启动配置校验完成");
    }

    /// <summary>
    /// 从数据库动态查询已启用的 OAuth Provider，校验其 ClientId/ClientSecret 是否配置完整
    /// </summary>
    private static async Task ValidateOAuthProviders(ApplicationDbContext dbContext, IConfigService configService, ILogger logger)
    {
        // 查询 OAuth 父分类下所有已启用的子分类 Code（如 GitHub、Google、WeChat）
        var providers = await dbContext.DictTypes
            .Where(t => t.Parent != null && t.Parent.Code == "OAuth" && t.IsEnabled)
            .Select(t => t.Code)
            .ToListAsync();

        if (providers.Count == 0)
        {
            logger.LogInformation("配置校验：未启用任何第三方登录 Provider");
            return;
        }

        foreach (var provider in providers)
        {
            var isEnabled = await configService.GetBoolAsync(provider, "IsEnabled");
            if (!isEnabled) continue;

            var clientId = await configService.GetStringAsync(provider, "ClientId");
            var clientSecret = await configService.GetStringAsync(provider, "ClientSecret");

            if (string.IsNullOrEmpty(clientId))
            {
                logger.LogError("配置校验失败：{Provider} 已启用但 ClientId 未配置，第三方登录不可用", provider);
            }
            else if (clientId == "placeholder")
            {
                logger.LogError("配置校验失败：{Provider} 的 ClientId 仍为占位值，请通过管理后台配置", provider);
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                logger.LogError("配置校验失败：{Provider} 已启用但 ClientSecret 未配置，第三方登录不可用", provider);
            }
            else if (clientSecret == "placeholder")
            {
                logger.LogError("配置校验失败：{Provider} 的 ClientSecret 仍为占位值，请通过管理后台配置", provider);
            }

            if (!string.IsNullOrEmpty(clientId) && clientId != "placeholder"
                && !string.IsNullOrEmpty(clientSecret) && clientSecret != "placeholder")
            {
                logger.LogInformation("配置校验通过：{Provider} 已启用且配置完整", provider);
            }
        }
    }

    /// <summary>
    /// 检查 SMTP 邮件配置
    /// </summary>
    private static async Task ValidateSmtpConfig(IConfigService configService, ILogger logger)
    {
        var host = await configService.GetStringAsync("SmtpServer", "Host");
        var port = await configService.GetIntAsync("SmtpServer", "Port");

        if (string.IsNullOrEmpty(host))
        {
            logger.LogWarning("配置校验：Smtp Host 未配置，邮件发送功能不可用");
        }
        else
        {
            logger.LogInformation("配置校验通过：Smtp 已配置 ({Host}:{Port})", host, port);
        }
    }
}
