using AspNet.Security.OAuth.GitHub;
using Microsoft.Extensions.Options;

namespace TPSSO.Auth.Options;

/// <summary>
/// GitHub OAuth 配置的 OptionsMonitorCache，禁用缓存
/// 每次获取配置都重新从数据库读取，确保管理后台修改后立即生效
/// OAuth 请求频率很低，性能影响可忽略
/// </summary>
public class GitHubNoCacheOptionsMonitorCache : IOptionsMonitorCache<GitHubAuthenticationOptions>
{
    public GitHubAuthenticationOptions GetOrAdd(string? name, Func<GitHubAuthenticationOptions> createOptions)
    {
        // 每次都重新创建，不使用缓存
        return createOptions();
    }

    public bool TryAdd(string? name, GitHubAuthenticationOptions options)
    {
        // 不缓存，直接返回 false
        return false;
    }

    public bool TryRemove(string? name) => true;

    public void Clear() { }
}
