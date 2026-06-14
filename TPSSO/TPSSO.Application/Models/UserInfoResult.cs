namespace TPSSO.Application.Models;

/// <summary>
/// 用户信息返回结果
/// </summary>
public class UserInfoResult
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string? NickName { get; set; }

    /// <summary>用户角色列表</summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>是否启用两步验证</summary>
    public bool TwoFactorEnabled { get; set; }
}

/// <summary>
/// 修改个人信息请求
/// </summary>
public class UpdateProfileModel
{
    /// <summary>昵称</summary>
    public string? NickName { get; set; }

    /// <summary>头像地址</summary>
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// 修改密码请求
/// </summary>
public class ChangePasswordModel
{
    /// <summary>当前密码</summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>新密码</summary>
    public string NewPassword { get; set; } = string.Empty;
}
