namespace TPSSO.Application.Exceptions;

/// <summary>
/// 业务错误码常量，6位编码：前3位=模块（110/120/130...），后3位=具体错误（001/002/003...）
/// 0=成功，非0=错误
/// </summary>
public static class AppCodes
{
    // ── 系统 110（框架异常，中间件使用） ──
    public const int SystemUnauthorized = 110001;
    public const int SystemBadRequest = 110002;
    public const int SystemNotFound = 110003;
    public const int SystemError = 110999;

    // ── 通用 120 ──
    public const int InvalidParameter = 120001;
    public const int ConcurrencyConflict = 120002;

    // ── 认证 130 ──
    public const int InvalidCredentials = 130001;
    public const int AccountDisabled = 130002;

    // ── 用户 140 ──
    public const int UserNotFound = 140001;
    public const int EmailExists = 140002;
    public const int RoleNotFound = 140003;
    public const int CannotRemoveOwnAdmin = 140004;

    // ── 验证码 150 ──
    public const int CodeExpired = 150001;
    public const int SendTooFrequent = 150002;
    public const int EmailNotRegistered = 150003;

    // ── 客户端 160 ──
    public const int ClientNotFound = 160001;
    public const int ClientNoPermission = 160002;
    public const int ClientInvalidStatus = 160003;
    public const int ClientInvalidRedirectUri = 160004;
    public const int ClientBuiltIn = 160005;
    public const int ClientNoSecret = 160006;

    // ── 字典 170 ──
    public const int DictTypeNotFound = 170001;
    public const int DictCodeExists = 170002;
    public const int DictItemNotFound = 170003;
    public const int DictParentSelf = 170004;

    // ── 上传 180 ──
    public const int UploadEmpty = 180001;
    public const int UploadInvalidType = 180002;
    public const int UploadTooLarge = 180003;

    // ── 两步验证 190 ──
    public const int TwoFactorRequired = 190001;
    public const int TwoFactorInvalidCode = 190002;
    public const int TwoFactorNotEnabled = 190003;
    public const int TwoFactorAlreadyEnabled = 190004;
    public const int TwoFactorSetupRequired = 190005;
}
