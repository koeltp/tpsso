using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using System.Security.Claims;
using Taipi.Core.RQRS;
using Taipi.Core.Exceptions;
using TPSSO.Application.Exceptions;
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
        var data = await _userService.GetByIdAsync(id);
        return new ResponseResult<UserListResult>(data);
    }

    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    [HttpGet("roles")]
    public async Task<ResponseResult<List<RoleResult>>> GetRoles()
    {
        var data = await _userService.GetRolesAsync();
        return new ResponseResult<List<RoleResult>>(data);
    }

    /// <summary>
    /// 修改用户角色
    /// </summary>
    [HttpPut("{id}/roles")]
    public async Task<StatusResponseResult> UpdateRoles(Guid id, [FromBody] UpdateUserRolesModel model)
    {
        var operatorId = GetUserId();
        await _userService.UpdateRolesAsync(id, operatorId, model);
        return StatusResponseResult.Success("角色更新成功");
    }

    /// <summary>
    /// 禁用用户（锁定）
    /// </summary>
    [HttpPost("{id}/lock")]
    public async Task<StatusResponseResult> Lock(Guid id)
    {
        await _userService.LockAsync(id);
        return StatusResponseResult.Success("已禁用");
    }

    /// <summary>
    /// 启用用户（解锁）
    /// </summary>
    [HttpPost("{id}/unlock")]
    public async Task<StatusResponseResult> Unlock(Guid id)
    {
        await _userService.UnlockAsync(id);
        return StatusResponseResult.Success("已启用");
    }

    /// <summary>
    /// 管理员重置用户密码
    /// </summary>
    [HttpPost("{id}/reset-password")]
    public async Task<StatusResponseResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordModel model)
    {
        await _userService.ResetPasswordAsync(id, model);
        return StatusResponseResult.Success("密码已重置");
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
