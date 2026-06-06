using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TPSSO.Api.Options;

namespace TPSSO.Api.Services;

/// <summary>
/// 邮件发送服务
/// </summary>
public class EmailService
{
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpOptions> smtpOptions, ILogger<EmailService> logger)
    {
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// 发送 HTML 格式邮件
    /// </summary>
    public async Task SendHtmlEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpOptions.FromName, _smtpOptions.FromEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var secureOptions = _smtpOptions.Port switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                _ => _smtpOptions.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None
            };

            await client.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, secureOptions);
            await client.AuthenticateAsync(_smtpOptions.Username, _smtpOptions.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("邮件发送成功: {ToEmail}, 主题: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "邮件发送失败: {ToEmail}", toEmail);
            throw;
        }
    }

    /// <summary>
    /// 发送验证码邮件
    /// </summary>
    public async Task SendVerificationCodeAsync(string toEmail, string code, int expirationMinutes)
    {
        var subject = "TPSSO 验证码";
        var htmlBody = $"""
            <div style="max-width: 480px; margin: 0 auto; padding: 40px 20px; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;">
                <div style="background: white; border-radius: 16px; padding: 40px; box-shadow: 0 4px 24px rgba(0,0,0,0.08);">
                    <h2 style="margin: 0 0 24px; color: #1a1a2e; font-size: 24px; font-weight: 600;">TPSSO 验证码</h2>
                    <p style="margin: 0 0 16px; color: #666; font-size: 15px; line-height: 1.6;">
                        你正在进行账户操作，验证码为：
                    </p>
                    <div style="background: linear-gradient(135deg, #f0f5ff 0%, #e8f0fe 100%); border-radius: 12px; padding: 16px 20px; text-align: center; margin: 24px 0;">
                        <span style="font-size: 32px; font-weight: 700; letter-spacing: 6px; color: #7c3aed; font-family: ui-monospace, Consolas, monospace; white-space: nowrap;">{code}</span>
                    </div>
                    <p style="margin: 0; color: #999; font-size: 13px;">
                        验证码有效期为 {expirationMinutes} 分钟，请勿泄露给他人。
                    </p>
                </div>
                <p style="margin-top: 24px; text-align: center; color: #bbb; font-size: 12px;">
                    此邮件由 TPSSO 系统自动发送，请勿回复。
                </p>
            </div>
            """;

        await SendHtmlEmailAsync(toEmail, subject, htmlBody);
    }
}
