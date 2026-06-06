namespace TPSSO.Api.Options;

/// <summary>
/// SMTP 邮件发送配置
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    /// <summary>SMTP 服务器地址</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP 端口</summary>
    public int Port { get; set; } = 465;

    /// <summary>SMTP 用户名</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP 密码</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>发件人显示名称</summary>
    public string FromName { get; set; } = "TPSSO";

    /// <summary>发件人邮箱地址</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>是否使用 SSL</summary>
    public bool UseSsl { get; set; } = true;
}
