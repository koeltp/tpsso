using System.ComponentModel.DataAnnotations;

namespace TPSSO.Application.Models;

/// <summary>
/// 创建客户端请求
/// </summary>
public class CreateClientModel
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

    /// <summary>允许的授权范围（空格分隔，默认 openid profile email）</summary>
    public string AllowedScopes { get; set; } = "openid profile email";

    /// <summary>是否公开客户端（SPA/移动端）</summary>
    public bool IsPublic { get; set; } = true;
}
