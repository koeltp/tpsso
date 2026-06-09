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
    /// 用户登录，返回 JWT Token 和用户信息
    /// </summary>
    Task<ResponseResult<LoginResult>> LoginAsync(LoginModel model);

    /// <summary>
    /// 用户登出
    /// </summary>
    Task<ResponseResult<bool>> LogoutAsync();

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<ResponseResult<UserInfoResult>> GetCurrentUserAsync(ClaimsPrincipal principal);

    /// <summary>
    /// 修改个人信息
    /// </summary>
    Task<ResponseResult<bool>> UpdateProfileAsync(ClaimsPrincipal principal, UpdateProfileModel model);

    /// <summary>
    /// 更新用户头像 URL
    /// </summary>
    Task<ResponseResult<string>> UpdateAvatarUrlAsync(ClaimsPrincipal principal, string avatarUrl);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task<ResponseResult<bool>> ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordModel model);

    /// <summary>
    /// 发送验证码邮件
    /// </summary>
    Task<ResponseResult<bool>> SendCodeAsync(SendCodeModel model);

    /// <summary>
    /// 用户注册
    /// </summary>
    Task<ResponseResult<bool>> RegisterAsync(RegisterModel model);

    /// <summary>
    /// 使用 Refresh Token 刷新 Access Token
    /// </summary>
    Task<ResponseResult<LoginResult>> RefreshTokenAsync(string refreshToken);
}
