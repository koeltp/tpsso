using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using TPSSO.Application.Interfaces;

namespace TPSSO.Infrastructure.Services;

/// <summary>
/// SMTP 邮件发送服务，配置从数据库（ConfigService）读取
/// typeCode = "SmtpServer"，key 分别为 Host/Port/UseSsl/Username/Password/SenderName/SenderEmail
/// 敏感字段（Password）在数据库中加密存储
/// 端口支持：465（隐式 SSL）、587（STARTTLS）、25（无加密）
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfigService _configService;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfigService configService, ILogger<EmailService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(string toEmail, string code, int expireMinutes)
    {
        var host = await _configService.GetStringAsync("SmtpServer", "Host");
        var port = await _configService.GetIntAsync("SmtpServer", "Port", 465);
        var useSsl = await _configService.GetBoolAsync("SmtpServer", "UseSsl", true);
        var username = await _configService.GetStringAsync("SmtpServer", "Username");
        var password = await _configService.GetStringAsync("SmtpServer", "Password");
        var senderName = await _configService.GetStringAsync("SmtpServer", "SenderName", "TPSSO");
        var senderEmail = await _configService.GetStringAsync("SmtpServer", "SenderEmail");

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(senderEmail))
        {
            _logger.LogWarning("SMTP 未配置，验证码将输出到控制台：{Email} -> {Code}", toEmail, code);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = "TPSSO 验证码";

        var body = $@"
            <div style='padding:20px;font-family:sans-serif;'>
                <h2 style='color:#333;'>TPSSO 统一身份认证</h2>
                <p>您的验证码为：</p>
                <p style='font-size:28px;font-weight:bold;color:#409eff;letter-spacing:4px;'>{code}</p>
                <p style='color:#999;'>验证码 {expireMinutes} 分钟内有效，请勿泄露给他人。</p>
            </div>";

        message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();

            // 根据端口和配置决定连接方式
            // 465: 隐式 SSL（直接 TLS）
            // 587: STARTTLS（先明文再升级）
            // 25:  无加密
            SecureSocketOptions socketOptions;
            if (port == 465)
            {
                socketOptions = SecureSocketOptions.SslOnConnect;
            }
            else if (port == 587)
            {
                socketOptions = SecureSocketOptions.StartTls;
            }
            else if (useSsl)
            {
                socketOptions = SecureSocketOptions.SslOnConnect;
            }
            else
            {
                socketOptions = SecureSocketOptions.Auto;
            }

            await client.ConnectAsync(host, port, socketOptions);
            if (!string.IsNullOrEmpty(username))
            {
                await client.AuthenticateAsync(username, password);
            }
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation("验证码邮件已发送至 {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送验证码邮件失败：{Email}  {UserName}", toEmail, username);
            throw;
        }
    }
}
