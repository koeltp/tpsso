using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TPSSO.Application.Exceptions;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;
using TPSSO.Domain.Entities;

namespace TPSSO.Infrastructure.Services;

/// <summary>
/// 账户业务服务实现
/// </summary>
public class AccountService : IAccountService
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IVerificationCodeService _verificationCodeService;
    private readonly IEmailService _emailService;
    private readonly IConfigService _configService;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IVerificationCodeService verificationCodeService,
        IEmailService emailService,
        IConfigService configService,
        ILogger<AccountService> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _verificationCodeService = verificationCodeService;
        _emailService = emailService;
        _configService = configService;
        _logger = logger;
    }

    public async Task<UserInfoResult> LoginAsync(LoginModel model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            _logger.LogWarning("登录失败：用户名或密码错误，Username={Username}", model.Username);
            throw new AppException(AppCodes.InvalidCredentials, "无效的用户名或密码");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("登录失败：账号已被禁用，UserId={UserId}", user.Id);
            throw new AppException(AppCodes.AccountDisabled, "该账号已被禁用，请联系管理员");
        }

        await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
        _logger.LogInformation("登录成功：UserId={UserId}, Username={Username}", user.Id, user.UserName);

        return await BuildUserInfoAsync(user);
    }

    public async Task LogoutAsync()
    {
        var userId = _userManager.GetUserId(_signInManager.Context?.User!);
        await _signInManager.SignOutAsync();
    }

    public async Task<UserInfoResult> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            throw new AppException(AppCodes.InvalidCredentials, "未登录");

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            throw new AppException(AppCodes.UserNotFound, "用户不存在");

        return await BuildUserInfoAsync(user);
    }

    public async Task UpdateProfileAsync(ClaimsPrincipal principal, UpdateProfileModel model)
    {
        if (principal.Identity?.IsAuthenticated != true)
            throw new AppException(AppCodes.InvalidCredentials, "未登录");

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            throw new AppException(AppCodes.UserNotFound, "用户不存在");

        user.NickName = model.NickName;
        user.AvatarUrl = model.AvatarUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(AppCodes.InvalidParameter, string.Join("；", result.Errors.Select(e => e.Description)));
    }

    public async Task UpdateAvatarUrlAsync(ClaimsPrincipal principal, string avatarUrl)
    {
        if (principal.Identity?.IsAuthenticated != true)
            throw new AppException(AppCodes.InvalidCredentials, "未登录");

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            throw new AppException(AppCodes.UserNotFound, "用户不存在");

        user.AvatarUrl = avatarUrl;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(AppCodes.InvalidParameter, string.Join("；", result.Errors.Select(e => e.Description)));
    }

    public async Task ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordModel model)
    {
        if (principal.Identity?.IsAuthenticated != true)
            throw new AppException(AppCodes.InvalidCredentials, "未登录");

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            throw new AppException(AppCodes.UserNotFound, "用户不存在");

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
            throw new BadRequestException(AppCodes.InvalidParameter, string.Join("；", result.Errors.Select(e => e.Description)));
    }

    public async Task SendCodeAsync(string email)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            throw new BadRequestException(AppCodes.EmailExists, "该邮箱已注册");

        if (!await _verificationCodeService.CanSendAsync(email))
            throw new BadRequestException(AppCodes.SendTooFrequent, "发送过于频繁，请60秒后再试");

        var expireMinutes = await _configService.GetIntAsync("System", "VerificationCodeExpireMinutes", 5);
        var code = await _verificationCodeService.GenerateAsync(email, expireMinutes);
        await _emailService.SendVerificationCodeAsync(email, code, expireMinutes);
    }

    public async Task RegisterAsync(RegisterModel model)
    {
        if (!await _verificationCodeService.VerifyAsync(model.Email, model.Code))
            throw new BadRequestException(AppCodes.CodeExpired, "验证码错误或已过期");

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
            throw new BadRequestException(AppCodes.EmailExists, "该邮箱已注册");

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            NickName = model.Email.Split('@')[0],
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("；", result.Errors.Select(e => e.Description));
            throw new BadRequestException(AppCodes.InvalidParameter, errors);
        }

        await _userManager.AddToRoleAsync(user, RoleConstants.User);
        _logger.LogInformation("新用户注册成功：UserId={UserId}, Email={Email}", user.Id, model.Email);
    }

    public async Task SendResetCodeAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            throw new BadRequestException(AppCodes.EmailNotRegistered, "该邮箱未注册");

        if (!await _verificationCodeService.CanSendAsync(email))
            throw new BadRequestException(AppCodes.SendTooFrequent, "发送过于频繁，请60秒后再试");

        var expireMinutes = await _configService.GetIntAsync("System", "VerificationCodeExpireMinutes", 5);
        var code = await _verificationCodeService.GenerateAsync(email, expireMinutes);
        await _emailService.SendVerificationCodeAsync(email, code, expireMinutes);
    }

    public async Task ResetPasswordAsync(ResetPasswordModel model)
    {
        if (!await _verificationCodeService.VerifyAsync(model.Email, model.Code))
            throw new BadRequestException(AppCodes.CodeExpired, "验证码错误或已过期");

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            throw new BadRequestException(AppCodes.EmailNotRegistered, "该邮箱未注册");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("；", result.Errors.Select(e => e.Description));
            throw new BadRequestException(AppCodes.InvalidParameter, errors);
        }

        _logger.LogInformation("密码重置成功：UserId={UserId}, Email={Email}", user.Id, model.Email);
    }

    // ──────── 私有方法 ────────

    /// <summary>
    /// 根据用户实体构建 UserInfoResult
    /// </summary>
    private async Task<UserInfoResult> BuildUserInfoAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserInfoResult
        {
            Username = user.UserName ?? "",
            Email = user.Email ?? "",
            AvatarUrl = user.AvatarUrl ?? "",
            NickName = user.NickName,
            Roles = roles.ToList()
        };
    }
}
