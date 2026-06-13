using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using TPSSO.Infrastructure.Utils;
using Microsoft.Extensions.Logging;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Infrastructure.Seeding;

public class ClientSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IOpenIddictApplicationManager _manager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ILogger<ClientSeeder> _logger;

    public ClientSeeder(
        ApplicationDbContext context,
        IOpenIddictApplicationManager manager,
        IOpenIddictScopeManager scopeManager,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILogger<ClientSeeder> logger)
    {
        _context = context;
        _manager = manager;
        _scopeManager = scopeManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("===== 开始执行种子数据初始化 =====");

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
        }
        else
        {
            _logger.LogInformation("生产环境：执行数据库迁移...");
            await _context.Database.MigrateAsync();
            _logger.LogInformation("数据库迁移完成");
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
        _logger.LogInformation("开始写入字典配置种子数据...");
        await SeedDictAsync();
        _logger.LogInformation("字典配置种子数据写入完成");

        // 管理后台（tpssoadmin）
        var adminExisting = await _manager.FindByClientIdAsync(SystemClientIds.AdminClient);
        if (adminExisting != null)
        {
            // 已存在则更新配置，确保新增的权限和 URI 同步
            await _manager.UpdateAsync(adminExisting, new OpenIddictApplicationDescriptor
            {
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "TPSSO 管理后台",
                RedirectUris = { new Uri("http://localhost:3009/callback") },
                PostLogoutRedirectUris = { new Uri("http://localhost:3009") },
                Permissions =
                {
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
                }
            });
            _logger.LogInformation("管理后台客户端已更新");
        }
        else
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
                }
            });

            var openIddictId = (string?)openIddictApp.GetType().GetProperty("Id")?.GetValue(openIddictApp);

            // 同步创建业务表记录，让管理页面能看到此客户端
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
                    ConsentType = "implicit",
                    Status = ClientStatus.Approved,
                    CreatedByUserId = creator?.Id ?? Guid.Empty,
                    ReviewedByUserId = creator?.Id ?? Guid.Empty,
                    ReviewedAt = DateTime.UtcNow,
                    RedirectUris = [new ClientRedirectUri { Uri = "http://localhost:3009/callback" }],
                    AllowedScopes = [
                        new ClientScope { Scope = "email" },
                        new ClientScope { Scope = "profile" },
                        new ClientScope { Scope = "roles" },
                        new ClientScope { Scope = "offline_access" }
                    ],
                    GrantTypes = [
                        new ClientGrantType { GrantType = "authorization_code" },
                        new ClientGrantType { GrantType = "refresh_token" }
                    ]
                });
                await _context.SaveChangesAsync();
            }
        }

        _logger.LogInformation("===== 种子数据初始化完成 =====");
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
        var smtpType = new DictType
        {
            Code = "Smtp", Name = "邮件配置", Description = "SMTP 邮件发送配置", Sort = 4,
        };

        _context.DictTypes.AddRange(oauthType, securityType, systemType, smtpType);
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
                    new DictItem { Key = "ClientId", Value = "Ov23lietsdCwxFEgi6Ky", Description = "GitHub OAuth Client ID", Sort = 1 },
                    new DictItem { Key = "ClientSecret", Value = "xxx", Description = "GitHub OAuth Client Secret", IsSensitive = true, Sort = 2 },
                    new DictItem { Key = "IsEnabled", Value = "true", Description = "是否启用 GitHub 登录", Sort = 3 },
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
                    new DictItem { Key = "IsEnabled", Value = "false", Description = "是否启用 Google 登录", Sort = 3 },
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
                    new DictItem { Key = "IsEnabled", Value = "false", Description = "是否启用微信登录", Sort = 3 },
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
                    new DictItem { Key = "VerificationCodeExpireMinutes", Value = "5", Description = "验证码有效期（分钟）", Sort = 3 },
                ]
            },
            new()
            {
                Code = "SmtpServer", Name = "SMTP 配置", Description = "邮件发送服务器配置", Sort = 1,
                ParentId = smtpType.Id,
                Items =
                [
                    new DictItem { Key = "Host", Value = "smtp.qiye.aliyun.com", Description = "SMTP 服务器地址", Sort = 1 },
                    new DictItem { Key = "Port", Value = "587", Description = "SMTP 端口", Sort = 2 },
                    new DictItem { Key = "UseSsl", Value = "true", Description = "是否使用 SSL", Sort = 3 },
                    new DictItem { Key = "Username", Value = "tp@taipi.top", Description = "SMTP 用户名", Sort = 4 },
                    new DictItem { Key = "Password", Value = "", Description = "SMTP 密码", IsSensitive = true, Sort = 5 },
                    new DictItem { Key = "SenderName", Value = "TPSSO", Description = "发件人名称", Sort = 6 },
                    new DictItem { Key = "SenderEmail", Value = "tp@taipi.top", Description = "发件人邮箱", Sort = 7 },
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
