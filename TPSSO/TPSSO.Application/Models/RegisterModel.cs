using System.ComponentModel.DataAnnotations;

namespace TPSSO.Application.Models;

/// <summary>
/// 发送验证码请求
/// </summary>
public class SendCodeModel
{
    /// <summary>邮箱地址</summary>
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = "";
}

/// <summary>
/// 注册请求
/// </summary>
public class RegisterModel
{
    /// <summary>邮箱地址</summary>
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = "";

    /// <summary>验证码</summary>
    [Required(ErrorMessage = "验证码不能为空")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "验证码为6位数字")]
    public string Code { get; set; } = "";

    /// <summary>密码</summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码至少6位")]
    public string Password { get; set; } = "";

    /// <summary>确认密码</summary>
    [Required(ErrorMessage = "确认密码不能为空")]
    [Compare("Password", ErrorMessage = "两次密码不一致")]
    public string ConfirmPassword { get; set; } = "";
}

/// <summary>
/// 重置密码请求（忘记密码）
/// </summary>
public class ResetPasswordModel
{
    /// <summary>邮箱地址</summary>
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = "";

    /// <summary>验证码</summary>
    [Required(ErrorMessage = "验证码不能为空")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "验证码为6位数字")]
    public string Code { get; set; } = "";

    /// <summary>新密码</summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码至少6位")]
    public string NewPassword { get; set; } = "";

    /// <summary>确认新密码</summary>
    [Required(ErrorMessage = "确认密码不能为空")]
    [Compare("NewPassword", ErrorMessage = "两次密码不一致")]
    public string ConfirmNewPassword { get; set; } = "";
}
