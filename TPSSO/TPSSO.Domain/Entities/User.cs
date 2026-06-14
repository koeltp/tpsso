using Microsoft.AspNetCore.Identity;

namespace TPSSO.Domain.Entities;

/// <summary>
/// 自定义用户实体，扩展 IdentityUser
/// TwoFactorEnabled 继承自 IdentityUser，无需重复声明
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>头像地址</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>昵称</summary>
    public string? NickName { get; set; }

    /// <summary>TOTP 密钥（Base64 编码）</summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>恢复码（JSON 数组，BCrypt 哈希存储）</summary>
    public string? RecoveryCodes { get; set; }

    /// <summary>注册时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
