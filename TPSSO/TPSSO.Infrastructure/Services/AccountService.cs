using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;
using TPSSO.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TPSSO.Infrastructure.Services;

/// <summary>
/// 账户业务服务实现
/// </summary>
public class AccountService : IAccountService
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        ILogger<AccountService> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
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
        Console.WriteLine("=====================================================GetCurrentUserAsync==========================================");
        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles, // .NET 6+
            WriteIndented = true
        };
        Console.WriteLine(user);
        Console.WriteLine(JsonSerializer.Serialize(principal.Claims, options));
        Console.WriteLine(JsonSerializer.Serialize(principal.Identity, options));
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
