namespace TPSSO.Application.Options;

/// <summary>
/// 文件上传配置
/// </summary>
public class UploadOptions
{
    public const string SectionName = "Upload";

    /// <summary>头像允许的 MIME 类型（逗号分隔）</summary>
    public string AvatarAllowedTypes { get; set; } = "image/jpeg,image/png,image/gif,image/webp";

    /// <summary>头像最大文件大小（MB）</summary>
    public int AvatarMaxSizeMB { get; set; } = 2;

    /// <summary>客户端 Logo 允许的 MIME 类型（逗号分隔）</summary>
    public string LogoAllowedTypes { get; set; } = "image/jpeg,image/png,image/gif,image/webp,image/svg+xml";

    /// <summary>客户端 Logo 最大文件大小（MB）</summary>
    public int LogoMaxSizeMB { get; set; } = 2;
}
