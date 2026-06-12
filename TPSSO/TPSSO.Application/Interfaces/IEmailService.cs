namespace TPSSO.Application.Interfaces;

/// <summary>
/// 邮件发送服务接口
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// 发送验证码邮件
    /// </summary>
    Task SendVerificationCodeAsync(string toEmail, string code, int expireMinutes);
}
