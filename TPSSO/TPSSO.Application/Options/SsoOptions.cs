namespace TPSSO.Application.Options;

/// <summary>
/// SSO 相关配置
/// </summary>
public class SsoOptions
{
    public const string SectionName = "SsoOptions";

    /// <summary>统一登录页基础地址（前端地址）</summary>
    public string LoginBaseUrl { get; set; } = "http://localhost:3010";

    /// <summary>
    /// OpenIddict Issuer（Auth 服务自身地址）
    /// 不设置则由 OpenIddict 自动从请求中推断
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>自定义登录页路径</summary>
    public string LoginPath { get; set; } = "/login";

    /// <summary>授权确认页面路径</summary>
    public string ConsentPath { get; set; } = "/authorize";
}
