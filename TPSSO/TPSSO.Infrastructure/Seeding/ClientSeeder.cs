using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using TPSSO.Infrastructure.Utils;
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

        if (await _roleManager.FindByNameAsync(RoleConstants.Admin) is null)
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = RoleConstants.Admin,
                Description = "系统管理员"
            });
        }

        if (await _roleManager.FindByNameAsync(RoleConstants.User) is null)
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = RoleConstants.User,
                Description = "普通用户"
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
        if (adminUser is not null && !await _userManager.IsInRoleAsync(adminUser, RoleConstants.Admin))
        {
            await _userManager.AddToRoleAsync(adminUser, RoleConstants.Admin);
        }

        // ──────── 测试用户 ────────

        const string testEmail = "tp@taipi.top";
        const string testPassword = "Admin@123";

        if (await _userManager.FindByEmailAsync(testEmail) is null)
        {
            var user = new User { UserName = testEmail, Email = testEmail, NickName = "测试用户" };
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

        // 确保测试用户拥有 User 角色
        var testUser = await _userManager.FindByEmailAsync(testEmail);
        if (testUser is not null && !await _userManager.IsInRoleAsync(testUser, RoleConstants.User))
        {
            await _userManager.AddToRoleAsync(testUser, RoleConstants.User);
        }

        // ──────── OAuth 客户端 ────────

        // 字典配置种子数据
        await SeedDictAsync();

        // 管理后台（tpssoadmin）
        if (await _manager.FindByClientIdAsync(SystemClientIds.AdminClient) == null)
        {
            var openIddictApp = await _manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = SystemClientIds.AdminClient,
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "TPSSO 管理后台",
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

            // 同步创建业务表记录，让管理页面能看到此客户端
            var openIddictId = (string?)openIddictApp.GetType().GetProperty("Id")?.GetValue(openIddictApp);
            if (!await _context.ClientApplications.AnyAsync(c => c.ClientId == SystemClientIds.AdminClient))
            {
                var creator = await _userManager.FindByEmailAsync(adminEmail);
                _context.ClientApplications.Add(new ClientApplication
                {
                    ClientId = SystemClientIds.AdminClient,
                    OpenIddictApplicationId = openIddictId,
                    Name = "TPSSO 管理后台",
                    Description = "TPSSO 后台管理系统",
                    IsPublic = true,
                    Status = ClientStatus.Approved,
                    CreatedByUserId = creator?.Id ?? Guid.Empty,
                    ReviewedByUserId = creator?.Id ?? Guid.Empty,
                    ReviewedAt = DateTime.UtcNow,
                    RedirectUris = [new ClientRedirectUri { Uri = "http://localhost:3009/callback" }],
                    AllowedScopes = [
                        new ClientScope { Scope = "email" },
                        new ClientScope { Scope = "profile" },
                        new ClientScope { Scope = "roles" }
                    ]
                });
                await _context.SaveChangesAsync();
            }
        }

        // 用户门户（tpssoportal）
        if (await _manager.FindByClientIdAsync(SystemClientIds.PortalClient) == null)
        {
            var openIddictApp = await _manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = SystemClientIds.PortalClient,
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "TPSSO 用户门户",
                RedirectUris = { new Uri("http://localhost:3011/callback") },
                PostLogoutRedirectUris = { new Uri("http://localhost:3011") },
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

            var openIddictId = (string?)openIddictApp.GetType().GetProperty("Id")?.GetValue(openIddictApp);
            if (!await _context.ClientApplications.AnyAsync(c => c.ClientId == SystemClientIds.PortalClient))
            {
                var creator = await _userManager.FindByEmailAsync(adminEmail);
                _context.ClientApplications.Add(new ClientApplication
                {
                    ClientId = SystemClientIds.PortalClient,
                    OpenIddictApplicationId = openIddictId,
                    Name = "TPSSO 用户门户",
                    Description = "TPSSO 用户自助服务门户",
                    IsPublic = true,
                    Status = ClientStatus.Approved,
                    CreatedByUserId = creator?.Id ?? Guid.Empty,
                    ReviewedByUserId = creator?.Id ?? Guid.Empty,
                    ReviewedAt = DateTime.UtcNow,
                    RedirectUris = [new ClientRedirectUri { Uri = "http://localhost:3011/callback" }],
                    AllowedScopes = [
                        new ClientScope { Scope = "email" },
                        new ClientScope { Scope = "profile" },
                        new ClientScope { Scope = "roles" }
                    ]
                });
                await _context.SaveChangesAsync();
            }
        }
    }

    // ──────── 字典配置种子数据 ────────
    private async Task SeedDictAsync()
    {
        if (await _context.DictTypes.AnyAsync()) return;

        // 先创建父分类
        var oauthType = new DictType
        {
            Code = "OAuth", Name = "第三方登录", Description = "第三方 OAuth 登录配置", Sort = 1,
        };
        var securityType = new DictType
        {
            Code = "Security", Name = "安全配置", Description = "JWT、密码策略等安全相关配置", Sort = 2,
        };
        var systemType = new DictType
        {
            Code = "System", Name = "系统配置", Description = "系统全局配置", Sort = 3,
        };

        _context.DictTypes.AddRange(oauthType, securityType, systemType);
        await _context.SaveChangesAsync();

        // 子分类和配置项
        var types = new List<DictType>
        {
            new()
            {
                Code = "GitHub", Name = "GitHub", Description = "GitHub OAuth 登录", Sort = 1,
                ParentId = oauthType.Id,
                Items =
                [
                    new DictItem { Key = "ClientId", Value = "", Description = "GitHub OAuth Client ID", Sort = 1 },
                    new DictItem { Key = "ClientSecret", Value = "", Description = "GitHub OAuth Client Secret", IsSensitive = true, Sort = 2 },
                    new DictItem { Key = "CallbackUrl", Value = "", Description = "GitHub OAuth 回调地址", Sort = 3 },
                ]
            },
            new()
            {
                Code = "Google", Name = "Google", Description = "Google OAuth 登录", Sort = 2,
                ParentId = oauthType.Id,
                Items =
                [
                    new DictItem { Key = "ClientId", Value = "", Description = "Google OAuth Client ID", Sort = 1 },
                    new DictItem { Key = "ClientSecret", Value = "", Description = "Google OAuth Client Secret", IsSensitive = true, Sort = 2 },
                    new DictItem { Key = "CallbackUrl", Value = "", Description = "Google OAuth 回调地址", Sort = 3 },
                ]
            },
            new()
            {
                Code = "WeChat", Name = "微信", Description = "微信 OAuth 登录", Sort = 3,
                ParentId = oauthType.Id,
                Items =
                [
                    new DictItem { Key = "AppId", Value = "", Description = "微信 App ID", Sort = 1 },
                    new DictItem { Key = "AppSecret", Value = "", Description = "微信 App Secret", IsSensitive = true, Sort = 2 },
                    new DictItem { Key = "CallbackUrl", Value = "", Description = "微信回调地址", Sort = 3 },
                ]
            },
            new()
            {
                Code = "JWT", Name = "JWT 配置", Description = "JWT Token 过期时间等配置", Sort = 1,
                ParentId = securityType.Id,
                Items =
                [
                    new DictItem { Key = "AccessTokenExpireMinutes", Value = "120", Description = "Access Token 过期时间（分钟）", Sort = 1 },
                    new DictItem { Key = "RefreshTokenExpireDays", Value = "7", Description = "Refresh Token 过期时间（天）", Sort = 2 },
                ]
            },
            new()
            {
                Code = "Password", Name = "密码策略", Description = "密码强度要求配置", Sort = 2,
                ParentId = securityType.Id,
                Items =
                [
                    new DictItem { Key = "MinLength", Value = "6", Description = "最小长度", Sort = 1 },
                    new DictItem { Key = "RequireUppercase", Value = "true", Description = "是否要求大写字母", Sort = 2 },
                    new DictItem { Key = "RequireLowercase", Value = "true", Description = "是否要求小写字母", Sort = 3 },
                    new DictItem { Key = "RequireDigit", Value = "true", Description = "是否要求数字", Sort = 4 },
                    new DictItem { Key = "RequireNonAlphanumeric", Value = "true", Description = "是否要求特殊字符", Sort = 5 },
                ]
            },
            new()
            {
                Code = "Upload", Name = "上传配置", Description = "文件上传相关配置", Sort = 1,
                ParentId = systemType.Id,
                Items =
                [
                    new DictItem { Key = "MaxAvatarSizeKB", Value = "512", Description = "头像最大大小（KB）", Sort = 1 },
                    new DictItem { Key = "AllowedAvatarTypes", Value = ".jpg,.jpeg,.png,.gif,.webp", Description = "允许的头像文件类型", Sort = 2 },
                ]
            },
            new()
            {
                Code = "General", Name = "通用配置", Description = "站点通用配置", Sort = 2,
                ParentId = systemType.Id,
                Items =
                [
                    new DictItem { Key = "SiteName", Value = "TPSSO", Description = "站点名称", Sort = 1 },
                    new DictItem { Key = "AllowRegistration", Value = "true", Description = "是否允许注册", Sort = 2 },
                ]
            },
        };

        // 敏感配置加密存储
        foreach (var type in types)
        {
            foreach (var item in type.Items.Where(i => i.IsSensitive && !string.IsNullOrEmpty(i.Value)))
            {
                item.Value = AesEncryption.Encrypt(item.Value);
            }
        }

        _context.DictTypes.AddRange(types);
        await _context.SaveChangesAsync();
    }
}
