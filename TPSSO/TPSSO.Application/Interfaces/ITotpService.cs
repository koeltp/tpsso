namespace TPSSO.Application.Interfaces;

/// <summary>
/// TOTP 两步验证服务接口
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// 生成新的 TOTP 密钥
    /// </summary>
    byte[] GenerateSecret();

    /// <summary>
    /// 生成 otpauth:// URI（用于二维码）
    /// </summary>
    string GenerateAuthenticatorUri(string secretBase64, string issuer, string accountName);

    /// <summary>
    /// 验证 TOTP 验证码
    /// </summary>
    bool VerifyCode(string secretBase64, string code);

    /// <summary>
    /// 生成恢复码列表
    /// </summary>
    List<string> GenerateRecoveryCodes(int count = 10);

    /// <summary>
    /// 验证恢复码（验证通过后从列表中移除）
    /// </summary>
    bool ValidateRecoveryCode(ref string recoveryCodesJson, string code);
}
