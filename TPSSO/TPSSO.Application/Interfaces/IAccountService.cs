using System.Security.Claims;
using TPSSO.Application.Models;

namespace TPSSO.Application.Interfaces;

/// <summary>
/// 账户业务服务接口
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// 用户登录，成功返回 true
    /// </summary>
    Task<bool> LoginAsync(LoginModel model);

    /// <summary>
    /// 用户登出
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// 获取当前用户信息，未登录返回 null
    /// </summary>
    Task<UserInfoResult?> GetCurrentUserAsync(ClaimsPrincipal principal);

    /// <summary>
    /// 发送验证码邮件
    /// </summary>
    Task SendCodeAsync(SendCodeModel model);

    /// <summary>
    /// 用户注册，成功返回 null，失败返回错误信息
    /// </summary>
    Task<string?> RegisterAsync(RegisterModel model);
}

/// <summary>
/// 用户信息返回结果
/// </summary>
public class UserInfoResult
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}
