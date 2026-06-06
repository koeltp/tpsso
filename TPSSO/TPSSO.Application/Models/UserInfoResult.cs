namespace TPSSO.Application.Models;

/// <summary>
/// 用户信息返回结果
/// </summary>
public class UserInfoResult
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}
