using System.Security.Cryptography;
using System.Text;

namespace TPSSO.Infrastructure.Utils;

/// <summary>
/// AES 加密解密工具，用于敏感配置加密存储
/// </summary>
public static class AesEncryption
{
    // 密钥从环境变量读取，生产环境必须设置
    private static readonly byte[] Key = Encoding.UTF8.GetBytes(
        Environment.GetEnvironmentVariable("TPSSO__AES_KEY") ?? "TPSSO2026!@#$%^&" // 16 字节（AES-128）
    );

    private static readonly byte[] IV = Encoding.UTF8.GetBytes(
        Environment.GetEnvironmentVariable("TPSSO__AES_IV") ?? "TPSSO_AES_IV_16!" // 16 字节
    );

    /// <summary>
    /// AES 加密
    /// </summary>
    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }

    /// <summary>
    /// AES 解密
    /// </summary>
    public static string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
