namespace TPSSO.Domain.Entities;

/// <summary>
/// 客户端允许的授权类型（如 authorization_code、client_credentials、device_code）
/// </summary>
public class ClientGrantType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>关联客户端</summary>
    public Guid ClientApplicationId { get; set; }

    /// <summary>授权类型（如 authorization_code、refresh_token、client_credentials、device_code）</summary>
    public string GrantType { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ClientApplication? ClientApplication { get; set; }
}
