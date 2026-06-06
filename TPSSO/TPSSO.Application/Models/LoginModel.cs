namespace TPSSO.Application.Models;

/// <summary>
/// 用户登录请求
/// </summary>
public class LoginModel
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public bool RememberMe { get; set; } = false;
}
