using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using TPSSO.Domain.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Auth.Services;

/// <summary>
/// OAuth 声明构建服务，负责创建用户 ClaimsIdentity 和决定 Claim 目标令牌
/// </summary>
public class ClaimsBuilderService(
    UserManager<User> userManager)
{
    /// <summary>
    /// 创建包含用户声明的 ClaimsIdentity，根据请求的 scopes 决定包含哪些声明
    /// </summary>
    public async Task<ClaimsIdentity> CreateUserClaimsIdentityAsync(User user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, await userManager.GetUserIdAsync(user))
            .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

        if (scopes.Contains(Scopes.Profile))
        {
            var name = await userManager.GetUserNameAsync(user);
            identity.AddClaim(new Claim(Claims.Name, name!)
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        }

        if (scopes.Contains(Scopes.Email))
        {
            var mail = await userManager.GetEmailAsync(user);
            identity.AddClaim(new Claim(Claims.Email, mail!)
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        }

        if (scopes.Contains(Scopes.Roles))
        {
            var roles = await userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(Claims.Role, role)
                    .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
            }
        }

        identity.SetDestinations(GetDestinations);

        return identity;
    }

    /// <summary>
    /// 决定每个 Claim 应该出现在哪些令牌中（access_token / id_token）
    /// </summary>
    public static IEnumerable<string> GetDestinations(Claim claim)
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
