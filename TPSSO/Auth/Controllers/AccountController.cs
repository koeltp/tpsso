using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;
using TPSSO.Domain.Entities;

namespace TPSSO.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class AccountController : ControllerBase
{
    ILogger<AccountController> _logger;
    private readonly IAccountService _accountService;
    private readonly SignInManager<User> _signInManager;

    public AccountController(ILogger<AccountController> logger, IAccountService accountService, SignInManager<User> signInManager)
    {
        _logger = logger;
        _accountService = accountService;
        _signInManager = signInManager;
    }

    /// <summary>
    /// 用户登录（Cookie 认证）
    /// </summary>
    [HttpPost("login")]
    public async Task<ResponseResult<LoginResult>> Login([FromBody] LoginModel model)
    {
        _logger.LogInformation("用户登录，用户名：{Username}。", model.Username);
        var data = await _accountService.LoginAsync(model);
        return new ResponseResult<LoginResult>(data);
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

    // ──────── 两步验证 ────────

    /// <summary>
    /// 两步验证登录（密码验证通过后）
    /// </summary>
    [HttpPost("login-2fa")]
    public async Task<ResponseResult<UserInfoResult>> LoginTwoFactor([FromBody] LoginTwoFactorModel model)
    {
        var data = await _accountService.LoginTwoFactorAsync(model);
        return ResponseResult<UserInfoResult>.Success(data);
    }

    /// <summary>
    /// 生成两步验证密钥和二维码信息
    /// </summary>
    [HttpPost("2fa/setup")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ResponseResult<TwoFactorSetupResult>> GenerateTwoFactorSetup()
    {
        var data = await _accountService.GenerateTwoFactorSetupAsync(User);
        return new ResponseResult<TwoFactorSetupResult>(data);
    }

    /// <summary>
    /// 启用两步验证（验证 TOTP 码确认绑定）
    /// </summary>
    [HttpPost("2fa/enable")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ResponseResult<TwoFactorSetupResult>> EnableTwoFactor([FromBody] TwoFactorVerifyModel model)
    {
        var data = await _accountService.EnableTwoFactorAsync(User, model.Code);
        return new ResponseResult<TwoFactorSetupResult>(data);
    }

    /// <summary>
    /// 禁用两步验证
    /// </summary>
    [HttpPost("2fa/disable")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<StatusResponseResult> DisableTwoFactor()
    {
        await _accountService.DisableTwoFactorAsync(User);
        return StatusResponseResult.Success("两步验证已禁用");
    }

    /// <summary>
    /// 重新生成恢复码
    /// </summary>
    [HttpPost("2fa/reset-codes")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ResponseResult<List<string>>> RegenerateRecoveryCodes()
    {
        var data = await _accountService.RegenerateRecoveryCodesAsync(User);
        return new ResponseResult<List<string>>(data);
    }

    // ──────── 外部登录绑定 ────────

    /// <summary>
    /// 获取当前用户的第三方登录绑定列表
    /// </summary>
    [HttpGet("external-logins")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ResponseResult<List<ExternalLoginProvider>>> GetExternalLogins()
    {
        var data = await _accountService.GetExternalLoginsAsync(User);
        return new ResponseResult<List<ExternalLoginProvider>>(data);
    }

    /// <summary>
    /// 发起绑定第三方登录（302重定向到第三方授权页）
    /// </summary>
    [HttpGet("external-login/{provider}/bind")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult BindExternalLogin(string provider)
    {
        // 构造绑定回调URL，通过 mode=bind 区分绑定和登录
        var callbackUrl = Url.Action("CallbackBind", "ExternalLogin", null, Request.Scheme);
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl);
        return Challenge(properties, provider);
    }

    /// <summary>
    /// 解绑第三方登录
    /// </summary>
    [HttpDelete("external-login/{provider}")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<StatusResponseResult> RemoveExternalLogin(string provider)
    {
        await _accountService.RemoveExternalLoginAsync(User, provider);
        return StatusResponseResult.Success("已解绑");
    }
}
