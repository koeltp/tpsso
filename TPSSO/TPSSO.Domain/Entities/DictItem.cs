namespace TPSSO.Domain.Entities;

/// <summary>
/// 字典项，存储具体配置的 Key-Value
/// </summary>
public class DictItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>所属类型 ID</summary>
    public Guid TypeId { get; set; }

    /// <summary>配置键</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>配置值（敏感配置为加密后的密文）</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>配置描述</summary>
    public string? Description { get; set; }

    /// <summary>是否为敏感配置（如密钥、密码），敏感配置加密存储</summary>
    public bool IsSensitive { get; set; }

    /// <summary>排序</summary>
    public int Sort { get; set; }

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>所属类型</summary>
    public DictType? Type { get; set; }
}
