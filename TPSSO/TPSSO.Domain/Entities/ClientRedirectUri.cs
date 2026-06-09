namespace TPSSO.Domain.Entities;

/// <summary>
/// 客户端允许的回调地址
/// </summary>
public class ClientRedirectUri
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>关联客户端</summary>
    public Guid ClientApplicationId { get; set; }

    /// <summary>回调地址</summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ClientApplication? ClientApplication { get; set; }
}
