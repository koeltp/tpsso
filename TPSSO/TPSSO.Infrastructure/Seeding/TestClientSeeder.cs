using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Infrastructure.Seeding;

/// <summary>
/// 测试客户端种子数据，仅在开发环境使用
/// 支持幂等操作：客户端已存在时会更新配置
/// </summary>
public class TestClientSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IOpenIddictApplicationManager _manager;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<TestClientSeeder> _logger;

    public TestClientSeeder(
        ApplicationDbContext context,
        IOpenIddictApplicationManager manager,
        UserManager<User> userManager,
        ILogger<TestClientSeeder> logger)
    {
        _context = context;
        _manager = manager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("===== 开始注册测试客户端 =====");

        var creator = await _userManager.FindByEmailAsync("admin@taipi.top");

        await SeedAuthCodePKCEClientAsync(creator);
        await SeedClientCredentialsClientAsync(creator);
        await SeedDeviceCodeClientAsync(creator);

        _logger.LogInformation("===== 测试客户端注册完成 =====");
    }

    /// <summary>
    /// Authorization Code + PKCE（SPA 公开客户端）
    /// </summary>
    private async Task SeedAuthCodePKCEClientAsync(User? creator)
    {
        var existing = await _manager.FindByClientIdAsync(SystemClientIds.TestAuthCodePKCE);
        if (existing != null)
        {
            // 已存在则更新配置
            await _manager.UpdateAsync(existing, new OpenIddictApplicationDescriptor
            {
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "测试客户端 - AuthCode+PKCE",
                RedirectUris =
                {
                    new Uri("http://localhost:3000/"),
                    new Uri("http://localhost:3000"),
                    new Uri("http://localhost:3000/index.html"),
                },
                PostLogoutRedirectUris =
                {
                    new Uri("http://localhost:3000"),
                    new Uri("http://localhost:3000/"),
                    new Uri("http://localhost:3000/index.html"),
                },
                Permissions =
                {
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
                }
            });
            _logger.LogInformation("测试客户端 AuthCode+PKCE 已更新");
            return;
        }

        var app = await _manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = SystemClientIds.TestAuthCodePKCE,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "测试客户端 - AuthCode+PKCE",
            RedirectUris =
            {
                new Uri("http://localhost:3000/"),
                new Uri("http://localhost:3000"),
                new Uri("http://localhost:3000/index.html"),
            },
            PostLogoutRedirectUris =
            {
                new Uri("http://localhost:3000"),
                new Uri("http://localhost:3000/"),
                new Uri("http://localhost:3000/index.html"),
            },
            Permissions =
            {
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
            }
        });

        var openIddictId = (string?)app.GetType().GetProperty("Id")?.GetValue(app);
        if (!await _context.ClientApplications.AnyAsync(c => c.ClientId == SystemClientIds.TestAuthCodePKCE))
        {
            _context.ClientApplications.Add(new ClientApplication
            {
                ClientId = SystemClientIds.TestAuthCodePKCE,
                OpenIddictApplicationId = openIddictId,
                Name = "测试客户端 - AuthCode+PKCE",
                Description = "用于测试 Authorization Code + PKCE 授权流程的 SPA 客户端",
                IsPublic = true,
                ConsentType = "implicit",
                Status = ClientStatus.Approved,
                CreatedByUserId = creator?.Id ?? Guid.Empty,
                ReviewedByUserId = creator?.Id ?? Guid.Empty,
                ReviewedAt = DateTime.UtcNow,
                RedirectUris =
                [
                    new ClientRedirectUri { Uri = "http://localhost:3000/" },
                    new ClientRedirectUri { Uri = "http://localhost:3000" },
                    new ClientRedirectUri { Uri = "http://localhost:3000/index.html" },
                ],
                AllowedScopes =
                [
                    new ClientScope { Scope = "openid" },
                    new ClientScope { Scope = "email" },
                    new ClientScope { Scope = "profile" },
                    new ClientScope { Scope = "roles" },
                    new ClientScope { Scope = "offline_access" },
                ],
                GrantTypes =
                [
                    new ClientGrantType { GrantType = "authorization_code" },
                    new ClientGrantType { GrantType = "refresh_token" },
                ]
            });
            await _context.SaveChangesAsync();
        }
        _logger.LogInformation("测试客户端 AuthCode+PKCE 注册完成");
    }

    /// <summary>
    /// Client Credentials（M2M 机密客户端）
    /// </summary>
    private async Task SeedClientCredentialsClientAsync(User? creator)
    {
        var existing = await _manager.FindByClientIdAsync(SystemClientIds.TestClientCredentials);
        if (existing != null)
        {
            await _manager.UpdateAsync(existing, new OpenIddictApplicationDescriptor
            {
                ClientSecret = "test_secret_123",
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "测试客户端 - ClientCredentials",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.Introspection,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles
                }
            });
            _logger.LogInformation("测试客户端 ClientCredentials 已更新");
            return;
        }

        var app = await _manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = SystemClientIds.TestClientCredentials,
            ClientSecret = "test_secret_123",
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "测试客户端 - ClientCredentials",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Introspection,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles
            }
        });

        var openIddictId = (string?)app.GetType().GetProperty("Id")?.GetValue(app);
        if (!await _context.ClientApplications.AnyAsync(c => c.ClientId == SystemClientIds.TestClientCredentials))
        {
            _context.ClientApplications.Add(new ClientApplication
            {
                ClientId = SystemClientIds.TestClientCredentials,
                OpenIddictApplicationId = openIddictId,
                Name = "测试客户端 - ClientCredentials",
                Description = "用于测试 Client Credentials 授权流程的机密客户端（M2M）",
                IsPublic = false,
                ConsentType = "implicit",
                Status = ClientStatus.Approved,
                CreatedByUserId = creator?.Id ?? Guid.Empty,
                ReviewedByUserId = creator?.Id ?? Guid.Empty,
                ReviewedAt = DateTime.UtcNow,
                AllowedScopes =
                [
                    new ClientScope { Scope = "openid" },
                    new ClientScope { Scope = "email" },
                    new ClientScope { Scope = "profile" },
                    new ClientScope { Scope = "roles" },
                ],
                GrantTypes =
                [
                    new ClientGrantType { GrantType = "client_credentials" },
                ]
            });
            await _context.SaveChangesAsync();
        }
        _logger.LogInformation("测试客户端 ClientCredentials 注册完成");
    }

    /// <summary>
    /// Device Code（设备码授权，公开客户端）
    /// </summary>
    private async Task SeedDeviceCodeClientAsync(User? creator)
    {
        var existing = await _manager.FindByClientIdAsync(SystemClientIds.TestDeviceCode);
        if (existing != null)
        {
            await _manager.UpdateAsync(existing, new OpenIddictApplicationDescriptor
            {
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "测试客户端 - DeviceCode",
                Permissions =
                {
                    Permissions.Endpoints.DeviceAuthorization,
                    Permissions.Endpoints.Token,
                    Permissions.Prefixes.Endpoint + "end_user_verification",
                    Permissions.GrantTypes.DeviceCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    "scp:offline_access"
                }
            });
            _logger.LogInformation("测试客户端 DeviceCode 已更新");
            return;
        }

        var app = await _manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = SystemClientIds.TestDeviceCode,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "测试客户端 - DeviceCode",
            Permissions =
            {
                Permissions.Endpoints.DeviceAuthorization,
                Permissions.Endpoints.Token,
                Permissions.Prefixes.Endpoint + "end_user_verification",
                Permissions.GrantTypes.DeviceCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                "scp:offline_access"
            }
        });

        var openIddictId = (string?)app.GetType().GetProperty("Id")?.GetValue(app);
        if (!await _context.ClientApplications.AnyAsync(c => c.ClientId == SystemClientIds.TestDeviceCode))
        {
            _context.ClientApplications.Add(new ClientApplication
            {
                ClientId = SystemClientIds.TestDeviceCode,
                OpenIddictApplicationId = openIddictId,
                Name = "测试客户端 - DeviceCode",
                Description = "用于测试 Device Code 授权流程的公开客户端（IoT/CLI）",
                IsPublic = true,
                ConsentType = "implicit",
                Status = ClientStatus.Approved,
                CreatedByUserId = creator?.Id ?? Guid.Empty,
                ReviewedByUserId = creator?.Id ?? Guid.Empty,
                ReviewedAt = DateTime.UtcNow,
                AllowedScopes =
                [
                    new ClientScope { Scope = "openid" },
                    new ClientScope { Scope = "email" },
                    new ClientScope { Scope = "profile" },
                    new ClientScope { Scope = "roles" },
                    new ClientScope { Scope = "offline_access" },
                ],
                GrantTypes =
                [
                    new ClientGrantType { GrantType = "urn:ietf:params:oauth:grant-type:device_code" },
                    new ClientGrantType { GrantType = "refresh_token" },
                ]
            });
            await _context.SaveChangesAsync();
        }
        _logger.LogInformation("测试客户端 DeviceCode 注册完成");
    }
}
