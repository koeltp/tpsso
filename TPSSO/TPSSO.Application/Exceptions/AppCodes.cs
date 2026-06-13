namespace TPSSO.Application.Exceptions;

/// <summary>
/// 业务错误码常量，4位编码：前2位=模块，后2位=具体错误
/// 0=成功，非0=错误
/// </summary>
public static class AppCodes
{
    // ── 通用 00xx ──
    public const int InvalidParameter = 1;
    public const int ConcurrencyConflict = 2;

    // ── 认证 10xx ──
    public const int InvalidCredentials = 1001;
    public const int AccountDisabled = 1002;

    // ── 用户 20xx ──
    public const int UserNotFound = 2001;
    public const int EmailExists = 2002;
    public const int RoleNotFound = 2003;
    public const int CannotRemoveOwnAdmin = 2004;

    // ── 验证码 30xx ──
    public const int CodeExpired = 3001;
    public const int SendTooFrequent = 3002;
    public const int EmailNotRegistered = 3003;

    // ── 客户端 40xx ──
    public const int ClientNotFound = 4001;
    public const int ClientNoPermission = 4002;
    public const int ClientInvalidStatus = 4003;
    public const int ClientInvalidRedirectUri = 4004;
    public const int ClientBuiltIn = 4005;
    public const int ClientNoSecret = 4006;

    // ── 字典 50xx ──
    public const int DictTypeNotFound = 5001;
    public const int DictCodeExists = 5002;
    public const int DictItemNotFound = 5003;
    public const int DictParentSelf = 5004;

    // ── 上传 60xx ──
    public const int UploadEmpty = 6001;
    public const int UploadInvalidType = 6002;
    public const int UploadTooLarge = 6003;
}
