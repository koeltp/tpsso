using System.ComponentModel.DataAnnotations;
using TPSSO.Domain.Entities;

namespace TPSSO.Application.Models;

/// <summary>
/// 审核拒绝请求
/// </summary>
public class RejectClientModel
{
    /// <summary>拒绝原因</summary>
    [Required(ErrorMessage = "拒绝原因不能为空")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 更新客户端请求
/// </summary>
public class UpdateClientModel
{
    /// <summary>应用名称</summary>
    [Required(ErrorMessage = "应用名称不能为空")]
    public string Name { get; set; } = string.Empty;

    /// <summary>应用描述</summary>
    public string? Description { get; set; }

    /// <summary>应用 Logo 地址</summary>
    public string? Logo { get; set; }

    /// <summary>回调地址（多个用换行分隔）</summary>
    [Required(ErrorMessage = "回调地址不能为空")]
    public string RedirectUris { get; set; } = string.Empty;

    /// <summary>允许的授权范围（空格分隔）</summary>
    public string AllowedScopes { get; set; } = "openid profile email";

    /// <summary>允许的授权类型（空格分隔，默认 authorization_code refresh_token）</summary>
    public string GrantTypes { get; set; } = "authorization_code refresh_token";

    /// <summary>授权确认类型：explicit=每次需用户确认，implicit=自动确认</summary>
    public string ConsentType { get; set; } = "explicit";

    /// <summary>乐观并发令牌，更新时必须传回服务端上次返回的值</summary>
    public string? RowVersion { get; set; }
}
