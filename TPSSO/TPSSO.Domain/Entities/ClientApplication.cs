namespace TPSSO.Domain.Entities;

/// <summary>
/// 客户端应用实体，存储业务信息（审核、创建人等）
/// 协议信息（ClientId、Secret、RedirectUris）审核通过后同步到 OpenIddict Applications 表
/// </summary>
public class ClientApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>关联 OpenIddict Applications 表的 Id（审核通过后写入）</summary>
    public string? OpenIddictApplicationId { get; set; }

    /// <summary>客户端标识</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>客户端密钥（仅机密客户端，BCrypt 哈希）</summary>
    public string? ClientSecretHash { get; set; }

    /// <summary>应用名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>应用描述</summary>
    public string? Description { get; set; }

    /// <summary>应用 Logo 地址</summary>
    public string? Logo { get; set; }

    /// <summary>是否公开客户端（SPA/移动端），公开客户端不需要 Secret</summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 授权确认类型：explicit=每次需用户确认，implicit=自动确认
    /// </summary>
    public string ConsentType { get; set; } = "explicit";

    /// <summary>审核状态</summary>
    public ClientStatus Status { get; set; } = ClientStatus.Draft;

    /// <summary>创建人</summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>审核人</summary>
    public Guid? ReviewedByUserId { get; set; }

    /// <summary>审核时间</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>审核备注/拒绝原因</summary>
    public string? ReviewRemark { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>更新时间</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>乐观并发控制：每次更新自动递增</summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>回调地址集合</summary>
    public ICollection<ClientRedirectUri> RedirectUris { get; set; } = new List<ClientRedirectUri>();

    /// <summary>允许的授权范围集合</summary>
    public ICollection<ClientScope> AllowedScopes { get; set; } = new List<ClientScope>();

    /// <summary>允许的授权类型集合</summary>
    public ICollection<ClientGrantType> GrantTypes { get; set; } = new List<ClientGrantType>();

    // ──────── 业务方法 ────────

    /// <summary>
    /// 判断客户端是否处于激活状态
    /// </summary>
    public bool IsActive() => Status == ClientStatus.Approved;

    /// <summary>
    /// 验证回调地址是否在允许列表中
    /// </summary>
    public bool ValidateRedirectUri(string? redirectUri)
    {
        if (string.IsNullOrEmpty(redirectUri)) return false;
        return RedirectUris.Any(u => string.Equals(u.Uri, redirectUri, StringComparison.Ordinal));
    }
}
