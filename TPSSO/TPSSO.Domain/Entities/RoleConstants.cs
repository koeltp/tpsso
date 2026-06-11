namespace TPSSO.Domain.Entities;

/// <summary>
/// 角色名常量，避免业务逻辑中硬编码字符串
/// </summary>
public static class RoleConstants
{
    public const string Admin = "Admin";
    public const string User = "User";
}

/// <summary>
/// 系统内置客户端 ID 常量，这些客户端不允许删除
/// </summary>
public static class SystemClientIds
{
    /// <summary>管理后台客户端</summary>
    public const string AdminClient = "tpsso_admin_client";

    /// <summary>用户门户客户端</summary>
    public const string PortalClient = "tpsso_portal_client";
}
