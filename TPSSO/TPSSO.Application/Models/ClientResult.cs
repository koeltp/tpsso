namespace TPSSO.Application.Models;

/// <summary>
/// 客户端信息返回结果
/// </summary>
public class ClientResult
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Logo { get; set; }
    public string RedirectUris { get; set; } = string.Empty;
    public string AllowedScopes { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewRemark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>乐观并发令牌，更新时必须传回</summary>
    public string? RowVersion { get; set; }
}

/// <summary>
/// 创建客户端成功后的返回结果（包含一次性 Secret）
/// </summary>
public class ClientCreatedResult : ClientResult
{
    /// <summary>明文 Secret，仅创建时返回一次</summary>
    public string? PlainSecret { get; set; }
}
