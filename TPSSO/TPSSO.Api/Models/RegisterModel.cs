using System.ComponentModel.DataAnnotations;

namespace TPSSO.Api.Models;

/// <summary>
/// 用户注册请求
/// </summary>
public class RegisterModel
{
    [Required(ErrorMessage = "用户名不能为空")]
    [MinLength(2, ErrorMessage = "用户名长度不能少于2位")]
    [MaxLength(20, ErrorMessage = "用户名长度不能超过20位")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "验证码不能为空")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "验证码为6位")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [MinLength(6, ErrorMessage = "密码长度不能少于6位")]
    public string Password { get; set; } = string.Empty;
}
