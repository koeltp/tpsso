using AspNet.Security.OAuth.GitHub;
using Microsoft.Extensions.Options;
using TPSSO.Application.Interfaces;

namespace TPSSO.Auth.Options;

/// <summary>
/// 应用启动时从数据库字典读取 GitHub OAuth 配置，覆盖占位值
/// 注意：修改数据库配置后需重启服务才能生效（OAuth 中间件初始化后不支持运行时更新）
/// </summary>
public class GitHubPostConfigureOptions : IPostConfigureOptions<GitHubAuthenticationOptions>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GitHubPostConfigureOptions> _logger;

    public GitHubPostConfigureOptions(IServiceScopeFactory scopeFactory, ILogger<GitHubPostConfigureOptions> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void PostConfigure(string? name, GitHubAuthenticationOptions options)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var configService = scope.ServiceProvider.GetRequiredService<IConfigService>();

            var clientId = configService.GetStringAsync("GitHub", "ClientId").GetAwaiter().GetResult();
            var clientSecret = configService.GetStringAsync("GitHub", "ClientSecret").GetAwaiter().GetResult();

            _logger.LogInformation("GitHub PostConfigure: ClientId={ClientId}, ClientSecret={HasSecret}", clientId, string.IsNullOrEmpty(clientSecret) ? "空" : clientSecret[..6]);

            if (!string.IsNullOrEmpty(clientId))
            {
                options.ClientId = clientId;
            }
            if (!string.IsNullOrEmpty(clientSecret))
            {
                options.ClientSecret = clientSecret;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub PostConfigure 读取配置失败：{Message}", ex.Message);
        }
    }
}
