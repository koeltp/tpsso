using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TPSSO.Infrastructure.Seeding;

/// <summary>
/// 客户端种子配置，集中定义所有属性避免 Create/Update 重复
/// ClientSeeder 和 TestClientSeeder 共用此类型
/// </summary>
public class ClientSeedConfig
{
    public string ClientId { get; init; } = string.Empty;
    public string? ClientSecret { get; init; }
    public string Type { get; init; } = ClientTypes.Confidential;
    public string ConsentType { get; init; } = ConsentTypes.Implicit;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsPublic { get; init; }
    public string[] RedirectUris { get; set; } = [];
    public string[] PostLogoutRedirectUris { get; set; } = [];
    public string[] Permissions { get; init; } = [];
    public string[] AllowedScopes { get; init; } = [];
    public string[] GrantTypes { get; init; } = [];
}
