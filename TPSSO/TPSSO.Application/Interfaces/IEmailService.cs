namespace TPSSO.Application.Interfaces;

/// <summary>
/// 邮件发送服务接口
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// 发送 HTML 格式邮件
    /// </summary>
    Task SendHtmlEmailAsync(string toEmail, string subject, string htmlBody);

    /// <summary>
    /// 发送验证码邮件
    /// </summary>
    Task SendVerificationCodeAsync(string toEmail, string code, int expirationMinutes);
}
