namespace TPSSO.Api;

/// <summary>
/// SSO 相关的前端地址配置
/// </summary>
public class SsoOptions
{
    public const string SectionName = "SsoOptions";

    /// <summary>统一登录页基础地址</summary>
    public string LoginBaseUrl { get; set; } = "http://localhost:3008";

    /// <summary>自定义登录页路径</summary>
    public string LoginPath { get; set; } = "/login";
}