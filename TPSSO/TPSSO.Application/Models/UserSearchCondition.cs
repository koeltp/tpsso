using System.Text.Json.Serialization;

namespace TPSSO.Application.Models;

/// <summary>
/// 用户状态枚举
/// </summary>
public enum UserStatus
{
    /// <summary>正常</summary>
    Active,

    /// <summary>已禁用（锁定）</summary>
    Locked
}

/// <summary>
/// 用户搜索条件
/// </summary>
public class UserSearchCondition
{
    /// <summary>按用户名、邮箱或昵称模糊搜索</summary>
    public string? Keyword { get; set; }

    /// <summary>按角色筛选</summary>
    public string? Role { get; set; }

    /// <summary>按状态筛选</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<UserStatus>))]
    public UserStatus? Status { get; set; }
}
