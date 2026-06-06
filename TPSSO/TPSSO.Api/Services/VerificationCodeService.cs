using Microsoft.EntityFrameworkCore;
using TPSSO.Api.Models;

namespace TPSSO.Api.Services;

/// <summary>
/// 验证码生成、校验、清理服务
/// </summary>
public class VerificationCodeService
{
    private readonly ApplicationDbContext _dbContext;
    private const int CodeLength = 6;
    private const int ExpirationMinutes = 10;

    public VerificationCodeService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 生成验证码并存储。如果有效期内已有未使用的验证码，直接返回已有码（防刷）。
    /// </summary>
    public async Task<string> GenerateAsync(string email, int purpose)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // 检查有效期内是否已有未使用的验证码（防刷）
        var existing = await _dbContext.VerificationCodes
            .Where(v => v.Email == normalizedEmail
                     && v.Purpose == purpose
                     && !v.IsUsed
                     && v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            return existing.Code;
        }

        // 生成新验证码
        var code = GenerateCode();
        var now = DateTime.UtcNow;

        var record = new VerificationCode
        {
            Email = normalizedEmail,
            Code = code,
            Purpose = purpose,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(ExpirationMinutes),
            IsUsed = false
        };

        _dbContext.VerificationCodes.Add(record);
        await _dbContext.SaveChangesAsync();

        return code;
    }

    /// <summary>
    /// 校验验证码：码正确 + 未过期 + 未使用 + 邮箱匹配 + 用途匹配
    /// 校验通过后标记为已使用。
    /// </summary>
    public async Task<bool> VerifyAsync(string email, string code, int purpose)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var record = await _dbContext.VerificationCodes
            .Where(v => v.Email == normalizedEmail
                     && v.Code == code
                     && v.Purpose == purpose
                     && !v.IsUsed
                     && v.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (record == null)
        {
            return false;
        }

        record.IsUsed = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 清理过期记录（可由定时任务调用）
    /// </summary>
    public async Task CleanupExpiredAsync()
    {
        var expired = await _dbContext.VerificationCodes
            .Where(v => v.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expired.Count > 0)
        {
            _dbContext.VerificationCodes.RemoveRange(expired);
            await _dbContext.SaveChangesAsync();
        }
    }

    private static string GenerateCode()
    {
        return Random.Shared.Next(0, 1_000_000).ToString().PadLeft(CodeLength, '0');
    }
}
