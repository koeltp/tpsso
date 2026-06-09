namespace TPSSO.Application.Options;

/// <summary>
/// JWT 签发配置
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>签名密钥（至少 32 字符）</summary>
    public string SecretKey { get; set; } = default!;

    /// <summary>签发者</summary>
    public string Issuer { get; set; } = "TPSSO";

    /// <summary>受众</summary>
    public string Audience { get; set; } = "TPSSO";

    /// <summary>Access Token 有效时长（分钟）</summary>
    public int ExpireMinutes { get; set; } = 30;

    /// <summary>Refresh Token 有效时长（天）</summary>
    public int RefreshExpireDays { get; set; } = 7;
}
