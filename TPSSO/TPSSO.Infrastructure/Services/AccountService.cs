using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Taipi.Core.RQRS;
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

    public async Task<ResponseResult<UserInfoResult>> LoginAsync(LoginModel model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            return ResponseResult<UserInfoResult>.Error(401, "无效的用户名或密码");

        // 检查用户是否被禁用
        if (await _userManager.IsLockedOutAsync(user))
            return ResponseResult<UserInfoResult>.Error(403, "该账号已被禁用，请联系管理员");

        // Cookie 登录，为 OAuth 授权流程保留登录态
        await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);

        var userInfo = await BuildUserInfoAsync(user);
        return new ResponseResult<UserInfoResult>(userInfo) { Code = 200, Message = "登录成功" };
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

        var userInfo = await BuildUserInfoAsync(user);
        return new ResponseResult<UserInfoResult>(userInfo) { Code = 200, Message = "获取成功" };
    }

    public async Task<ResponseResult<bool>> UpdateProfileAsync(ClaimsPrincipal principal, UpdateProfileModel model)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return ResponseResult<bool>.Unauthorized("未登录");

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return ResponseResult<bool>.NotFound("用户不存在");

        user.NickName = model.NickName;
        user.AvatarUrl = model.AvatarUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ResponseResult<bool>.BadRequest(string.Join("；", result.Errors.Select(e => e.Description)));

        return ResponseResult<bool>.Success("修改成功");
    }

    public async Task<ResponseResult<string>> UpdateAvatarUrlAsync(ClaimsPrincipal principal, string avatarUrl)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return ResponseResult<string>.Unauthorized("未登录");

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return ResponseResult<string>.NotFound("用户不存在");

        user.AvatarUrl = avatarUrl;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ResponseResult<string>.BadRequest(string.Join("；", result.Errors.Select(e => e.Description)));

        return new ResponseResult<string>(avatarUrl) { Message = "上传成功" };
    }

    public async Task<ResponseResult<bool>> ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordModel model)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return ResponseResult<bool>.Unauthorized("未登录");

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return ResponseResult<bool>.NotFound("用户不存在");

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
            return ResponseResult<bool>.BadRequest(string.Join("；", result.Errors.Select(e => e.Description)));

        return ResponseResult<bool>.Success("密码修改成功");
    }

    public async Task<ResponseResult<bool>> SendCodeAsync(string email)
    {
        // 检查邮箱是否已注册
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            return ResponseResult<bool>.BadRequest("该邮箱已注册");

        // 检查发送频率
        if (!await _verificationCodeService.CanSendAsync(email))
            return ResponseResult<bool>.BadRequest("发送过于频繁，请60秒后再试");

        // 读取验证码有效期配置（默认5分钟）
        var expireMinutes = await _configService.GetIntAsync("System", "VerificationCodeExpireMinutes", 5);

        // 生成验证码
        var code = await _verificationCodeService.GenerateAsync(email, expireMinutes);

        // 发送邮件
        await _emailService.SendVerificationCodeAsync(email, code, expireMinutes);

        return ResponseResult<bool>.Success("验证码已发送");
    }

    public async Task<ResponseResult<bool>> RegisterAsync(RegisterModel model)
    {
        // 验证验证码
        if (!await _verificationCodeService.VerifyAsync(model.Email, model.Code))
            return ResponseResult<bool>.BadRequest("验证码错误或已过期");

        // 检查邮箱是否已注册
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
            return ResponseResult<bool>.BadRequest("该邮箱已注册");

        // 创建用户
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
            return ResponseResult<bool>.BadRequest(errors);
        }

        // 自动分配 User 角色
        await _userManager.AddToRoleAsync(user, RoleConstants.User);

        _logger.LogInformation("新用户注册成功：{Email}", model.Email);
        return ResponseResult<bool>.Success("注册成功");
    }

    public async Task<ResponseResult<bool>> SendResetCodeAsync(string email)
    {
        // 检查邮箱是否已注册
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return ResponseResult<bool>.BadRequest("该邮箱未注册");

        // 检查发送频率
        if (!await _verificationCodeService.CanSendAsync(email))
            return ResponseResult<bool>.BadRequest("发送过于频繁，请60秒后再试");

        // 读取验证码有效期配置
        var expireMinutes = await _configService.GetIntAsync("System", "VerificationCodeExpireMinutes", 5);

        // 生成验证码
        var code = await _verificationCodeService.GenerateAsync(email, expireMinutes);

        // 发送邮件
        await _emailService.SendVerificationCodeAsync(email, code, expireMinutes);

        return ResponseResult<bool>.Success("验证码已发送");
    }

    public async Task<ResponseResult<bool>> ResetPasswordAsync(ResetPasswordModel model)
    {
        // 验证验证码
        if (!await _verificationCodeService.VerifyAsync(model.Email, model.Code))
            return ResponseResult<bool>.BadRequest("验证码错误或已过期");

        // 查找用户
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return ResponseResult<bool>.BadRequest("该邮箱未注册");

        // 重置密码
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("；", result.Errors.Select(e => e.Description));
            return ResponseResult<bool>.BadRequest(errors);
        }

        _logger.LogInformation("用户 {Email} 通过验证码重置了密码", model.Email);
        return ResponseResult<bool>.Success("密码重置成功");
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
