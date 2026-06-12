namespace TPSSO.Application.Models;

/// <summary>
/// 角色列表项
/// </summary>
public class RoleResult
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// 用户列表项
/// </summary>
public class UserListResult
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? NickName { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsLockedOut { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 修改用户角色请求
/// </summary>
public class UpdateUserRolesModel
{
    /// <summary>角色列表</summary>
    public List<string> Roles { get; set; } = [];
}

/// <summary>
/// 管理员重置密码请求
/// </summary>
public class AdminResetPasswordModel
{
    /// <summary>新密码</summary>
    public string NewPassword { get; set; } = string.Empty;
}
