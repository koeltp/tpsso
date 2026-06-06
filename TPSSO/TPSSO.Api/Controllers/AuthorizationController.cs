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
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Api.Controllers;

[ApiController]
[Route("connect")]
[IgnoreAntiforgeryToken]
public class AuthorizationController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SsoOptions _ssoOptions;

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<SsoOptions> ssoOptions)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
        _ssoOptions = ssoOptions.Value;
    }

    /// <summary>
    /// GET /connect/authorize - 展示授权确认页面
    /// </summary>
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // 如果用户尚未登录，重定向到前端登录页面
        if (!User.Identity?.IsAuthenticated == true)
        {
            var returnUrl = $"{Request.Scheme}://{Request.Host}/connect/authorize{HttpContext.Request.QueryString}";
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
            var loginUrl = $"{_ssoOptions.LoginBaseUrl}{_ssoOptions.LoginPath}?returnUrl={encodedReturnUrl}";
            return Redirect(loginUrl);
        }

        // 验证客户端应用是否存在
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId);
        if (application == null)
        {
            throw new InvalidOperationException("The client application cannot be found.");
        }

        // 获取当前登录的用户
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            await _signInManager.SignOutAsync();
            var returnUrl = $"{Request.Scheme}://{Request.Host}/connect/authorize{HttpContext.Request.QueryString}";
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
            var loginUrl = $"{_ssoOptions.LoginBaseUrl}{_ssoOptions.LoginPath}?returnUrl={encodedReturnUrl}";
            return Redirect(loginUrl);
        }

        // 获取客户端应用的显示名称
        var appName = (await _applicationManager.GetDisplayNameAsync(application)) ?? request.ClientId;

        // 重定向到前端授权确认页面
        var consentUrl = $"{_ssoOptions.LoginBaseUrl}{_ssoOptions.ConsentPath}" +
            $"?client_id={Uri.EscapeDataString(request.ClientId)}" +
            $"&scope={Uri.EscapeDataString(string.Join(" ", request.GetScopes()))}" +
            $"&redirect_uri={Uri.EscapeDataString(request.RedirectUri ?? "")}" +
            $"&state={Uri.EscapeDataString(request.State ?? "")}" +
            $"&response_type={Uri.EscapeDataString(request.ResponseType ?? "")}" +
            $"&code_challenge={Uri.EscapeDataString(request.CodeChallenge ?? "")}" +
            $"&code_challenge_method={Uri.EscapeDataString(request.CodeChallengeMethod ?? "")}" +
            $"&app_name={Uri.EscapeDataString(appName)}";
        return Redirect(consentUrl);
    }

    /// <summary>
    /// POST /connect/authorize - 用户同意授权后，签发授权码
    /// </summary>
    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizeConfirm()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (!User.Identity?.IsAuthenticated == true)
        {
            var returnUrl = $"{Request.Scheme}://{Request.Host}/connect/authorize{HttpContext.Request.QueryString}";
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
            var loginUrl = $"{_ssoOptions.LoginBaseUrl}{_ssoOptions.LoginPath}?returnUrl={encodedReturnUrl}";
            return Redirect(loginUrl);
        }

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId);
        if (application == null)
        {
            throw new InvalidOperationException("The client application cannot be found.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            await _signInManager.SignOutAsync();
            var returnUrl = $"{Request.Scheme}://{Request.Host}/connect/authorize{HttpContext.Request.QueryString}";
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
            var loginUrl = $"{_ssoOptions.LoginBaseUrl}{_ssoOptions.LoginPath}?returnUrl={encodedReturnUrl}";
            return Redirect(loginUrl);
        }

        // 用户已同意授权，创建身份标识并签发授权码
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        // sub 声明始终包含
        identity.AddClaim(new Claim(Claims.Subject, await _userManager.GetUserIdAsync(user))
            .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

        var scopes = request.GetScopes();

        // 只有用户授权了 profile scope 才包含姓名
        if (scopes.Contains(Scopes.Profile))
        {
            identity.AddClaim(new Claim(Claims.Name, await _userManager.GetUserNameAsync(user))
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        }

        // 只有用户授权了 email scope 才包含邮箱
        if (scopes.Contains(Scopes.Email))
        {
            identity.AddClaim(new Claim(Claims.Email, await _userManager.GetEmailAsync(user))
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        }

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

    [HttpGet("logout")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var request = HttpContext.GetOpenIddictServerRequest();

        await _signInManager.SignOutAsync();

        if (request?.PostLogoutRedirectUri != null)
        {
            return Redirect(request.PostLogoutRedirectUri);
        }

        return Redirect(_ssoOptions.LoginBaseUrl);
    }

    [HttpPost("token")]
    public async Task<IActionResult> Exchange()
    {
        try
        {
            var request = HttpContext.GetOpenIddictServerRequest();
            if (request == null)
            {
                return BadRequest(new { error = "invalid_request", error_description = "The OpenID Connect request cannot be retrieved." });
            }

            if (request.IsAuthorizationCodeGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                if (result?.Principal == null)
                {
                    return Forbid(authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                                  properties: new AuthenticationProperties(new Dictionary<string, string?>
                                  {
                                      [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                                      [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The authorization code is invalid or expired."
                                  }));
                }

                var userId = result.Principal.FindFirst(Claims.Subject)?.Value;
                var user = await _userManager.FindByIdAsync(userId!);
                if (user == null)
                {
                    return Forbid(authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                                  properties: new AuthenticationProperties(new Dictionary<string, string?>
                                  {
                                      [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                                      [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user associated with the authorization code cannot be found."
                                  }));
                }

                // 重新创建 principal，确保包含最新的声明
                var identity = new ClaimsIdentity(
                    result.Principal.Identities.First().Claims,
                    authenticationType: TokenValidationParameters.DefaultAuthenticationType);

                var newPrincipal = new ClaimsPrincipal(identity);
                newPrincipal.SetScopes(request.GetScopes());
                newPrincipal.SetResources(await _scopeManager.ListResourcesAsync(request.GetScopes()).ToListAsync());

                return SignIn(newPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return BadRequest(new { error = "unsupported_grant_type", error_description = "The specified grant type is not supported." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "server_error", error_description = ex.Message });
        }
    }
}
