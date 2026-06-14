using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Infrastructure.Seeding;

/// <summary>
/// OAuth 客户端种子数据初始化
/// 采用 Upsert 模式：已存在则更新，不存在则创建，配置只需定义一次
/// </summary>
public class ClientSeeder(
    ApplicationDbContext context,
    IOpenIddictApplicationManager manager,
    UserManager<User> userManager,
    ILogger<ClientSeeder> logger)
{
    /// <summary>
    /// 客户端配置定义，Create 和 Update 共用同一份配置
    /// </summary>
    private static readonly ClientSeedConfig[] ClientConfigs =
    [
        new()
        {
            ClientId = SystemClientIds.AdminClient,
            DisplayName = "TPSSO 管理后台",
            Description = "TPSSO 后台管理系统",
            ConsentType = ConsentTypes.Implicit,
            IsPublic = true,
            RedirectUris = ["http://localhost:3009/callback"],
            PostLogoutRedirectUris = ["http://localhost:3009"],
            Permissions =
            [
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Introspection,
                Permissions.Endpoints.Revocation,
                Permissions.Endpoints.DeviceAuthorization,
                Permissions.Prefixes.Endpoint + "end_user_verification",
                Permissions.ResponseTypes.Code,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                "scp:offline_access"
            ],
            AllowedScopes = ["openid", "email", "profile", "roles", "offline_access"],
            GrantTypes = ["authorization_code", "refresh_token"]
        }
    ];

    public async Task SeedAsync()
    {
        var creator = await userManager.FindByEmailAsync("admin@taipi.top");

        foreach (var config in ClientConfigs)
        {
            await UpsertClientAsync(config, creator);
        }
    }

    /// <summary>
    /// Upsert 客户端：已存在则更新配置，不存在则创建
    /// </summary>
    private async Task UpsertClientAsync(ClientSeedConfig config, User? creator)
    {
        var existing = await manager.FindByClientIdAsync(config.ClientId);

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = config.ClientId,
            ClientSecret = config.ClientSecret,
            ConsentType = config.ConsentType,
            DisplayName = config.DisplayName,
        };

        foreach (var uri in config.RedirectUris) descriptor.RedirectUris.Add(new Uri(uri));
        foreach (var uri in config.PostLogoutRedirectUris) descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
        foreach (var perm in config.Permissions) descriptor.Permissions.Add(perm);

        if (existing != null)
        {
            await manager.UpdateAsync(existing, descriptor);
            logger.LogInformation("客户端 '{ClientId}' 已更新", config.ClientId);
        }
        else
        {
            var app = await manager.CreateAsync(descriptor);
            var openIddictId = (string?)app.GetType().GetProperty("Id")?.GetValue(app);

            // 同步创建业务表记录
            if (!await context.ClientApplications.AnyAsync(c => c.ClientId == config.ClientId))
            {
                context.ClientApplications.Add(new ClientApplication
                {
                    ClientId = config.ClientId,
                    OpenIddictApplicationId = openIddictId,
                    Name = config.DisplayName,
                    Description = config.Description,
                    IsPublic = config.IsPublic,
                    ConsentType = config.ConsentType,
                    Status = ClientStatus.Approved,
                    CreatedByUserId = creator?.Id ?? Guid.Empty,
                    ReviewedByUserId = creator?.Id ?? Guid.Empty,
                    ReviewedAt = DateTime.UtcNow,
                    RedirectUris = config.RedirectUris.Select(u => new ClientRedirectUri { Uri = u }).ToList(),
                    AllowedScopes = config.AllowedScopes.Select(s => new ClientScope { Scope = s }).ToList(),
                    GrantTypes = config.GrantTypes.Select(g => new ClientGrantType { GrantType = g }).ToList()
                });
                await context.SaveChangesAsync();
            }

            logger.LogInformation("客户端 '{ClientId}' 已创建", config.ClientId);
        }
    }
}
