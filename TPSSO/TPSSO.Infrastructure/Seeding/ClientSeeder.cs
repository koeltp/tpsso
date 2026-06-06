using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TPSSO.Infrastructure.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Infrastructure.Seeding;

public class ClientSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IOpenIddictApplicationManager _manager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly UserManager<IdentityUser> _userManager;

    public ClientSeeder(
        ApplicationDbContext context,
        IOpenIddictApplicationManager manager,
        IOpenIddictScopeManager scopeManager,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _manager = manager;
        _scopeManager = scopeManager;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
        }
        else
        {
            await _context.Database.MigrateAsync();
        }

        // 注册 profile scope
        if (await _scopeManager.FindByNameAsync("profile") is null)
        {
            await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "profile",
                DisplayName = "Profile information",
                Resources = { "tpsso-api" }
            });
        }

        // 注册 email scope
        if (await _scopeManager.FindByNameAsync("email") is null)
        {
            await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "email",
                DisplayName = "Email address",
                Resources = { "tpsso-api" }
            });
        }

        // 注册 SPA 客户端
        if (await _manager.FindByClientIdAsync("tpsso_spa_client") == null)
        {
            await _manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "tpsso_spa_client",
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "SPA Client",
                RedirectUris = { new Uri("http://localhost:3007/callback") },
                PostLogoutRedirectUris = { new Uri("http://localhost:3007") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.EndSession,
                    Permissions.ResponseTypes.Code,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile
                }
            });

            // 创建测试用户
            string testEmail = "tp@taipi.top";
            string testPassword = "Admin@123";

            if (await _userManager.FindByEmailAsync(testEmail) == null)
            {
                var user = new IdentityUser { UserName = testEmail, Email = testEmail };
                var result = await _userManager.CreateAsync(user, testPassword);
                if (result.Succeeded)
                {
                    Console.WriteLine($"测试用户 '{testEmail}' 创建成功。");
                }
                else
                {
                    Console.WriteLine($"创建用户失败: {string.Join(", ", result.Errors)}");
                }
            }
        }
    }
}
