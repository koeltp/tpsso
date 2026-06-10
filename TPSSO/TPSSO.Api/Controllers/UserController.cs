using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;

namespace TPSSO.Api.Controllers;

[ApiController]
[Route("api/user")]
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
    [Authorize(Roles = "Admin")]
    public async Task<PagerResponseResult<UserListResult>> Search([FromBody] SearchPager<UserSearchCondition> pager)
    {
        return await _userService.SearchAsync(pager);
    }

    /// <summary>
    /// 用户详情（管理员）
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ResponseResult<UserListResult>> GetById(Guid id)
    {
        return await _userService.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ResponseResult<List<RoleResult>>> GetRoles()
    {
        return await _userService.GetRolesAsync();
    }

    /// <summary>
    /// 修改用户角色（管理员）
    /// </summary>
    [HttpPut("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ResponseResult<bool>> UpdateRoles(Guid id, [FromBody] UpdateUserRolesModel model)
    {
        var operatorId = GetUserId();
        return await _userService.UpdateRolesAsync(id, operatorId, model);
    }

    /// <summary>
    /// 禁用用户（管理员）
    /// </summary>
    [HttpPost("{id}/lock")]
    [Authorize(Roles = "Admin")]
    public async Task<ResponseResult<bool>> Lock(Guid id)
    {
        return await _userService.LockAsync(id);
    }

    /// <summary>
    /// 启用用户（管理员）
    /// </summary>
    [HttpPost("{id}/unlock")]
    [Authorize(Roles = "Admin")]
    public async Task<ResponseResult<bool>> Unlock(Guid id)
    {
        return await _userService.UnlockAsync(id);
    }

    /// <summary>
    /// 重置用户密码（管理员）
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<ResponseResult<bool>> ResetPassword(Guid id, [FromBody] ResetPasswordModel model)
    {
        return await _userService.ResetPasswordAsync(id, model);
    }

    /// <summary>
    /// 从 Claims 中读取当前用户 ID
    /// </summary>
    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.Parse(userIdStr!);
    }
}
