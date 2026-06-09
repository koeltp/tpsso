namespace TPSSO.Domain.Entities;

/// <summary>
/// 客户端允许的授权范围
/// </summary>
public class ClientScope
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>关联客户端</summary>
    public Guid ClientApplicationId { get; set; }

    /// <summary>Scope 名称（如 openid、profile、email）</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ClientApplication? ClientApplication { get; set; }
}
