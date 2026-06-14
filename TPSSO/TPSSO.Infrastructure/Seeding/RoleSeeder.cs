using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TPSSO.Domain.Entities;

namespace TPSSO.Infrastructure.Seeding;

/// <summary>
/// 角色种子数据初始化
/// </summary>
public class RoleSeeder(RoleManager<Role> roleManager, ILogger<RoleSeeder> logger)
{
    private static readonly (string Name, string Description)[] Roles =
    [
        (RoleConstants.Admin, "系统管理员"),
        (RoleConstants.User, "普通用户"),
    ];

    public async Task SeedAsync()
    {
        foreach (var (name, description) in Roles)
        {
            if (await roleManager.FindByNameAsync(name) is null)
            {
                await roleManager.CreateAsync(new Role { Name = name, Description = description });
                logger.LogInformation("角色 '{Name}' 已创建", name);
            }
        }
    }
}
