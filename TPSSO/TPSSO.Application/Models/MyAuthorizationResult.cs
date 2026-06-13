namespace TPSSO.Application.Models;

/// <summary>
/// 我的授权记录
/// </summary>
public class MyAuthorizationResult
{
    public string Id { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
