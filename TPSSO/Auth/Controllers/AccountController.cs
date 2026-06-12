using Microsoft.AspNetCore.Mvc;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;

namespace TPSSO.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// 用户登录（Cookie 认证）
    /// </summary>
    [HttpPost("login")]
    public async Task<ResponseResult<UserInfoResult>> Login([FromBody] LoginModel model)
    {
        return await _accountService.LoginAsync(model);
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    [HttpPost("logout")]
    public async Task<ResponseResult<bool>> Logout()
    {
        return await _accountService.LogoutAsync();
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ResponseResult<UserInfoResult>> Me()
    {
        return await _accountService.GetCurrentUserAsync(User);
    }

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    [HttpPost("send-code")]
    public async Task<ResponseResult<bool>> SendCode([FromBody] SendCodeModel model)
    {
        return await _accountService.SendCodeAsync(model.Email);
    }

    /// <summary>
    /// 注册新用户
    /// </summary>
    [HttpPost("register")]
    public async Task<ResponseResult<bool>> Register([FromBody] RegisterModel model)
    {
        return await _accountService.RegisterAsync(model);
    }

    /// <summary>
    /// 发送重置密码验证码
    /// </summary>
    [HttpPost("send-reset-code")]
    public async Task<ResponseResult<bool>> SendResetCode([FromBody] SendCodeModel model)
    {
        return await _accountService.SendResetCodeAsync(model.Email);
    }

    /// <summary>
    /// 重置密码（忘记密码）
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<ResponseResult<bool>> ResetPassword([FromBody] ResetPasswordModel model)
    {
        return await _accountService.ResetPasswordAsync(model);
    }
}
