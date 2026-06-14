using TPSSO.Application.Interfaces;

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

        // 检查第三方登录配置
        await ValidateOAuthProviders(configService, logger);

        // 检查邮件配置
        await ValidateSmtpConfig(configService, logger);

        logger.LogInformation("启动配置校验完成");
    }

    /// <summary>
    /// 检查已启用的第三方登录 Provider 是否配置了 ClientId/ClientSecret
    /// </summary>
    private static async Task ValidateOAuthProviders(IConfigService configService, ILogger logger)
    {
        var providers = new[] { "GitHub", "Google", "WeChat" };

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
