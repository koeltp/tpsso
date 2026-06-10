namespace TPSSO.Application.Models;

/// <summary>
/// 字典类型 DTO
/// </summary>
public class DictTypeDto
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>父分类 ID，null 表示顶级分类</summary>
    public Guid? ParentId { get; set; }
}

/// <summary>
/// 字典类型结果（树形）
/// </summary>
public class DictTypeResult
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
    public Guid? ParentId { get; set; }

    /// <summary>子分类列表</summary>
    public List<DictTypeResult> Children { get; set; } = [];

    /// <summary>配置项列表（叶子分类才有）</summary>
    public List<DictItemResult> Items { get; set; } = [];
}

/// <summary>
/// 字典项 DTO
/// </summary>
public class DictItemDto
{
    public Guid? Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSensitive { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 字典项结果
/// </summary>
public class DictItemResult
{
    public Guid Id { get; set; }
    public Guid TypeId { get; set; }
    public string Key { get; set; } = string.Empty;

    /// <summary>敏感配置返回 ***，非敏感返回明文</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>是否为敏感配置</summary>
    public bool IsSensitive { get; set; }

    public string? Description { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
}
