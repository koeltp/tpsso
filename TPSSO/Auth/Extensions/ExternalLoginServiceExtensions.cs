using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using AuthOptions = TPSSO.Auth.Options;

namespace TPSSO.Auth.Extensions;

/// <summary>
/// 第三方 OAuth 登录服务注册扩展
/// 将所有第三方登录的 Handler 注册和 PostConfigure 集中管理
/// 新增 Provider 时只需在此类添加对应方法，并在 AddExternalLogins 中调用
/// </summary>
public static class ExternalLoginServiceExtensions
{
    /// <summary>
    /// 注册所有第三方 OAuth 登录 Provider
    /// Handler 无条件注册（ClientId 用占位值），实际配置通过 PostConfigureOptions 从数据库字典动态读取
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
        builder.Services.AddSingleton<IPostConfigureOptions<GitHubAuthenticationOptions>, AuthOptions.GitHubPostConfigureOptions>();

        // 新增 Provider 模板：
        // builder.AddGoogle(options => { options.ClientId = "placeholder"; options.ClientSecret = "placeholder"; });
        // builder.Services.AddSingleton<IPostConfigureOptions<GoogleAuthenticationOptions>, GooglePostConfigureOptions>();

        return builder;
    }
}
