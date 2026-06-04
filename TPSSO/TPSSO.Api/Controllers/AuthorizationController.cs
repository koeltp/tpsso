using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
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

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("authorize")]
    [HttpPost("authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // 1. 如果用户尚未登录，重定向到前端登录页面
        if (!User.Identity?.IsAuthenticated == true)
        {
            // 保存当前授权请求的完整URL，以便登录后返回
            var returnUrl = $"{Request.Scheme}://{Request.Host}/connect/authorize{HttpContext.Request.QueryString}";
            Console.WriteLine($"========================={HttpContext.Request.QueryString}==================================");
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
            var loginUrl = $"http://localhost:3008/custom-login?returnUrl={encodedReturnUrl}";
            return Redirect(loginUrl);
        }

        // 2. 用户已登录，验证客户端应用是否存在
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId);
        if (application == null)
        {
            throw new InvalidOperationException("The client application cannot be found.");
        }

        // 3. 获取当前登录的用户
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            // 用户不存在，需要登出并重定向到登录页
            await _signInManager.SignOutAsync();
            var returnUrl = $"{Request.Scheme}://{Request.Host}/connect/authorize{HttpContext.Request.QueryString}";
            Console.WriteLine($"111111111111111111111111111111111111111111111111111111111111111111111111111111111{HttpContext.Request.QueryString}==================================");
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
            var loginUrl = $"http://localhost:3008/custom-login?returnUrl={encodedReturnUrl}";
            return Redirect(loginUrl);
        }

        // 4. 创建身份标识（ClaimsIdentity）
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        // 添加必要的声明（sub 是必须的）
        identity.AddClaim(new Claim(Claims.Subject, await _userManager.GetUserIdAsync(user))
            .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        identity.AddClaim(new Claim(Claims.Name, await _userManager.GetUserNameAsync(user))
            .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        identity.AddClaim(new Claim(Claims.Email, await _userManager.GetEmailAsync(user))
            .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

        // 5. 添加用户角色声明（如果有）
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(Claims.Role, role)
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        }

        // 6. 处理请求的 scopes
        var scopes = request.GetScopes(); // 返回 ImmutableArray<string>
        identity.SetScopes(scopes);
        identity.SetResources(await _scopeManager.ListResourcesAsync(scopes).ToListAsync());

        // 7. 创建 AuthenticationTicket
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            new AuthenticationProperties(),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // 8. 可选：设置授权码的过期时间等（这里使用默认）
        // 9. 返回 SignIn 结果，OpenIddict 会自动生成授权码并重定向到 redirect_uri
        return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
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
                // 授权码交换访问令牌
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

                // 从 principal 中获取用户 ID
                var userId = result.Principal.FindFirst(Claims.Subject)?.Value;
                var user = await _userManager.FindByIdAsync(userId);
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
                // 注意：principal 中已包含 sub 声明（在 Authorize 方法中添加），无需重复添加

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