using System.Security.Claims;
using Taipi.Core.RQRS;
using TPSSO.Application.Models;

namespace TPSSO.Application.Interfaces;

/// <summary>
/// 账户业务服务接口
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// 用户登录（Cookie 认证），返回登录结果（可能需要2FA）
    /// </summary>
    Task<LoginResult> LoginAsync(LoginModel model);

    /// <summary>
    /// 用户登出
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<UserInfoResult> GetCurrentUserAsync(ClaimsPrincipal principal);

    /// <summary>
    /// 修改个人信息
    /// </summary>
    Task UpdateProfileAsync(ClaimsPrincipal principal, UpdateProfileModel model);

    /// <summary>
    /// 更新用户头像 URL
    /// </summary>
    Task UpdateAvatarUrlAsync(ClaimsPrincipal principal, string avatarUrl);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordModel model);

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    Task SendCodeAsync(string email);

    /// <summary>
    /// 注册新用户
    /// </summary>
    Task RegisterAsync(RegisterModel model);

    /// <summary>
    /// 发送重置密码验证码
    /// </summary>
    Task SendResetCodeAsync(string email);

    /// <summary>
    /// 重置密码（忘记密码）
    /// </summary>
    Task ResetPasswordAsync(ResetPasswordModel model);

    /// <summary>
    /// 生成两步验证密钥和二维码信息
    /// </summary>
    Task<TwoFactorSetupResult> GenerateTwoFactorSetupAsync(ClaimsPrincipal principal);

    /// <summary>
    /// 启用两步验证（验证 TOTP 码确认绑定）
    /// </summary>
    Task<TwoFactorSetupResult> EnableTwoFactorAsync(ClaimsPrincipal principal, string code);

    /// <summary>
    /// 禁用两步验证
    /// </summary>
    Task DisableTwoFactorAsync(ClaimsPrincipal principal);

    /// <summary>
    /// 两步验证登录（密码验证通过后）
    /// </summary>
    Task<UserInfoResult> LoginTwoFactorAsync(LoginTwoFactorModel model);

    /// <summary>
    /// 生成新的恢复码（已启用2FA时重新生成）
    /// </summary>
    Task<List<string>> RegenerateRecoveryCodesAsync(ClaimsPrincipal principal);
}
