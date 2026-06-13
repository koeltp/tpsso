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
    ILogger<AccountController> _logger;
    private readonly IAccountService _accountService;

    public AccountController(ILogger<AccountController> logger, IAccountService accountService)
    {
        _logger = logger;
        _accountService = accountService;
    }

    /// <summary>
    /// 用户登录（Cookie 认证）
    /// </summary>
    [HttpPost("login")]
    public async Task<ResponseResult<UserInfoResult>> Login([FromBody] LoginModel model)
    {
        _logger.LogInformation("用户登录，用户名：{Username}。", model.Username);
        var data = await _accountService.LoginAsync(model);
        return ResponseResult<UserInfoResult>.Success(data);
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    [HttpPost("logout")]
    public async Task<StatusResponseResult> Logout()
    {
        _logger.LogInformation("用户登出。");
        await _accountService.LogoutAsync();
        return StatusResponseResult.Success("已登出");
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ResponseResult<UserInfoResult>> Me()
    {
        var data = await _accountService.GetCurrentUserAsync(User);
        return ResponseResult<UserInfoResult>.Success(data);
    }

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    [HttpPost("send-code")]
    public async Task<StatusResponseResult> SendCode([FromBody] SendCodeModel model)
    {
        await _accountService.SendCodeAsync(model.Email);
        return StatusResponseResult.Success("验证码已发送");
    }

    /// <summary>
    /// 注册新用户
    /// </summary>
    [HttpPost("register")]
    public async Task<StatusResponseResult> Register([FromBody] RegisterModel model)
    {
        await _accountService.RegisterAsync(model);
        return StatusResponseResult.Success("注册成功");
    }

    /// <summary>
    /// 发送重置密码验证码
    /// </summary>
    [HttpPost("send-reset-code")]
    public async Task<StatusResponseResult> SendResetCode([FromBody] SendCodeModel model)
    {
        await _accountService.SendResetCodeAsync(model.Email);
        return StatusResponseResult.Success("验证码已发送");
    }

    /// <summary>
    /// 重置密码（忘记密码）
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<StatusResponseResult> ResetPassword([FromBody] ResetPasswordModel model)
    {
        await _accountService.ResetPasswordAsync(model);
        return StatusResponseResult.Success("密码已重置");
    }
}
