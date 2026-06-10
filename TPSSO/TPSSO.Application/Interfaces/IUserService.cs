using Taipi.Core.RQRS;
using TPSSO.Application.Models;

namespace TPSSO.Application.Interfaces;

/// <summary>
/// 用户管理服务接口（管理员）
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 搜索用户（分页）
    /// </summary>
    Task<PagerResponseResult<UserListResult>> SearchAsync(SearchPager<UserSearchCondition> pager);

    /// <summary>
    /// 获取用户详情
    /// </summary>
    Task<ResponseResult<UserListResult>> GetByIdAsync(Guid id);

    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    Task<ResponseResult<List<RoleResult>>> GetRolesAsync();

    /// <summary>
    /// 修改用户角色
    /// </summary>
    Task<ResponseResult<bool>> UpdateRolesAsync(Guid id, Guid operatorId, UpdateUserRolesModel model);

    /// <summary>
    /// 禁用用户（锁定）
    /// </summary>
    Task<ResponseResult<bool>> LockAsync(Guid id);

    /// <summary>
    /// 启用用户（解锁）
    /// </summary>
    Task<ResponseResult<bool>> UnlockAsync(Guid id);

    /// <summary>
    /// 管理员重置用户密码
    /// </summary>
    Task<ResponseResult<bool>> ResetPasswordAsync(Guid id, ResetPasswordModel model);
}
