using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;

namespace TPSSO.Admin.Controllers;

[ApiController]
[Route("api/user")]
[Authorize(Roles = "Admin")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 搜索用户（管理员，分页）
    /// </summary>
    [HttpPost("search")]
    public async Task<PagerResponseResult<UserListResult>> Search([FromBody] SearchPager<UserSearchCondition> pager)
    {
        return await _userService.SearchAsync(pager);
    }

    /// <summary>
    /// 用户详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ResponseResult<UserListResult>> GetById(Guid id)
    {
        return await _userService.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    [HttpGet("roles")]
    public async Task<ResponseResult<List<RoleResult>>> GetRoles()
    {
        return await _userService.GetRolesAsync();
    }

    /// <summary>
    /// 修改用户角色
    /// </summary>
    [HttpPut("{id}/roles")]
    public async Task<ResponseResult<bool>> UpdateRoles(Guid id, [FromBody] UpdateUserRolesModel model)
    {
        var operatorId = GetUserId();
        return await _userService.UpdateRolesAsync(id, operatorId, model);
    }

    /// <summary>
    /// 禁用用户（锁定）
    /// </summary>
    [HttpPost("{id}/lock")]
    public async Task<ResponseResult<bool>> Lock(Guid id)
    {
        return await _userService.LockAsync(id);
    }

    /// <summary>
    /// 启用用户（解锁）
    /// </summary>
    [HttpPost("{id}/unlock")]
    public async Task<ResponseResult<bool>> Unlock(Guid id)
    {
        return await _userService.UnlockAsync(id);
    }

    /// <summary>
    /// 管理员重置用户密码
    /// </summary>
    [HttpPost("{id}/reset-password")]
    public async Task<ResponseResult<bool>> ResetPassword(Guid id, [FromBody] AdminResetPasswordModel model)
    {
        return await _userService.ResetPasswordAsync(id, model);
    }

    /// <summary>
    /// 从 Claims 中读取当前用户 ID
    /// </summary>
    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        return Guid.Parse(userIdStr!);
    }
}
