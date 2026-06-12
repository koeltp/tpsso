using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TPSSO.Application.Interfaces;

namespace TPSSO.Infrastructure.Services;

/// <summary>
/// 基于 Redis 的验证码服务
/// </summary>
public class VerificationCodeService : IVerificationCodeService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<VerificationCodeService> _logger;

    // Redis key 前缀
    private const string CodePrefix = "verify:code:";
    private const string RatePrefix = "verify:rate:";

    public VerificationCodeService(IDistributedCache cache, ILogger<VerificationCodeService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string email, int expireMinutes = 5)
    {
        // 生成6位数字验证码
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();

        // 存入 Redis，设置过期时间
        await _cache.SetStringAsync(
            CodePrefix + email,
            code,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expireMinutes) });

        // 设置发送频率限制（60秒）
        await _cache.SetStringAsync(
            RatePrefix + email,
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) });

        _logger.LogInformation("验证码已生成：{Email}", email);
        return code;
    }

    public async Task<bool> VerifyAsync(string email, string code)
    {
        var key = CodePrefix + email;
        var storedCode = await _cache.GetStringAsync(key);

        if (storedCode == null || storedCode != code)
            return false;

        // 验证成功后删除验证码，防止重复使用
        await _cache.RemoveAsync(key);
        return true;
    }

    public async Task<bool> CanSendAsync(string email)
    {
        var rateKey = RatePrefix + email;
        var exists = await _cache.GetStringAsync(rateKey);
        return string.IsNullOrEmpty(exists);
    }
}
