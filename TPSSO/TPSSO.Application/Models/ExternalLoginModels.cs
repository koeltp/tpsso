namespace TPSSO.Application.Models;

/// <summary>
/// 已绑定的第三方账号信息
/// </summary>
public class ExternalLoginInfo
{
    /// <summary>登录提供者（GitHub / Google / WeChat）</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>提供者中的用户Key</summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>显示名称</summary>
    public string? DisplayName { get; set; }

    /// <summary>是否已绑定</summary>
    public bool IsBound => !string.IsNullOrEmpty(ProviderKey);
}

/// <summary>
/// 第三方登录提供者（含绑定状态）
/// </summary>
public class ExternalLoginProvider
{
    /// <summary>提供者标识（GitHub / Google / WeChat）</summary>
    public string Scheme { get; set; } = string.Empty;

    /// <summary>显示名称</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>是否已绑定</summary>
    public bool IsBound { get; set; }

    /// <summary>绑定的第三方用户名</summary>
    public string? BoundDisplayName { get; set; }
}
