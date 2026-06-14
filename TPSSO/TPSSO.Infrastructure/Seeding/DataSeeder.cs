using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TPSSO.Infrastructure.Data;

namespace TPSSO.Infrastructure.Seeding;

/// <summary>
/// 种子数据统一入口，协调各独立 Seeder 的执行顺序
/// </summary>
public class DataSeeder(
    ApplicationDbContext context,
    ScopeSeeder scopeSeeder,
    RoleSeeder roleSeeder,
    UserSeeder userSeeder,
    ClientSeeder clientSeeder,
    DictSeeder dictSeeder,
    IServiceProvider serviceProvider,
    ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(bool isDevelopment)
    {
        logger.LogInformation("===== 开始执行种子数据初始化 =====");

        // 数据库迁移
        logger.LogInformation("执行数据库迁移...");
        await context.Database.MigrateAsync();
        logger.LogInformation("数据库迁移完成");

        // 按依赖顺序执行：Scope → 角色 → 用户 → 客户端 → 字典
        await scopeSeeder.SeedAsync();
        await roleSeeder.SeedAsync();
        await userSeeder.SeedAsync();
        await clientSeeder.SeedAsync();
        await dictSeeder.SeedAsync();

        // 开发环境额外注册测试客户端
        if (isDevelopment)
        {
            var testSeeder = serviceProvider.GetRequiredService<TestClientSeeder>();
            await testSeeder.SeedAsync();
        }

        logger.LogInformation("===== 种子数据初始化完成 =====");
    }
}
