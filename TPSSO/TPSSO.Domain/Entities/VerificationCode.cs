namespace TPSSO.Domain.Entities;

/// <summary>
/// 验证码实体
/// </summary>
public class VerificationCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>接收邮箱</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>验证码（6位数字）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>用途：0=注册, 1=重置密码</summary>
    public int Purpose { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>过期时间</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>是否已使用</summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// 判断是否已过期
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// 判断是否仍然有效（未使用且未过期）
    /// </summary>
    public bool IsValid() => !IsUsed && !IsExpired();

    /// <summary>
    /// 标记为已使用
    /// </summary>
    public void MarkAsUsed() => IsUsed = true;
}
