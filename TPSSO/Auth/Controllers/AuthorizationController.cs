using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using TPSSO.Application.Options;
using TPSSO.Domain.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Auth.Controllers;

[ApiController]
[Route("connect")]
[IgnoreAntiforgeryToken]
public class AuthorizationController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ILogger<AuthorizationController> _logger;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly SsoOptions _ssoOptions;

    public AuthorizationController(
        ILogger<AuthorizationController> logger,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IOptions<SsoOptions> ssoOptions)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
        _ssoOptions = ssoOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// GET /connect/authorize - OAuth 授权端点
    /// </summary>
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("无法获取 OpenID Connect 请求。");

        _logger.LogInformation("收到授权请求。");

        // 未登录则重定向到前端登录页
        if (!User.Identity?.IsAuthenticated == true)
            return RedirectToLoginPage();

        _logger.LogInformation("用户已登录，开始处理授权请求。");

        // 查找客户端应用
        _logger.LogInformation("尝试查找客户端应用。");
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!);
        if (application == null)
            throw new InvalidOperationException("客户端应用不存在。");
        _logger.LogInformation("客户端应用已找到。");

        // 查找用户
        _logger.LogInformation("尝试查找用户。");
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToLoginPage();
        }
        _logger.LogInformation("用户已找到。");

        var appName = (await _applicationManager.GetDisplayNameAsync(application)) ?? request.ClientId;
        _logger.LogInformation("客户端应用显示名称：{appName}。", appName);

        // 重定向到前端授权确认页面
        var consentUrl = $"{_ssoOptions.LoginBaseUrl}{_ssoOptions.ConsentPath}" +
            $"?client_id={Uri.EscapeDataString(request.ClientId!)}" +
            $"&scope={Uri.EscapeDataString(string.Join(" ", request.GetScopes()))}" +
            $"&redirect_uri={Uri.EscapeDataString(request.RedirectUri ?? "")}" +
            $"&state={Uri.EscapeDataString(request.State ?? "")}" +
            $"&response_type={Uri.EscapeDataString(request.ResponseType ?? "")}" +
            $"&code_challenge={Uri.EscapeDataString(request.CodeChallenge ?? "")}" +
            $"&code_challenge_method={Uri.EscapeDataString(request.CodeChallengeMethod ?? "")}" +
            $"&app_name={Uri.EscapeDataString(appName!)}";
        _logger.LogInformation("重定向到授权确认页面：{consentUrl}。", consentUrl);
        return Redirect(consentUrl);
    }

    /// <summary>
    /// POST /connect/authorize - 用户同意授权后签发授权码
    /// </summary>
    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizeConfirm()
    {
        _logger.LogInformation("收到授权确认请求。");

        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("无法获取 OpenID Connect 请求。");

        _logger.LogInformation("用户已登录，开始处理授权确认请求。");

        // 查找客户端应用
        _logger.LogInformation("尝试查找客户端应用。");
        if (!User.Identity?.IsAuthenticated == true)
            return RedirectToLoginPage();

        _logger.LogInformation("用户已登录，开始处理授权确认请求。");

        // 查找用户
        _logger.LogInformation("尝试查找用户。");
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!);
        if (application == null)
            throw new InvalidOperationException("客户端应用不存在。");
        _logger.LogInformation("客户端应用已找到。");

        // 查找用户
        _logger.LogInformation("尝试查找用户。");
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToLoginPage();
        }
        _logger.LogInformation("用户已找到。");

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, await _userManager.GetUserIdAsync(user))
            .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

        var scopes = request.GetScopes();

        if (scopes.Contains(Scopes.Profile))
        {
            var name = await _userManager.GetUserNameAsync(user);
            identity.AddClaim(new Claim(Claims.Name, name!)
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        }

        if (scopes.Contains(Scopes.Email))
        {
            var mail = await _userManager.GetEmailAsync(user);
            identity.AddClaim(new Claim(Claims.Email, mail!)
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        }

        // 查找用户角色
        _logger.LogInformation("尝试查找用户角色。");
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(Claims.Role, role)
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        }

        identity.SetScopes(scopes);
        identity.SetResources(await _scopeManager.ListResourcesAsync(scopes).ToListAsync());

        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            new AuthenticationProperties(),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
    }

    /// <summary>
    /// GET/POST /connect/logout - 登出端点
    /// </summary>
    [HttpGet("logout")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var request = HttpContext.GetOpenIddictServerRequest();

        await _signInManager.SignOutAsync();

        if (request?.PostLogoutRedirectUri != null)
            return Redirect(request.PostLogoutRedirectUri);

        return Redirect(_ssoOptions.LoginBaseUrl);
    }

    /// <summary>
    /// POST /connect/token - 用授权码换 Token
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> Exchange()
    {
        try
        {
            _logger.LogInformation("收到用授权码换 Token 请求。");
            var request = HttpContext.GetOpenIddictServerRequest();
            if (request == null)
                return BadRequest(new { error = "invalid_request", error_description = "无法获取 OpenID Connect 请求。" });

            if (request.IsAuthorizationCodeGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                if (result?.Principal == null)
                {
                    _logger.LogInformation("授权码无效或已过期。");
                    return Forbid(authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                                  properties: new AuthenticationProperties(new Dictionary<string, string?>
                                  {
                                      [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                                      [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "授权码无效或已过期。"
                                  }));
                }

                _logger.LogInformation("授权码有效，用户已找到。");

                var userId = result.Principal.FindFirst(Claims.Subject)?.Value;
                var user = await _userManager.FindByIdAsync(userId!);
                if (user == null)
                {
                    _logger.LogInformation("关联用户不存在。");
                    return Forbid(authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                                  properties: new AuthenticationProperties(new Dictionary<string, string?>
                                  {
                                      [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                                      [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "关联用户不存在。"
                                  }));
                }

                _logger.LogInformation("关联用户存在。");

                // 重新创建 principal，确保包含最新的声明
                var identity = new ClaimsIdentity(
                    result.Principal.Identities.First().Claims,
                    authenticationType: TokenValidationParameters.DefaultAuthenticationType);

                // 重新创建 principal，确保包含最新的声明
                var newPrincipal = new ClaimsPrincipal(identity);
                newPrincipal.SetScopes(request.GetScopes());
                newPrincipal.SetResources(await _scopeManager.ListResourcesAsync(request.GetScopes()).ToListAsync());



                return SignIn(newPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            _logger.LogInformation("不支持的授权类型。");
            return BadRequest(new { error = "unsupported_grant_type", error_description = "不支持的授权类型。" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用授权码换 Token 失败。");
            return BadRequest(new { error = "server_error", error_description = ex.Message });
        }
    }

    // ──────── 私有方法 ────────

    /// <summary>
    /// 构造前端登录页重定向 URL
    /// </summary>
    private IActionResult RedirectToLoginPage()
    {
        var returnUrl = $"{Request.Scheme}://{Request.Host}/connect/authorize{HttpContext.Request.QueryString}";
        var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
        var loginUrl = $"{_ssoOptions.LoginBaseUrl}{_ssoOptions.LoginPath}?returnUrl={encodedReturnUrl}";
        _logger.LogInformation("重定向到登录页，returnUrl：{returnUrl}。", returnUrl);
        return Redirect(loginUrl);
    }
}
