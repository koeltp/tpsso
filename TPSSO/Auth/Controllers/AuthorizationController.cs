using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Taipi.Core.Extensions;
using TPSSO.Application.Options;
using TPSSO.Auth.Services;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Auth.Controllers;

[ApiController]
[Route("connect")]
[IgnoreAntiforgeryToken]
public class AuthorizationController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly ILogger<AuthorizationController> _logger;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly SsoOptions _ssoOptions;
    private readonly ApplicationDbContext _context;
    private readonly TokenGrantService _tokenGrantService;
    private readonly ClaimsBuilderService _claimsBuilder;

    public AuthorizationController(
        ILogger<AuthorizationController> logger,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IOptions<SsoOptions> ssoOptions,
        ApplicationDbContext context,
        TokenGrantService tokenGrantService,
        ClaimsBuilderService claimsBuilder)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
        _ssoOptions = ssoOptions.Value;
        _context = context;
        _logger = logger;
        _tokenGrantService = tokenGrantService;
        _claimsBuilder = claimsBuilder;
    }

    // ──────── 授权端点 ────────

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

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!) ??
            throw new InvalidOperationException("客户端应用不存在。");

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToLoginPage();
        }

        var appName = (await _applicationManager.GetDisplayNameAsync(application)) ?? request.ClientId;

        // 获取客户端业务信息（Logo、描述）
        var openIddictId = await _applicationManager.GetIdAsync(application);
        var clientApp = await _context.ClientApplications
            .FirstOrDefaultAsync(c => c.OpenIddictApplicationId == openIddictId);

        // 查找已存在的授权记录（用户之前已同意过）
        var authorizations = await _authorizationManager.FindAsync(
            subject: await _userManager.GetUserIdAsync(user),
            client: await _applicationManager.GetIdAsync(application)!,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes()).ToListAsync();

        // 已有永久授权则自动跳过确认页
        if (authorizations.Any())
        {
            return await SignInWithAuthorizationAsync(user, application, request);
        }

        // 重定向到前端授权确认页面
        var consentUrl = $"{_ssoOptions.LoginBaseUrl}{_ssoOptions.ConsentPath}" +
            $"?client_id={Uri.EscapeDataString(request.ClientId!)}" +
            $"&scope={Uri.EscapeDataString(string.Join(" ", request.GetScopes()))}" +
            $"&redirect_uri={Uri.EscapeDataString(request.RedirectUri ?? "")}" +
            $"&state={Uri.EscapeDataString(request.State ?? "")}" +
            $"&response_type={Uri.EscapeDataString(request.ResponseType ?? "")}" +
            $"&code_challenge={Uri.EscapeDataString(request.CodeChallenge ?? "")}" +
            $"&code_challenge_method={Uri.EscapeDataString(request.CodeChallengeMethod ?? "")}" +
            $"&app_name={Uri.EscapeDataString(appName!)}" +
            $"&app_logo={Uri.EscapeDataString(clientApp?.Logo ?? "")}" +
            $"&app_desc={Uri.EscapeDataString(clientApp?.Description ?? "")}";
        return Redirect(consentUrl);
    }

    /// <summary>
    /// POST /connect/authorize - 用户同意授权后签发授权码
    /// </summary>
    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizeConfirm()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("无法获取 OpenID Connect 请求。");

        if (!User.Identity?.IsAuthenticated == true)
            return RedirectToLoginPage();

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!) ??
            throw new InvalidOperationException("客户端应用不存在。");

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToLoginPage();
        }

        return await SignInWithAuthorizationAsync(user, application, request);
    }

    // ──────── Device Authorization 端点 ────────

    /// <summary>
    /// POST /connect/device - 设备授权流：返回验证码和用户码
    /// </summary>
    [HttpPost("device")]
    public async Task<IActionResult> Device()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("无法获取 OpenID Connect 请求。");

        _logger.LogInformation("收到设备授权请求，ClientId: {ClientId}", request.ClientId);

        // OpenIddict 自动处理设备码生成和返回
        return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// GET /connect/verify - 设备授权流：显示用户码输入/确认页面
    /// </summary>
    [HttpGet("verify")]
    public async Task<IActionResult> Verify()
    {
        var request = HttpContext.GetOpenIddictServerRequest();

        // 未登录则重定向到登录页，登录后再回来验证
        if (!User.Identity?.IsAuthenticated == true)
        {
            var returnUrl = $"{Request.Scheme}://{Request.Host}/connect/verify{HttpContext.Request.QueryString}";
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
            var loginUrl = $"{_ssoOptions.LoginBaseUrl}{_ssoOptions.LoginPath}?returnUrl={encodedReturnUrl}";
            return Redirect(loginUrl);
        }

        // 如果 URL 中已有 user_code 参数，直接跳转到前端验证确认页
        var userCode = request?.UserCode;
        if (!string.IsNullOrEmpty(userCode))
        {
            var verifyUrl = $"{_ssoOptions.LoginBaseUrl}/device-verify?user_code={Uri.EscapeDataString(userCode)}";
            return Redirect(verifyUrl);
        }

        // 否则重定向到前端用户码输入页
        return Redirect($"{_ssoOptions.LoginBaseUrl}/device-verify");
    }

    /// <summary>
    /// POST /connect/verify - 设备授权流：用户确认授权（同意）
    /// 参照 OpenIddict 官方 Matty 示例实现
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyAccept()
    {
        // 获取当前登录用户
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("VerifyAccept: 用户未登录，返回 Forbid");
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // 通过 OpenIddict 认证获取用户码关联的 Claims
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        _logger.LogInformation("VerifyAccept: AuthenticateAsync 结果 - Succeeded={Succeeded}, Principal={HasPrincipal}, ClientId={ClientId}",
            result.Succeeded,
            result.Principal != null,
            result.Principal?.GetClaim(Claims.ClientId) ?? "(null)");

        if (result is { Succeeded: true } && !string.IsNullOrEmpty(result.Principal?.GetClaim(Claims.ClientId)))
        {
            // 使用 ClaimsBuilderService 创建声明，保持与其他授权流一致
            var scopes = result.Principal.GetScopes();
            var identity = await _claimsBuilder.CreateUserClaimsIdentityAsync(user, scopes);
            identity.SetScopes(scopes);

            var resources = await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync();
            identity.SetResources(resources);

            var properties = new AuthenticationProperties
            {
                // 授权成功后重定向到前端成功页面
                RedirectUri = $"{_ssoOptions.LoginBaseUrl}/device-verify?step=success"
            };

            _logger.LogInformation("VerifyAccept: 授权成功，SignIn 用户 {UserName}", user.UserName);
            return SignIn(new ClaimsPrincipal(identity), properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // 用户码无效，通知 OpenIddict 拒绝授权（与 Matty 示例一致）
        _logger.LogWarning("VerifyAccept: 用户码验证失败，返回 Forbid（access_denied）");
        return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // ──────── Token 端点 ────────

    /// <summary>
    /// POST /connect/token - 处理所有授权类型的 Token 请求
    /// </summary>
    [HttpPost("token")]
    [EnableRateLimiting(RateLimitPolicies.TokenEndpoint)]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
            return BadRequest(new { error = "invalid_request", error_description = "无法获取 OpenID Connect 请求。" });

        return request.GrantType switch
        {
            GrantTypes.AuthorizationCode => await _tokenGrantService.HandleAuthorizationCodeGrantAsync(HttpContext, request),
            GrantTypes.RefreshToken => await _tokenGrantService.HandleRefreshTokenGrantAsync(HttpContext, request),
            GrantTypes.ClientCredentials => await _tokenGrantService.HandleClientCredentialsGrantAsync(request),
            GrantTypes.DeviceCode => await _tokenGrantService.HandleDeviceCodeGrantAsync(HttpContext, request),
            _ => BadRequest(new { error = "unsupported_grant_type", error_description = $"不支持的授权类型：{request.GrantType}" })
        };
    }

    // ──────── 登出端点 ────────

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

    // ──────── 私有方法 ────────

    /// <summary>
    /// 签发授权码（含授权确认持久化）
    /// </summary>
    private async Task<IActionResult> SignInWithAuthorizationAsync(User user, object application, OpenIddictRequest request)
    {
        var scopes = request.GetScopes();
        var identity = await _claimsBuilder.CreateUserClaimsIdentityAsync(user, scopes);
        identity.SetScopes(scopes);
        identity.SetResources(await _scopeManager.ListResourcesAsync(scopes).ToListAsync());

        // 授权确认持久化：自动创建永久授权记录，下次免确认
        var authorizations = await _authorizationManager.FindAsync(
            subject: await _userManager.GetUserIdAsync(user),
            client: await _applicationManager.GetIdAsync(application) ?? "",
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: scopes).ToListAsync();

        // 没有永久授权则自动创建（用户已确认授权）
        if (!authorizations.Any())
        {
            var descriptor = new OpenIddictAuthorizationDescriptor
            {
                Subject = await _userManager.GetUserIdAsync(user),
                ApplicationId = await _applicationManager.GetIdAsync(application) ?? "",
                Type = AuthorizationTypes.Permanent
            };
            foreach (var scope in scopes)
            {
                descriptor.Scopes.Add(scope);
            }
            await _authorizationManager.CreateAsync(descriptor);
        }

        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            new AuthenticationProperties(),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
    }

    /// <summary>
    /// 构造前端登录页重定向 URL
    /// </summary>
    private IActionResult RedirectToLoginPage()
    {
        var returnUrl = $"{Request.Scheme}://{Request.Host}/connect/authorize{HttpContext.Request.QueryString}";
        var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
        var loginUrl = $"{_ssoOptions.LoginBaseUrl}{_ssoOptions.LoginPath}?returnUrl={encodedReturnUrl}";
        return Redirect(loginUrl);
    }
}
