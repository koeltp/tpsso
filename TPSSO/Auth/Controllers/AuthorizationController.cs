using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using TPSSO.Application.Options;
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

    public AuthorizationController(
        ILogger<AuthorizationController> logger,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IOptions<SsoOptions> ssoOptions,
        ApplicationDbContext context)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
        _ssoOptions = ssoOptions.Value;
        _context = context;
        _logger = logger;
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
            // 创建 Claims 身份，OpenIddict 将据此生成令牌
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                    .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                    .SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user))
                    .SetClaim(Claims.PreferredUsername, await _userManager.GetUserNameAsync(user))
                    .SetClaims(Claims.Role, [.. await _userManager.GetRolesAsync(user)]);

            // 授予请求的 scopes
            identity.SetScopes(result.Principal.GetScopes());

            // 设置资源
            var resources = await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync();
            identity.SetResources(resources);

            identity.SetDestinations(GetDestinations);

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
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
            return BadRequest(new { error = "invalid_request", error_description = "无法获取 OpenID Connect 请求。" });

        return request.GrantType switch
        {
            GrantTypes.AuthorizationCode => await HandleAuthorizationCodeGrant(request),
            GrantTypes.RefreshToken => await HandleRefreshTokenGrant(request),
            GrantTypes.ClientCredentials => await HandleClientCredentialsGrant(request),
            GrantTypes.DeviceCode => await HandleDeviceCodeGrant(request),
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
    /// 处理授权码换 Token
    /// </summary>
    private async Task<IActionResult> HandleAuthorizationCodeGrant(OpenIddictRequest request)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (result?.Principal == null)
            return ForbidWithError(Errors.InvalidGrant, "授权码无效或已过期。");

        var userId = result.Principal.FindFirst(Claims.Subject)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
            return ForbidWithError(Errors.InvalidGrant, "关联用户不存在。");

        // 重新创建 principal，确保包含最新的声明
        var identity = new ClaimsIdentity(
            result.Principal.Identities.First().Claims,
            authenticationType: TokenValidationParameters.DefaultAuthenticationType);

        var newPrincipal = new ClaimsPrincipal(identity);
        newPrincipal.SetScopes(request.GetScopes());
        newPrincipal.SetResources(await _scopeManager.ListResourcesAsync(request.GetScopes()).ToListAsync());

        return SignIn(newPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// 处理 Refresh Token 刷新
    /// </summary>
    private async Task<IActionResult> HandleRefreshTokenGrant(OpenIddictRequest request)
    {
        _logger.LogInformation("收到 Refresh Token 刷新请求。");

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (result?.Principal == null)
            return ForbidWithError(Errors.InvalidGrant, "Refresh Token 无效或已过期。");

        var userId = result.Principal.FindFirst(Claims.Subject)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
            return ForbidWithError(Errors.InvalidGrant, "关联用户不存在。");

        // 确保用户仍然有权登录（未被禁用等）
        if (!await _signInManager.CanSignInAsync(user))
            return ForbidWithError(Errors.InvalidGrant, "用户已被禁用。");

        // 重新构建 principal，确保声明是最新的
        var identity = await CreateUserClaimsIdentityAsync(user, request.GetScopes());
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources(await _scopeManager.ListResourcesAsync(request.GetScopes()).ToListAsync());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// 处理客户端凭证授权（M2M，无用户参与）
    /// </summary>
    private async Task<IActionResult> HandleClientCredentialsGrant(OpenIddictRequest request)
    {
        _logger.LogInformation("收到 Client Credentials 请求，ClientId: {ClientId}", request.ClientId);

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!) ??
            throw new InvalidOperationException("客户端应用不存在。");

        // 创建客户端身份（无用户）
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, await _applicationManager.GetIdAsync(application) ?? "")
            .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        identity.AddClaim(new Claim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application) ?? "")
            .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

        var scopes = request.GetScopes();
        identity.SetScopes(scopes);
        identity.SetResources(await _scopeManager.ListResourcesAsync(scopes).ToListAsync());

        var principal = new ClaimsPrincipal(identity);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// 处理设备码授权（用户已在其他设备上确认授权）
    /// </summary>
    private async Task<IActionResult> HandleDeviceCodeGrant(OpenIddictRequest request)
    {
        _logger.LogInformation("收到 Device Code 请求，ClientId: {ClientId}", request.ClientId);

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (result?.Principal == null)
            return ForbidWithError(Errors.InvalidGrant, "设备码无效或已过期。");

        var userId = result.Principal.FindFirst(Claims.Subject)?.Value;
        if (string.IsNullOrEmpty(userId))
            return ForbidWithError(Errors.InvalidGrant, "设备码尚未被用户确认。");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ForbidWithError(Errors.InvalidGrant, "关联用户不存在。");

        var identity = await CreateUserClaimsIdentityAsync(user, request.GetScopes());
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources(await _scopeManager.ListResourcesAsync(request.GetScopes()).ToListAsync());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// 签发授权码（含授权确认持久化）
    /// </summary>
    private async Task<IActionResult> SignInWithAuthorizationAsync(User user, object application, OpenIddictRequest request)
    {
        var scopes = request.GetScopes();
        var identity = await CreateUserClaimsIdentityAsync(user, scopes);
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
    /// 创建包含用户声明的 ClaimsIdentity
    /// </summary>
    private async Task<ClaimsIdentity> CreateUserClaimsIdentityAsync(User user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, await _userManager.GetUserIdAsync(user))
            .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

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

        if (scopes.Contains(Scopes.Roles))
        {
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(Claims.Role, role)
                    .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
            }
        }

        return identity;
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

    /// <summary>
    /// 返回 OAuth 错误响应
    /// </summary>
    private IActionResult ForbidWithError(string error, string description)
    {
        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));
    }

    /// <summary>
    /// 决定每个 Claim 应该出现在哪些令牌中（access_token / id_token）
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case Claims.Name or Claims.PreferredUsername:
                yield return Destinations.AccessToken;
                if (claim.Subject!.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;
                if (claim.Subject!.HasScope(Scopes.Email))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                if (claim.Subject!.HasScope(Scopes.Roles))
                    yield return Destinations.IdentityToken;
                yield break;

            // 安全戳不出现在令牌中
            case "AspNet.Identity.SecurityStamp":
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
