using OpenIddict.Abstractions;
using Microsoft.Extensions.Logging;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Infrastructure.Seeding;

/// <summary>
/// Scope 种子数据初始化
/// </summary>
public class ScopeSeeder(IOpenIddictScopeManager scopeManager, ILogger<ScopeSeeder> logger)
{
    private static readonly (string Name, string DisplayName, string Resource)[] Scopes =
    [
        ("profile", "Profile information", "tpsso-api"),
        ("email", "Email address", "tpsso-api"),
        ("roles", "User roles", "tpsso-api"),
    ];

    public async Task SeedAsync()
    {
        foreach (var (name, displayName, resource) in Scopes)
        {
            if (await scopeManager.FindByNameAsync(name) is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = name,
                    DisplayName = displayName,
                    Resources = { resource }
                });
                logger.LogInformation("Scope '{Name}' 已创建", name);
            }
        }
    }
}
