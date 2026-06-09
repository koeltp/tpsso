using Microsoft.AspNetCore.Identity;

namespace TPSSO.Domain.Entities;

/// <summary>
/// 自定义角色实体，扩展 IdentityRole
/// </summary>
public class Role : IdentityRole<Guid>
{
    /// <summary>角色描述</summary>
    public string? Description { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
