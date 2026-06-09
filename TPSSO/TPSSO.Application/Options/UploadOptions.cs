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
}
