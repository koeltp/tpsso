using Taipi.Core.RQRS;
using TPSSO.Application.Models;

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
    /// 搜索客户端（分页），管理员可查所有，普通用户仅查自己的
    /// </summary>
    Task<PagerResponseResult<ClientResult>> SearchAsync(SearchPager<ClientSearchCondition> pager, Guid userId, bool isAdmin);

    /// <summary>
    /// 获取客户端详情
    /// </summary>
    Task<ResponseResult<ClientResult>> GetByIdAsync(Guid id);

    /// <summary>
    /// 重置客户端密钥（仅机密类型），返回新的明文 Secret
    /// </summary>
    Task<ResponseResult<ClientCreatedResult>> RegenerateSecretAsync(Guid id, Guid userId);
}
