using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPSSO.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;
using TPSSO.Application.Options;
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
    private readonly ILogger<AccountService> _logger;
    private readonly JwtOptions _jwtOptions;

    public AccountService(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IVerificationCodeService verificationCodeService,
        IEmailService emailService,
        ILogger<AccountService> logger,
        IOptions<JwtOptions> jwtOptions)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _verificationCodeService = verificationCodeService;
        _emailService = emailService;
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<ResponseResult<LoginResult>> LoginAsync(LoginModel model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            return ResponseResult<LoginResult>.Error(401, "无效的用户名或密码");

        // 检查用户是否被禁用
        if (await _userManager.IsLockedOutAsync(user))
            return ResponseResult<LoginResult>.Error(403, "该账号已被禁用，请联系管理员");

        // 为 OAuth 授权流程保留 Cookie 登录态
        await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);

        return await IssueTokensAsync(user, "登录成功");
    }

    public async Task<ResponseResult<bool>> LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return ResponseResult<bool>.Success();
    }

    public async Task<ResponseResult<LoginResult>> RefreshTokenAsync(string refreshToken)
    {
        // 直接数据库查询，避免全表加载到内存
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        if (user == null)
            return ResponseResult<LoginResult>.Unauthorized("无效的 Refresh Token");

        if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            return ResponseResult<LoginResult>.Unauthorized("Refresh Token 已过期");

        // 检查用户是否被禁用
        if (await _userManager.IsLockedOutAsync(user))
            return ResponseResult<LoginResult>.Forbidden("该账号已被禁用，请联系管理员");

        return await IssueTokensAsync(user, "刷新成功");
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
            AvatarUrl = user.AvatarUrl ?? "",
            NickName = user.NickName,
            Roles = (await _userManager.GetRolesAsync(user)).ToList()
        };

        return new ResponseResult<UserInfoResult>(data) { Code = 200, Message = "获取成功" };
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
        var user = new User
        {
            UserName = model.Username,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return ResponseResult<bool>.BadRequest(string.Join("；", result.Errors.Select(e => e.Description)));

        // 默认分配 User 角色
        await _userManager.AddToRoleAsync(user, RoleConstants.User);

        return ResponseResult<bool>.Success("注册成功");
    }

    // ──────── 私有方法 ────────

    /// <summary>
    /// 签发 JWT Access Token + Refresh Token，统一登录和刷新的 Token 生成逻辑
    /// </summary>
    private async Task<ResponseResult<LoginResult>> IssueTokensAsync(User user, string message)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.UserName ?? ""),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // 轮换 Refresh Token：每次签发都生成新的，旧的作废
        var newRefreshToken = Guid.NewGuid().ToString("N");
        var newRefreshExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshExpireDays);
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAt = newRefreshExpiresAt;
        await _userManager.UpdateAsync(user);

        var userInfo = new UserInfoResult
        {
            Username = user.UserName ?? "",
            Email = user.Email ?? "",
            AvatarUrl = user.AvatarUrl ?? "",
            NickName = user.NickName,
            Roles = roles.ToList()
        };

        var result = new LoginResult
        {
            Token = tokenString,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt,
            UserInfo = userInfo
        };

        return new ResponseResult<LoginResult>(result) { Code = 200, Message = message };
    }
}
