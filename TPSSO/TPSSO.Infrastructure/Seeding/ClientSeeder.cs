using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Infrastructure.Seeding;

public class ClientSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IOpenIddictApplicationManager _manager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public ClientSeeder(
        ApplicationDbContext context,
        IOpenIddictApplicationManager manager,
        IOpenIddictScopeManager scopeManager,
        UserManager<User> userManager,
        RoleManager<Role> roleManager)
    {
        _context = context;
        _manager = manager;
        _scopeManager = scopeManager;
        _userManager = userManager;
        _roleManager = roleManager;
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

        // ──────── Scopes ────────

        if (await _scopeManager.FindByNameAsync("profile") is null)
        {
            await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "profile",
                DisplayName = "Profile information",
                Resources = { "tpsso-api" }
            });
        }

        if (await _scopeManager.FindByNameAsync("email") is null)
        {
            await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "email",
                DisplayName = "Email address",
                Resources = { "tpsso-api" }
            });
        }

        // 注册 roles scope，用于在令牌中包含角色声明
        if (await _scopeManager.FindByNameAsync("roles") is null)
        {
            await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "roles",
                DisplayName = "User roles",
                Resources = { "tpsso-api" }
            });
        }

        // ──────── 角色 ────────

        if (await _roleManager.FindByNameAsync("Admin") is null)
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = "Admin",
                Description = "系统管理员"
            });
        }

        // ──────── 管理员用户 ────────

        const string adminEmail = "admin@taipi.top";
        const string adminPassword = "Admin@123";

        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new User { UserName = adminEmail, Email = adminEmail, NickName = "管理员" };
            var result = await _userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                Console.WriteLine($"管理员用户 '{adminEmail}' 创建成功。");
            }
            else
            {
                Console.WriteLine($"创建管理员用户失败: {string.Join(", ", result.Errors)}");
            }
        }

        // 确保管理员用户拥有 Admin 角色
        if (adminUser is not null && !await _userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // ──────── 测试用户 ────────

        const string testEmail = "tp@taipi.top";
        const string testPassword = "Admin@123";

        if (await _userManager.FindByEmailAsync(testEmail) is null)
        {
            var user = new User { UserName = testEmail, Email = testEmail };
            var result = await _userManager.CreateAsync(user, testPassword);
            if (result.Succeeded)
            {
                Console.WriteLine($"测试用户 '{testEmail}' 创建成功。");
            }
            else
            {
                Console.WriteLine($"创建测试用户失败: {string.Join(", ", result.Errors)}");
            }
        }

        // ──────── OAuth 客户端 ────────

        // 用户前端（tpssoweb）
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
        }

        // 管理后台（tpssoadmin）
        if (await _manager.FindByClientIdAsync("tpsso_admin_client") == null)
        {
            await _manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "tpsso_admin_client",
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "Admin Client",
                RedirectUris = { new Uri("http://localhost:3009/callback") },
                PostLogoutRedirectUris = { new Uri("http://localhost:3009") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.EndSession,
                    Permissions.ResponseTypes.Code,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles
                }
            });
        }
    }
}
