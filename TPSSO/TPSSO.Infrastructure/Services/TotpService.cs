using System.Security.Cryptography;
using System.Text.Json;
using BCrypt.Net;
using OtpNet;
using TPSSO.Application.Interfaces;

namespace TPSSO.Infrastructure.Services;

/// <summary>
/// TOTP 两步验证服务实现
/// </summary>
public class TotpService : ITotpService
{
    public byte[] GenerateSecret()
    {
        // 生成 20 字节（160 位）的密钥，符合 RFC 6238 推荐
        var key = KeyGeneration.GenerateRandomKey(20);
        return key;
    }

    public string GenerateAuthenticatorUri(string secretBase64, string issuer, string accountName)
    {
        var secretBytes = Convert.FromBase64String(secretBase64);
        var base32Secret = Base32Encoding.ToString(secretBytes).TrimEnd('=');

        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={base32Secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
    }

    public bool VerifyCode(string secretBase64, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            return false;

        var secretBytes = Convert.FromBase64String(secretBase64);
        var totp = new Totp(secretBytes, step: 30, totpSize: 6, mode: OtpHashMode.Sha1);

        // 允许前后各1个时间窗口的偏差（共3个窗口，±30秒）
        return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
    }

    public List<string> GenerateRecoveryCodes(int count = 10)
    {
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            // 生成 8 位字母数字混合恢复码，格式：XXXX-XXXX
            var bytes = RandomNumberGenerator.GetBytes(5);
            var code = Convert.ToHexString(bytes);
            codes.Add($"{code[..4]}-{code[4..8]}");
        }
        return codes;
    }

    public bool ValidateRecoveryCode(ref string recoveryCodesJson, string code)
    {
        if (string.IsNullOrWhiteSpace(recoveryCodesJson) || string.IsNullOrWhiteSpace(code))
            return false;

        var hashedCodes = JsonSerializer.Deserialize<List<string>>(recoveryCodesJson);
        if (hashedCodes == null || hashedCodes.Count == 0)
            return false;

        // 逐个验证，找到匹配的恢复码
        for (var i = 0; i < hashedCodes.Count; i++)
        {
            if (BCrypt.Net.BCrypt.Verify(code, hashedCodes[i]))
            {
                // 使用后移除该恢复码
                hashedCodes.RemoveAt(i);
                recoveryCodesJson = JsonSerializer.Serialize(hashedCodes);
                return true;
            }
        }

        return false;
    }
}
