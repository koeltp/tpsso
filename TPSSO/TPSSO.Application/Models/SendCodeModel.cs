using System.ComponentModel.DataAnnotations;

namespace TPSSO.Application.Models;

/// <summary>
/// 发送验证码请求
/// </summary>
public class SendCodeModel
{
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string Email { get; set; } = string.Empty;

    /// <summary>用途：0=注册, 1=重置密码</summary>
    [Required]
    public int Purpose { get; set; }
}
