using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using TPSSO.Auth.Options;

namespace TPSSO.Auth.Extensions;

/// <summary>
/// 第三方 OAuth 登录服务注册扩展
/// 新增 Provider 时只需在此类添加对应方法，并在 AddExternalLogins 中调用
/// </summary>
public static class ExternalLoginServiceExtensions
{
    /// <summary>
    /// 注册所有第三方 OAuth 登录 Provider
    /// Handler 无条件注册（ClientId 用占位值），实际配置通过 PostConfigureOptions 从数据库字典动态读取
    /// 使用自定义 IOptionsMonitorCache 禁用缓存，确保每次 OAuth 请求都从数据库读取最新配置
    /// </summary>
    public static AuthenticationBuilder AddExternalLogins(this AuthenticationBuilder builder)
    {
        // GitHub
        builder.AddGitHub(options =>
        {
            options.ClientId = "placeholder";
            options.ClientSecret = "placeholder";
            options.Scope.Add("user:email");
        });
        // PostConfigure：从数据库读取配置覆盖占位值
        builder.Services.AddSingleton<IPostConfigureOptions<GitHubAuthenticationOptions>, Options.GitHubPostConfigureOptions>();
        // 禁用缓存：每次 Get() 都触发 PostConfigure 从数据库读取最新值
        builder.Services.AddSingleton<IOptionsMonitorCache<GitHubAuthenticationOptions>, GitHubNoCacheOptionsMonitorCache>();

        // 新增 Provider 模板：
        // builder.AddGoogle(options => { options.ClientId = "placeholder"; options.ClientSecret = "placeholder"; });
        // builder.Services.AddSingleton<IPostConfigureOptions<GoogleAuthenticationOptions>, GooglePostConfigureOptions>();
        // builder.Services.AddSingleton<IOptionsMonitorCache<GoogleAuthenticationOptions>, GoogleNoCacheOptionsMonitorCache>();

        return builder;
    }
}
