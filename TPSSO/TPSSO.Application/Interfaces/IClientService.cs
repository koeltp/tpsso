using Taipi.Core.RQRS;
using TPSSO.Application.Models;
using TPSSO.Domain.Entities;

namespace TPSSO.Application.Interfaces;

/// <summary>
/// 客户端管理服务接口
/// </summary>
public interface IClientService
{
    /// <summary>
    /// 创建客户端（草稿状态）
    /// </summary>
    Task<ResponseResult<ClientCreatedResult>> CreateAsync(CreateClientModel model, Guid userId);

    /// <summary>
    /// 提交审核
    /// </summary>
    Task<ResponseResult<bool>> SubmitAsync(Guid id, Guid userId);

    /// <summary>
    /// 撤回审核（回到草稿状态）
    /// </summary>
    Task<ResponseResult<bool>> WithdrawAsync(Guid id, Guid userId);

    /// <summary>
    /// 审核通过（同步到 OpenIddict）
    /// </summary>
    Task<ResponseResult<bool>> ApproveAsync(Guid id, Guid reviewerId);

    /// <summary>
    /// 审核拒绝
    /// </summary>
    Task<ResponseResult<bool>> RejectAsync(Guid id, Guid reviewerId, string reason);

    /// <summary>
    /// 更新客户端（仅草稿状态可编辑）
    /// </summary>
    Task<ResponseResult<bool>> UpdateAsync(Guid id, Guid userId, UpdateClientModel model);

    /// <summary>
    /// 删除客户端
    /// </summary>
    Task<ResponseResult<bool>> DeleteAsync(Guid id, Guid userId);

    /// <summary>
    /// 获取我创建的客户端
    /// </summary>
    Task<ResponseResult<List<ClientResult>>> GetMyClientsAsync(Guid userId);

    /// <summary>
    /// 获取客户端详情
    /// </summary>
    Task<ResponseResult<ClientResult>> GetByIdAsync(Guid id);

    /// <summary>
    /// 获取待审核列表
    /// </summary>
    Task<ResponseResult<List<ClientResult>>> GetPendingAsync();

    /// <summary>
    /// 获取所有客户端（管理员）
    /// </summary>
    Task<ResponseResult<List<ClientResult>>> GetAllAsync();
}
