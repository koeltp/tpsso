using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Api
{
    public class ClientSeeder
    {

        private readonly ApplicationDbContext context;
        private readonly IOpenIddictApplicationManager manager;
        private readonly IOpenIddictScopeManager scopeManager;
        private readonly UserManager<IdentityUser> userManager;
        public ClientSeeder(ApplicationDbContext context, IOpenIddictApplicationManager manager, IOpenIddictScopeManager scopeManager, UserManager<IdentityUser> userManager)
        {
            this.context = context;
            this.manager = manager;
            this.userManager = userManager;
            this.scopeManager = scopeManager;
        }

        public async Task SeedAsync()
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.MigrateAsync();   // 自动应用迁移
            }




            // ========== 1. 注册 Scopes（作用域） ==========
            // 创建 profile scope
            if (await scopeManager.FindByNameAsync("profile") is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "profile",
                    DisplayName = "Profile information",
                    Resources = { "tpsso-api" } // 资源名称，可以自定义
                });
            }

            // 如果有 email scope 也可以类似创建
            if (await scopeManager.FindByNameAsync("email") is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "email",
                    DisplayName = "Email address",
                    Resources = { "tpsso-api" }
                });
            }

            // 注册一个 SPA 客户端（第三方应用）
            if (await manager.FindByClientIdAsync("tpsso_spa_client") == null)
            {
                await manager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "tpsso_spa_client",
                    // SPA 是 public client，不需要 client_secret（使用 PKCE 确保安全）
                    ConsentType = ConsentTypes.Implicit,
                    DisplayName = "TPSSO SPA Client",
                    RedirectUris = { new Uri("http://localhost:3007/callback") },
                    PostLogoutRedirectUris = { new Uri("http://localhost:3007") },
                    Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.ResponseTypes.Code,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile
                }
                });


                string testEmail = "admin@test.com";
                string testPassword = "AdminPassword123!";

                // 检查用户是否存在，如果不存在则创建
                if (await userManager.FindByEmailAsync(testEmail) == null)
                {
                    var user = new IdentityUser { UserName = testEmail, Email = testEmail };
                    var result = await userManager.CreateAsync(user, testPassword);
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
}
