using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Infrastructure.Seeding;

/// <summary>
/// 测试客户端种子数据，仅在开发环境使用（仅创建，不更新）
/// </summary>
public class TestClientSeeder(
    ApplicationDbContext context,
    IOpenIddictApplicationManager manager,
    UserManager<User> userManager,
    ILogger<TestClientSeeder> logger)
{
    private static readonly ClientSeedConfig[] ClientConfigs =
    [
        // Authorization Code + PKCE（SPA 公开客户端）
        new()
        {
            ClientId = SystemClientIds.TestAuthCodePKCE,
            DisplayName = "测试客户端 - AuthCode+PKCE",
            Description = "用于测试 Authorization Code + PKCE 授权流程的 SPA 客户端",
            ConsentType = ConsentTypes.Implicit,
            IsPublic = true,
            Type = ClientTypes.Public,
            RedirectUris =
            [
                "http://localhost:3000/",
                "http://localhost:3000",
                "http://localhost:3000/index.html"
            ],
            PostLogoutRedirectUris =
            [
                "http://localhost:3000",
                "http://localhost:3000/",
                "http://localhost:3000/index.html"
            ],
            Permissions =
            [
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
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
        },
        // Client Credentials（M2M 机密客户端）
        new()
        {
            ClientId = SystemClientIds.TestClientCredentials,
            ClientSecret = "test_secret_123",
            DisplayName = "测试客户端 - ClientCredentials",
            Description = "用于测试 Client Credentials 授权流程的机密客户端（M2M）",
            ConsentType = ConsentTypes.Implicit,
            IsPublic = false,
            Permissions =
            [
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Introspection,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles
            ],
            AllowedScopes = ["openid", "email", "profile", "roles"],
            GrantTypes = ["client_credentials"]
        },
        // Device Code（设备码授权，公开客户端）
        new()
        {
            ClientId = SystemClientIds.TestDeviceCode,
            DisplayName = "测试客户端 - DeviceCode",
            Description = "用于测试 Device Code 授权流程的公开客户端（IoT/CLI）",
            ConsentType = ConsentTypes.Implicit,
            IsPublic = true,
            Type = ClientTypes.Public,
            Permissions =
            [
                Permissions.Endpoints.DeviceAuthorization,
                Permissions.Endpoints.Token,
                Permissions.Prefixes.Endpoint + "end_user_verification",
                Permissions.GrantTypes.DeviceCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                "scp:offline_access"
            ],
            AllowedScopes = ["openid", "email", "profile", "roles", "offline_access"],
            GrantTypes = ["urn:ietf:params:oauth:grant-type:device_code", "refresh_token"]
        }
    ];

    public async Task SeedAsync()
    {
        logger.LogInformation("===== 开始注册测试客户端 =====");

        var creator = await userManager.FindByEmailAsync("admin@taipi.top");

        foreach (var config in ClientConfigs)
        {
            await CreateClientAsync(config, creator);
        }

        logger.LogInformation("===== 测试客户端注册完成 =====");
    }

    private async Task CreateClientAsync(ClientSeedConfig config, User? creator)
    {
        // 已存在则跳过
        if (await manager.FindByClientIdAsync(config.ClientId) is not null)
        {
            return;
        }

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

        var app = await manager.CreateAsync(descriptor);
        var openIddictId = (string?)app.GetType().GetProperty("Id")?.GetValue(app);

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

        logger.LogInformation("测试客户端 '{ClientId}' 已创建", config.ClientId);
    }
}
