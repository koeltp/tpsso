namespace TPSSO.Domain.Entities;

/// <summary>
/// 客户端状态枚举
/// </summary>
public enum ClientStatus
{
    /// <summary>草稿</summary>
    Draft = 0,
    /// <summary>待审核</summary>
    Pending = 1,
    /// <summary>已通过</summary>
    Approved = 2,
    /// <summary>已拒绝</summary>
    Rejected = 3
}
