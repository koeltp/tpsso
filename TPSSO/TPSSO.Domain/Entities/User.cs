using Microsoft.AspNetCore.Identity;

namespace TPSSO.Domain.Entities;

/// <summary>
/// 自定义用户实体，扩展 IdentityUser
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>头像地址</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>昵称</summary>
    public string? NickName { get; set; }

    /// <summary>注册时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
