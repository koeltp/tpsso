using AspNet.Security.OAuth.GitHub;
using Microsoft.Extensions.Options;
using TPSSO.Application.Interfaces;

namespace TPSSO.Auth.Options;

/// <summary>
/// 运行时从数据库字典读取 GitHub OAuth 配置，覆盖占位值
/// 管理后台修改 ClientId/ClientSecret 后无需重启服务即可生效
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

            _logger.LogInformation("GitHub PostConfigure: ClientId={ClientId}, ClientSecret={HasSecret}", clientId, string.IsNullOrEmpty(clientSecret) ? "空" : "已设置");

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
