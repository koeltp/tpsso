using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;

namespace TPSSO.Infrastructure.Services;

/// <summary>
/// 账户业务服务实现
/// </summary>
public class AccountService : IAccountService
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IVerificationCodeService _verificationCodeService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IVerificationCodeService verificationCodeService,
        IEmailService emailService,
        ILogger<AccountService> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _verificationCodeService = verificationCodeService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ResponseResult<bool>> LoginAsync(LoginModel model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            return ResponseResult<bool>.Error(401, "无效的用户名或密码");

        await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
        return new ResponseResult<bool>(true) { Code = 200, Message = "登录成功" };
    }

    public async Task<ResponseResult<bool>> LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return ResponseResult<bool>.Success();
    }

    public async Task<ResponseResult<UserInfoResult>> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return ResponseResult<UserInfoResult>.Unauthorized("未登录");

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return ResponseResult<UserInfoResult>.NotFound("用户不存在");

        var data = new UserInfoResult
        {
            Username = user.UserName ?? "",
            Email = user.Email ?? "",
            AvatarUrl = ""
        };

        return new ResponseResult<UserInfoResult>(data) { Code = 200, Message = "获取成功" };
    }

    public async Task<ResponseResult<bool>> SendCodeAsync(SendCodeModel model)
    {
        try
        {
            var code = await _verificationCodeService.GenerateAsync(model.Email, model.Purpose);
            await _emailService.SendVerificationCodeAsync(model.Email, code, 10);
            return ResponseResult<bool>.Success("验证码已发送");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送验证码失败，邮箱: {Email}", model.Email);
            return ResponseResult<bool>.InternalError("发送验证码失败，请稍后重试");
        }
    }

    public async Task<ResponseResult<bool>> RegisterAsync(RegisterModel model)
    {
        // 校验验证码
        var isValid = await _verificationCodeService.VerifyAsync(model.Email, model.Code, purpose: 0);
        if (!isValid)
            return ResponseResult<bool>.BadRequest("验证码无效或已过期");

        // 创建用户
        var user = new IdentityUser
        {
            UserName = model.Username,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return ResponseResult<bool>.BadRequest(string.Join("；", result.Errors.Select(e => e.Description)));

        return ResponseResult<bool>.Success("注册成功");
    }
}
