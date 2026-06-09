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

/// <summary>
/// 登录成功返回结果
/// </summary>
public class LoginResult
{
    /// <summary>Access Token</summary>
    public string Token { get; set; } = default!;

    /// <summary>Refresh Token</summary>
    public string RefreshToken { get; set; } = default!;

    /// <summary>Token 过期时间（ISO 8601）</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>用户信息</summary>
    public UserInfoResult UserInfo { get; set; } = default!;
}

/// <summary>
/// 刷新 Token 请求
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = default!;
}
