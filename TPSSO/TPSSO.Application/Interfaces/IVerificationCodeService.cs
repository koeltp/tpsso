namespace TPSSO.Application.Interfaces;

/// <summary>
/// 验证码服务接口（基于 Redis）
/// </summary>
public interface IVerificationCodeService
{
    /// <summary>
    /// 生成6位数字验证码并存入 Redis
    /// </summary>
    Task<string> GenerateAsync(string email, int expireMinutes = 5);

    /// <summary>
    /// 验证验证码是否正确，验证成功后删除
    /// </summary>
    Task<bool> VerifyAsync(string email, string code);

    /// <summary>
    /// 检查发送频率限制（同一邮箱60秒内只能发一次）
    /// </summary>
    Task<bool> CanSendAsync(string email);
}
