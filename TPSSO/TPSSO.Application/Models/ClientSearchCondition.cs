using System.Text.Json.Serialization;
using TPSSO.Domain.Entities;

namespace TPSSO.Application.Models;

/// <summary>
/// 客户端搜索条件
/// </summary>
public class ClientSearchCondition
{
    /// <summary>按名称或 ClientId 模糊搜索</summary>
    public string? Keyword { get; set; }

    /// <summary>按状态筛选</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ClientStatus>))]
    public ClientStatus? Status { get; set; }
}
