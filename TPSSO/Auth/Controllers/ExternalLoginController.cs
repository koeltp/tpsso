using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Options;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;

namespace TPSSO.Auth.Controllers;

/// <summary>
/// 外部登录控制器：处理第三方 OAuth 登录的发起、回调和 Provider 列表
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
public class ExternalLoginController : ControllerBase
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfigService _configService;
    private readonly SsoOptions _ssoOptions;
    private readonly ILogger<ExternalLoginController> _logger;

    public ExternalLoginController(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        ApplicationDbContext context,
        IConfigService configService,
        Microsoft.Extensions.Options.IOptions<SsoOptions> ssoOptions,
        ILogger<ExternalLoginController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _configService = configService;
        _ssoOptions = ssoOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/external-login/providers - 获取已启用的第三方登录 Provider 列表
    /// </summary>
    [HttpGet("api/external-login/providers")]
    public async Task<IActionResult> GetProviders()
    {
        // 查询 OAuth 父分类下的所有子分类
        var providers = await _context.DictTypes
            .Where(t => t.Parent != null && t.Parent.Code == "OAuth" && t.IsEnabled)
            .Where(t => t.Items.Any(i => i.Key == "IsEnabled" && i.Value == "true" && i.IsEnabled))
            .Select(t => new
            {
                scheme = t.Code,       // GitHub / Google / WeChat
                displayName = t.Name   // GitHub / Google / 微信
            })
            .ToListAsync();

        return Ok(providers);
    }

    /// <summary>
    /// GET /api/external-login/{provider} - 发起第三方登录
    /// 前端调用此接口后，后端 302 重定向到第三方授权页
    /// </summary>
    [HttpGet("api/external-login/{provider}")]
    public IActionResult Challenge(string provider, [FromQuery] string? returnUrl = null)
    {
        // 验证 Provider 是否已启用
        var isEnabled = _configService.GetBoolAsync(provider, "IsEnabled").GetAwaiter().GetResult();
        if (!isEnabled)
        {
            return BadRequest(new { error = "provider_disabled", message = $"第三方登录 {provider} 未启用" });
        }

        // 构造回调 URL，把 returnUrl 传递给回调
        var callbackUrl = Url.Action(nameof(Callback), "ExternalLogin", new { returnUrl }, Request.Scheme);

        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl);
        return Challenge(properties, provider);
    }

    /// <summary>
    /// GET /api/external-login/callback - 第三方登录统一回调
    /// ASP.NET Core 外部认证中间件处理后自动跳转到此方法
    /// </summary>
    [HttpGet("api/external-login/callback")]
    public async Task<IActionResult> Callback(string? returnUrl = null, string? remoteError = null)
    {
        // 第三方授权出错
        if (remoteError != null)
        {
            _logger.LogWarning("外部登录出错：{Error}", remoteError);
            return RedirectWithError(returnUrl, $"外部登录失败：{remoteError}");
        }

        // 获取外部登录信息
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("无法获取外部登录信息");
            return RedirectWithError(returnUrl, "无法获取外部登录信息");
        }

        var provider = info.LoginProvider;
        var providerKey = info.ProviderKey;
        _logger.LogInformation("外部登录回调：Provider={Provider}, ProviderKey={ProviderKey}", provider, providerKey);

        // 1. 尝试用 LoginProvider + ProviderKey 查找已关联的用户
        var user = await _userManager.FindByLoginAsync(provider, providerKey);

        if (user != null)
        {
            // 已关联用户，直接登录
            _logger.LogInformation("外部登录：已关联用户 {UserId}", user.Id);
            await _signInManager.SignInAsync(user, isPersistent: true);
            return RedirectAfterLogin(returnUrl);
        }

        // 2. 未关联用户，尝试通过邮箱匹配已有用户
        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                // 邮箱匹配，自动关联外部登录
                var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
                if (addLoginResult.Succeeded)
                {
                    _logger.LogInformation("外部登录：通过邮箱 {Email} 自动关联用户 {UserId}", email, user.Id);
                    await _signInManager.SignInAsync(user, isPersistent: true);
                    return RedirectAfterLogin(returnUrl);
                }
                _logger.LogWarning("外部登录：关联用户失败 {Errors}", string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
            }
        }

        // 3. 无匹配用户，自动创建新用户
        var userName = email ?? $"github_{providerKey}";
        var nickName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                       ?? info.Principal.FindFirst("urn:github:name")?.Value
                       ?? userName;

        var newUser = new User
        {
            UserName = userName,
            Email = email,
            NickName = nickName,
            AvatarUrl = info.Principal.FindFirst("urn:github:avatar_url")?.Value,
            EmailConfirmed = !string.IsNullOrEmpty(email), // GitHub 已验证的邮箱直接标记为已确认
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            _logger.LogWarning("外部登录：创建用户失败 {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return RedirectWithError(returnUrl, "自动创建用户失败：" + string.Join("；", createResult.Errors.Select(e => e.Description)));
        }

        // 分配普通用户角色
        await _userManager.AddToRoleAsync(newUser, RoleConstants.User);

        // 关联外部登录
        await _userManager.AddLoginAsync(newUser, new UserLoginInfo(provider, providerKey, provider));

        _logger.LogInformation("外部登录：自动创建用户 {UserId} 并关联 {Provider}", newUser.Id, provider);
        await _signInManager.SignInAsync(newUser, isPersistent: true);
        return RedirectAfterLogin(returnUrl);
    }

    /// <summary>
    /// GET /api/external-login/callback-bind - 第三方登录绑定回调
    /// 已登录用户绑定第三方账号时使用，通过回调URL区分绑定和登录场景
    /// </summary>
    [HttpGet("api/external-login/callback-bind")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> CallbackBind(string? remoteError = null)
    {
        var adminUrl = _ssoOptions.LoginBaseUrl.Replace("auth.taipi.top", "admin.taipi.top")
            .Replace("localhost:3010", "localhost:3009");
        var profileUrl = $"{adminUrl}/account/profile?tab=external";

        // 第三方授权出错
        if (remoteError != null)
        {
            _logger.LogWarning("绑定外部登录出错：{Error}", remoteError);
            return Redirect($"{profileUrl}&bindError={Uri.EscapeDataString($"绑定失败：{remoteError}")}");
        }

        // 获取外部登录信息
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("绑定回调：无法获取外部登录信息");
            return Redirect($"{profileUrl}&bindError={Uri.EscapeDataString("无法获取外部登录信息")}");
        }

        var provider = info.LoginProvider;
        var providerKey = info.ProviderKey;
        var displayName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? provider;

        _logger.LogInformation("绑定回调：Provider={Provider}, ProviderKey={ProviderKey}", provider, providerKey);

        // 检查该第三方账号是否已被其他用户绑定
        var existingUser = await _userManager.FindByLoginAsync(provider, providerKey);
        if (existingUser != null)
        {
            return Redirect($"{profileUrl}&bindError={Uri.EscapeDataString($"该 {provider} 账号已被其他用户绑定")}");
        }

        // 获取当前登录用户
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Redirect($"{profileUrl}&bindError={Uri.EscapeDataString("请先登录")}");
        }

        // 检查当前用户是否已绑定该Provider
        var logins = await _userManager.GetLoginsAsync(currentUser);
        if (logins.Any(l => l.LoginProvider == provider))
        {
            return Redirect($"{profileUrl}&bindError={Uri.EscapeDataString($"已绑定 {provider} 账号")}");
        }

        // 关联外部登录到当前用户
        var addLoginResult = await _userManager.AddLoginAsync(currentUser, new UserLoginInfo(provider, providerKey, displayName));
        if (!addLoginResult.Succeeded)
        {
            var errors = string.Join("；", addLoginResult.Errors.Select(e => e.Description));
            return Redirect($"{profileUrl}&bindError={Uri.EscapeDataString($"绑定失败：{errors}")}");
        }

        _logger.LogInformation("用户 {UserId} 绑定 {Provider} 成功", currentUser.Id, provider);

        // 清除外部认证Cookie
        await _signInManager.SignOutAsync();

        // 重新登录当前用户（因为 SignOutAsync 会清除登录状态）
        await _signInManager.SignInAsync(currentUser, isPersistent: true);

        return Redirect($"{profileUrl}&bindSuccess={Uri.EscapeDataString(provider)}");
    }

    /// <summary>
    /// 登录成功后重定向到前端，继续 OAuth 授权流程或跳转管理后台
    /// </summary>
    private IActionResult RedirectAfterLogin(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl))
        {
            // returnUrl 指向 OAuth 授权端点或前端页面
            if (returnUrl.StartsWith("/connect/") || returnUrl.StartsWith("http"))
            {
                return Redirect(returnUrl);
            }
        }

        // 默认跳转管理后台
        var adminUrl = _ssoOptions.LoginBaseUrl.Replace("auth.taipi.top", "admin.taipi.top")
            .Replace("localhost:3010", "localhost:3009");
        return Redirect($"{adminUrl}/dashboard");
    }

    /// <summary>
    /// 出错时重定向到前端登录页，附带错误信息
    /// </summary>
    private IActionResult RedirectWithError(string? returnUrl, string error)
    {
        var loginUrl = $"{_ssoOptions.LoginBaseUrl}{_ssoOptions.LoginPath}";
        var query = $"externalError={Uri.EscapeDataString(error)}";
        if (!string.IsNullOrEmpty(returnUrl))
        {
            query += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        }
        return Redirect($"{loginUrl}?{query}");
    }
}
