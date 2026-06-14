namespace TPSSO.Application.Models;

/// <summary>
/// 两步验证绑定结果（生成密钥和二维码信息）
/// </summary>
public class TwoFactorSetupResult
{
    /// <summary>共享密钥（Base32 编码，供手动输入）</summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>二维码 URI（otpauth://totp/...）</summary>
    public string AuthenticatorUri { get; set; } = string.Empty;

    /// <summary>恢复码列表（明文，仅此一次展示）</summary>
    public List<string> RecoveryCodes { get; set; } = [];
}

/// <summary>
/// 两步验证验证请求
/// </summary>
public class TwoFactorVerifyModel
{
    /// <summary>TOTP 验证码（6位数字）</summary>
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 两步验证登录请求（密码验证通过后，需要2FA时使用）
/// </summary>
public class LoginTwoFactorModel
{
    /// <summary>用户ID（密码验证通过后返回）</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>TOTP 验证码 或 恢复码</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>是否为恢复码</summary>
    public bool IsRecoveryCode { get; set; }

    /// <summary>记住我</summary>
    public bool RememberMe { get; set; }
}

/// <summary>
/// 登录结果（可能需要2FA验证）
/// </summary>
public class LoginResult
{
    /// <summary>用户信息（登录成功时返回）</summary>
    public UserInfoResult? UserInfo { get; set; }

    /// <summary>是否需要两步验证</summary>
    public bool RequiresTwoFactor { get; set; }

    /// <summary>需要2FA时的用户ID</summary>
    public string? UserId { get; set; }
}
