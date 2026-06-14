using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using TPSSO.Infrastructure.Utils;

namespace TPSSO.Infrastructure.Seeding;

/// <summary>
/// 字典配置种子数据初始化
/// </summary>
public class DictSeeder(ApplicationDbContext context, ILogger<DictSeeder> logger)
{
    public async Task SeedAsync()
    {
        if (await context.DictTypes.AnyAsync()) return;

        logger.LogInformation("开始写入字典配置种子数据...");

        // 父分类
        var oauthType = new DictType { Code = "OAuth", Name = "第三方登录", Description = "第三方 OAuth 登录配置", Sort = 1 };
        var securityType = new DictType { Code = "Security", Name = "安全配置", Description = "JWT、密码策略等安全相关配置", Sort = 2 };
        var systemType = new DictType { Code = "System", Name = "系统配置", Description = "系统全局配置", Sort = 3 };
        var smtpType = new DictType { Code = "Smtp", Name = "邮件配置", Description = "SMTP 邮件发送配置", Sort = 4 };

        context.DictTypes.AddRange(oauthType, securityType, systemType, smtpType);
        await context.SaveChangesAsync();

        // 子分类和配置项
        var types = new List<DictType>
        {
            new()
            {
                Code = "GitHub", Name = "GitHub", Description = "GitHub OAuth 登录", Sort = 1,
                ParentId = oauthType.Id,
                Items =
                [
                    new DictItem { Key = "ClientId", Value = "Ov23lietsdCwxFEgi6Ky", Description = "GitHub OAuth Client ID", Sort = 1 },
                    new DictItem { Key = "ClientSecret", Value = "xxx", Description = "GitHub OAuth Client Secret", IsSensitive = true, Sort = 2 },
                    new DictItem { Key = "IsEnabled", Value = "true", Description = "是否启用 GitHub 登录", Sort = 3 },
                ]
            },
            new()
            {
                Code = "Google", Name = "Google", Description = "Google OAuth 登录", Sort = 2,
                ParentId = oauthType.Id,
                Items =
                [
                    new DictItem { Key = "ClientId", Value = "", Description = "Google OAuth Client ID", Sort = 1 },
                    new DictItem { Key = "ClientSecret", Value = "", Description = "Google OAuth Client Secret", IsSensitive = true, Sort = 2 },
                    new DictItem { Key = "IsEnabled", Value = "false", Description = "是否启用 Google 登录", Sort = 3 },
                ]
            },
            new()
            {
                Code = "WeChat", Name = "微信", Description = "微信 OAuth 登录", Sort = 3,
                ParentId = oauthType.Id,
                Items =
                [
                    new DictItem { Key = "AppId", Value = "", Description = "微信 App ID", Sort = 1 },
                    new DictItem { Key = "AppSecret", Value = "", Description = "微信 App Secret", IsSensitive = true, Sort = 2 },
                    new DictItem { Key = "IsEnabled", Value = "false", Description = "是否启用微信登录", Sort = 3 },
                ]
            },
            new()
            {
                Code = "JWT", Name = "JWT 配置", Description = "JWT Token 过期时间等配置", Sort = 1,
                ParentId = securityType.Id,
                Items =
                [
                    new DictItem { Key = "AccessTokenExpireMinutes", Value = "120", Description = "Access Token 过期时间（分钟）", Sort = 1 },
                    new DictItem { Key = "RefreshTokenExpireDays", Value = "7", Description = "Refresh Token 过期时间（天）", Sort = 2 },
                ]
            },
            new()
            {
                Code = "Password", Name = "密码策略", Description = "密码强度要求配置", Sort = 2,
                ParentId = securityType.Id,
                Items =
                [
                    new DictItem { Key = "MinLength", Value = "6", Description = "最小长度", Sort = 1 },
                    new DictItem { Key = "RequireUppercase", Value = "true", Description = "是否要求大写字母", Sort = 2 },
                    new DictItem { Key = "RequireLowercase", Value = "true", Description = "是否要求小写字母", Sort = 3 },
                    new DictItem { Key = "RequireDigit", Value = "true", Description = "是否要求数字", Sort = 4 },
                    new DictItem { Key = "RequireNonAlphanumeric", Value = "true", Description = "是否要求特殊字符", Sort = 5 },
                ]
            },
            new()
            {
                Code = "Upload", Name = "上传配置", Description = "文件上传相关配置", Sort = 1,
                ParentId = systemType.Id,
                Items =
                [
                    new DictItem { Key = "MaxAvatarSizeKB", Value = "512", Description = "头像最大大小（KB）", Sort = 1 },
                    new DictItem { Key = "AllowedAvatarTypes", Value = ".jpg,.jpeg,.png,.gif,.webp", Description = "允许的头像文件类型", Sort = 2 },
                ]
            },
            new()
            {
                Code = "General", Name = "通用配置", Description = "站点通用配置", Sort = 2,
                ParentId = systemType.Id,
                Items =
                [
                    new DictItem { Key = "SiteName", Value = "TPSSO", Description = "站点名称", Sort = 1 },
                    new DictItem { Key = "AllowRegistration", Value = "true", Description = "是否允许注册", Sort = 2 },
                    new DictItem { Key = "VerificationCodeExpireMinutes", Value = "5", Description = "验证码有效期（分钟）", Sort = 3 },
                ]
            },
            new()
            {
                Code = "SmtpServer", Name = "SMTP 配置", Description = "邮件发送服务器配置", Sort = 1,
                ParentId = smtpType.Id,
                Items =
                [
                    new DictItem { Key = "Host", Value = "smtp.qiye.aliyun.com", Description = "SMTP 服务器地址", Sort = 1 },
                    new DictItem { Key = "Port", Value = "587", Description = "SMTP 端口", Sort = 2 },
                    new DictItem { Key = "UseSsl", Value = "true", Description = "是否使用 SSL", Sort = 3 },
                    new DictItem { Key = "Username", Value = "tp@taipi.top", Description = "SMTP 用户名", Sort = 4 },
                    new DictItem { Key = "Password", Value = "", Description = "SMTP 密码", IsSensitive = true, Sort = 5 },
                    new DictItem { Key = "SenderName", Value = "TPSSO", Description = "发件人名称", Sort = 6 },
                    new DictItem { Key = "SenderEmail", Value = "tp@taipi.top", Description = "发件人邮箱", Sort = 7 },
                ]
            },
        };

        // 敏感配置加密存储
        foreach (var type in types)
        {
            foreach (var item in type.Items.Where(i => i.IsSensitive && !string.IsNullOrEmpty(i.Value)))
            {
                item.Value = AesEncryption.Encrypt(item.Value);
            }
        }

        context.DictTypes.AddRange(types);
        await context.SaveChangesAsync();

        logger.LogInformation("字典配置种子数据写入完成");
    }
}
