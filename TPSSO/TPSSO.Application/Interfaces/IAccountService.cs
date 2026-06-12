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
    /// 用户登录（Cookie 认证），返回用户信息
    /// </summary>
    Task<ResponseResult<UserInfoResult>> LoginAsync(LoginModel model);

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
    /// 发送邮箱验证码
    /// </summary>
    Task<ResponseResult<bool>> SendCodeAsync(string email);

    /// <summary>
    /// 注册新用户
    /// </summary>
    Task<ResponseResult<bool>> RegisterAsync(RegisterModel model);

    /// <summary>
    /// 发送重置密码验证码
    /// </summary>
    Task<ResponseResult<bool>> SendResetCodeAsync(string email);

    /// <summary>
    /// 重置密码（忘记密码）
    /// </summary>
    Task<ResponseResult<bool>> ResetPasswordAsync(ResetPasswordModel model);
}
