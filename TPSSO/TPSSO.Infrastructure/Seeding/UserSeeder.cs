using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TPSSO.Domain.Entities;

namespace TPSSO.Infrastructure.Seeding;

/// <summary>
/// 用户种子数据初始化
/// </summary>
public class UserSeeder(UserManager<User> userManager, ILogger<UserSeeder> logger)
{
    private static readonly (string Email, string Password, string NickName, string Role)[] Users =
    [
        ("admin@taipi.top", "Admin@123", "管理员", RoleConstants.Admin),
        ("tp@taipi.top", "Admin@123", "测试用户", RoleConstants.User),
    ];

    public async Task SeedAsync()
    {
        foreach (var (email, password, nickName, role) in Users)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new User { UserName = email, Email = email, NickName = nickName };
                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    logger.LogError("创建用户 '{Email}' 失败：{Errors}", email, string.Join(", ", result.Errors));
                    continue;
                }
                logger.LogInformation("用户 '{Email}' 已创建", email);
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
