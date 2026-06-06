namespace TPSSO.Application.Interfaces;

/// <summary>
/// 验证码生成与校验服务接口
/// </summary>
public interface IVerificationCodeService
{
    /// <summary>
    /// 生成验证码并存储
    /// </summary>
    Task<string> GenerateAsync(string email, int purpose);

    /// <summary>
    /// 校验验证码，通过后标记为已使用
    /// </summary>
    Task<bool> VerifyAsync(string email, string code, int purpose);

    /// <summary>
    /// 清理过期记录
    /// </summary>
    Task CleanupExpiredAsync();
}
