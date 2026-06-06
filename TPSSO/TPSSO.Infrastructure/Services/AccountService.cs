using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
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

    public AccountService(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IVerificationCodeService verificationCodeService,
        IEmailService emailService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _verificationCodeService = verificationCodeService;
        _emailService = emailService;
    }

    public async Task<bool> LoginAsync(LoginModel model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            return false;

        await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
        return true;
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<UserInfoResult?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return null;

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return null;

        return new UserInfoResult
        {
            Username = user.UserName ?? "",
            Email = user.Email ?? "",
            AvatarUrl = ""
        };
    }

    public async Task SendCodeAsync(SendCodeModel model)
    {
        var code = await _verificationCodeService.GenerateAsync(model.Email, model.Purpose);
        await _emailService.SendVerificationCodeAsync(model.Email, code, 10);
    }

    public async Task<string?> RegisterAsync(RegisterModel model)
    {
        // 校验验证码
        var isValid = await _verificationCodeService.VerifyAsync(model.Email, model.Code, purpose: 0);
        if (!isValid)
            return "验证码无效或已过期";

        // 创建用户
        var user = new IdentityUser
        {
            UserName = model.Username,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return string.Join("；", result.Errors.Select(e => e.Description));

        return null; // 成功
    }
}
