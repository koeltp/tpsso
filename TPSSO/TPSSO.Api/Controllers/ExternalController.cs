using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Options;
using TPSSO.Domain.Entities;

namespace TPSSO.Api.Controllers;

/// <summary>
/// 外部登录控制器（GitHub 等）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExternalController : ControllerBase
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IAccountService _accountService;
    private readonly ILogger<ExternalController> _logger;

    public ExternalController(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IAccountService accountService,
        ILogger<ExternalController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// 发起 GitHub 登录挑战
    /// 前端调用此端点，重定向到 GitHub 授权页
    /// </summary>
    [HttpGet("github")]
    [AllowAnonymous]
    public IActionResult GitHubLogin([FromQuery] string? returnUrl)
    {
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("GitHub",
            Url.Action(nameof(GitHubCallback), "External", new { returnUrl }));
        return Challenge(properties, "GitHub");
    }

    /// <summary>
    /// GitHub 回调端点
    /// GitHub 授权成功后回调此地址，自动查找或创建用户，签发 JWT，重定向回前端
    /// </summary>
    [HttpGet("github/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GitHubCallback([FromQuery] string? returnUrl)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("GitHub 外部登录信息获取失败");
            return Redirect($"/login?error=external_login_failed");
        }

        // 尝试用外部登录信息查找已有用户
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
        User user;

        if (result.Succeeded)
        {
            // 已有关联用户
            user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey)
                ?? throw new InvalidOperationException("外部登录成功但未找到用户");
        }
        else
        {
            // 没有关联用户，尝试通过邮箱查找并关联，或创建新用户
            var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
            var userName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? $"github_{info.ProviderKey}";

            user = !string.IsNullOrEmpty(email) ? await _userManager.FindByEmailAsync(email) : null;

            if (user != null)
            {
                // 邮箱已存在，关联外部登录
                var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(info.LoginProvider, info.ProviderKey, info.LoginProvider));
                if (!addLoginResult.Succeeded)
                {
                    _logger.LogError("关联外部登录失败: {Errors}", string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                    return Redirect($"/login?error=link_external_failed");
                }
            }
            else
            {
                // 创建新用户
                user = new User
                {
                    UserName = userName,
                    Email = email,
                    NickName = info.Principal.FindFirstValue("urn:github:name") ?? userName,
                    AvatarUrl = info.Principal.FindFirstValue("urn:github:avatar_url"),
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("创建外部登录用户失败: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return Redirect($"/login?error=create_user_failed");
                }

                // 分配默认角色
                await _userManager.AddToRoleAsync(user, RoleConstants.User);

                // 关联外部登录
                await _userManager.AddLoginAsync(user, new UserLoginInfo(info.LoginProvider, info.ProviderKey, info.LoginProvider));
            }

            // 登录用户（为 OAuth 授权流程保留 Cookie）
            await _signInManager.SignInAsync(user, isPersistent: false);
        }

        // 签发 JWT Token
        var loginResult = await _accountService.ExternalLoginAsync(new Application.Models.ExternalLoginModel
        {
            UserId = user.Id
        });

        if (loginResult.Code != 200)
        {
            return Redirect($"/login?error=token_issue_failed");
        }

        // 重定向回前端，携带 token
        var token = loginResult.Data!.Token;
        var refreshToken = loginResult.Data.RefreshToken;
        var frontendUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;

        // 如果是前端路由，跳转到 callback 页面处理 token
        var separator = frontendUrl.Contains('?') ? "&" : "?";
        return Redirect($"{frontendUrl}{separator}token={Uri.EscapeDataString(token)}&refreshToken={Uri.EscapeDataString(refreshToken)}");
    }
}
