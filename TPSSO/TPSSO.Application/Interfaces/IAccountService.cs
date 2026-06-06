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
    /// 用户登录
    /// </summary>
    Task<ResponseResult<bool>> LoginAsync(LoginModel model);

    /// <summary>
    /// 用户登出
    /// </summary>
    Task<ResponseResult<bool>> LogoutAsync();

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<ResponseResult<UserInfoResult>> GetCurrentUserAsync(ClaimsPrincipal principal);

    /// <summary>
    /// 发送验证码邮件
    /// </summary>
    Task<ResponseResult<bool>> SendCodeAsync(SendCodeModel model);

    /// <summary>
    /// 用户注册
    /// </summary>
    Task<ResponseResult<bool>> RegisterAsync(RegisterModel model);
}
