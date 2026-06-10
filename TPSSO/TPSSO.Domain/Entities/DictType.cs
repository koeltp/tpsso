namespace TPSSO.Domain.Entities;

/// <summary>
/// 字典类型，用于配置分组（如 OAuth、JWT、系统等），支持树形层级
/// </summary>
public class DictType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>类型编码，唯一标识（如 OAuth、GitHub、JWT）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>类型名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>描述</summary>
    public string? Description { get; set; }

    /// <summary>排序</summary>
    public int Sort { get; set; }

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>父分类 ID，null 表示顶级分类</summary>
    public Guid? ParentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>父分类</summary>
    public DictType? Parent { get; set; }

    /// <summary>子分类列表</summary>
    public List<DictType> Children { get; set; } = [];

    /// <summary>字典项列表（只有叶子分类才有配置项）</summary>
    public List<DictItem> Items { get; set; } = [];
}
