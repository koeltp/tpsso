-- ============================================================
-- TPSSO 生产环境配置 SQL
-- 使用前请替换所有 <<占位符>> 为实际值
-- 敏感字段（IsSensitive=1）的 Value 需填入 AES 加密后的密文
-- 加密工具：管理后台字典配置页面（自动加密）或 AesEncryption.Encrypt()
-- ============================================================

-- ──────── 1. GitHub 第三方登录 ────────

-- 启用 GitHub 登录

-- GitHub ClientId（非敏感，明文）
UPDATE DictItems SET Value = 'Ov23lietsdCwxFEgi6Ky', UpdatedAt = UTC_TIMESTAMP()
WHERE TypeId = (SELECT Id FROM DictTypes WHERE Code = 'GitHub') AND `Key` = 'ClientId';

-- GitHub ClientSecret（敏感，需填入 AES 加密后的密文）
UPDATE DictItems SET Value = '<<AES 加密后的 Client Secret>>', UpdatedAt = UTC_TIMESTAMP()
WHERE TypeId = (SELECT Id FROM DictTypes WHERE Code = 'GitHub') AND `Key` = 'ClientSecret';


-- ──────── 2. OAuth 客户端回调地址（localhost → 生产域名） ────────
-- 注意：需要同时更新 OpenIddict Applications 表和业务表 ClientApplications/ClientRedirectUris

-- 管理后台（tpssoadmin → https://admin.taipi.top）
UPDATE Applications SET RedirectUris = '["https://admin.taipi.top/callback"]'
WHERE ClientId = 'tpsso_admin_client';

UPDATE Applications SET PostLogoutRedirectUris = '["https://admin.taipi.top"]'
WHERE ClientId = 'tpsso_admin_client';

UPDATE ClientRedirectUris SET Uri = 'https://admin.taipi.top/callback'
WHERE ClientApplicationId = (SELECT Id FROM ClientApplications WHERE ClientId = 'tpsso_admin_client')
  AND Uri = 'http://localhost:3009/callback';

-- ──────── 3. SMTP 邮件配置 ────────

-- SMTP 密码（敏感，需填入 AES 加密后的密文）
UPDATE DictItems SET Value = '<<AES 加密后的 SMTP 密码>>', UpdatedAt = UTC_TIMESTAMP()
WHERE TypeId = (SELECT Id FROM DictTypes WHERE Code = 'SmtpServer') AND `Key` = 'Password';