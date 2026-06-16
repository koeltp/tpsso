using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using TPSSO.Domain.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Auth.Services;

/// <summary>
/// Token 授权类型处理服务，负责各 GrantType 的 Token 签发逻辑
/// </summary>
public class TokenGrantService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    ClaimsBuilderService claimsBuilder,
    ILogger<TokenGrantService> logger)
{
    /// <summary>
    /// 处理授权码换 Token
    /// </summary>
    public async Task<IActionResult> HandleAuthorizationCodeGrantAsync(HttpContext httpContext, OpenIddictRequest request)
    {
        var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (result?.Principal == null)
            return ForbidWithError(Errors.InvalidGrant, "授权码无效或已过期。");

        var userId = result.Principal.FindFirst(Claims.Subject)?.Value;
        var user = await userManager.FindByIdAsync(userId!);
        if (user == null)
            return ForbidWithError(Errors.InvalidGrant, "关联用户不存在。");

        // 重新创建 principal，确保包含最新的声明
        var identity = new ClaimsIdentity(
            result.Principal.Identities.First().Claims,
            authenticationType: TokenValidationParameters.DefaultAuthenticationType);

        var newPrincipal = new ClaimsPrincipal(identity);
        newPrincipal.SetScopes(request.GetScopes());
        newPrincipal.SetResources(await scopeManager.ListResourcesAsync(request.GetScopes()).ToListAsync());

        return new Microsoft.AspNetCore.Mvc.SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, newPrincipal);
    }

    /// <summary>
    /// 处理 Refresh Token 刷新
    /// </summary>
    public async Task<IActionResult> HandleRefreshTokenGrantAsync(HttpContext httpContext, OpenIddictRequest request)
    {
        logger.LogInformation("收到 Refresh Token 刷新请求。");

        var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (result?.Principal == null)
            return ForbidWithError(Errors.InvalidGrant, "Refresh Token 无效或已过期。");

        var userId = result.Principal.FindFirst(Claims.Subject)?.Value;
        var user = await userManager.FindByIdAsync(userId!);
        if (user == null)
            return ForbidWithError(Errors.InvalidGrant, "关联用户不存在。");

        // 确保用户仍然有权登录（未被禁用等）
        if (!await signInManager.CanSignInAsync(user))
            return ForbidWithError(Errors.InvalidGrant, "用户已被禁用。");

        // 重新构建 principal，确保声明是最新的
        var identity = await claimsBuilder.CreateUserClaimsIdentityAsync(user, request.GetScopes());
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources(await scopeManager.ListResourcesAsync(request.GetScopes()).ToListAsync());

        return new Microsoft.AspNetCore.Mvc.SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
    }

    /// <summary>
    /// 处理客户端凭证授权（M2M，无用户参与）
    /// </summary>
    public async Task<IActionResult> HandleClientCredentialsGrantAsync(OpenIddictRequest request)
    {
        logger.LogInformation("收到 Client Credentials 请求，ClientId: {ClientId}", request.ClientId);

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!) ??
            throw new InvalidOperationException("客户端应用不存在。");

        // 创建客户端身份（无用户）
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, await applicationManager.GetIdAsync(application) ?? "")
            .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        identity.AddClaim(new Claim(Claims.Name, await applicationManager.GetDisplayNameAsync(application) ?? "")
            .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

        var scopes = request.GetScopes();
        identity.SetScopes(scopes);
        identity.SetResources(await scopeManager.ListResourcesAsync(scopes).ToListAsync());

        var principal = new ClaimsPrincipal(identity);

        return new Microsoft.AspNetCore.Mvc.SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
    }

    /// <summary>
    /// 处理设备码授权（用户已在其他设备上确认授权）
    /// </summary>
    public async Task<IActionResult> HandleDeviceCodeGrantAsync(HttpContext httpContext, OpenIddictRequest request)
    {
        logger.LogInformation("收到 Device Code 请求，ClientId: {ClientId}", request.ClientId);

        var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (result?.Principal == null)
            return ForbidWithError(Errors.InvalidGrant, "设备码无效或已过期。");

        var userId = result.Principal.FindFirst(Claims.Subject)?.Value;
        if (string.IsNullOrEmpty(userId))
            return ForbidWithError(Errors.InvalidGrant, "设备码尚未被用户确认。");

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return ForbidWithError(Errors.InvalidGrant, "关联用户不存在。");

        var identity = await claimsBuilder.CreateUserClaimsIdentityAsync(user, request.GetScopes());
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources(await scopeManager.ListResourcesAsync(request.GetScopes()).ToListAsync());

        return new Microsoft.AspNetCore.Mvc.SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
    }

    /// <summary>
    /// 返回 OAuth 错误响应
    /// </summary>
    private static ForbidResult ForbidWithError(string error, string description)
    {
        return new ForbidResult(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));
    }
}
