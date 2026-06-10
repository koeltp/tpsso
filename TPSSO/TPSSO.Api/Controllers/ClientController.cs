using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;

namespace TPSSO.Api.Controllers;

[ApiController]
[Route("api/client")]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientController(IClientService clientService)
    {
        _clientService = clientService;
    }

    /// <summary>
    /// 创建客户端
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ResponseResult<ClientCreatedResult>> Create([FromBody] CreateClientModel model)
    {
        var userId = GetUserId();
        return await _clientService.CreateAsync(model, userId);
    }

    /// <summary>
    /// 搜索我的客户端（分页）
    /// </summary>
    [HttpPost("my/search")]
    [Authorize]
    public async Task<PagerResponseResult<ClientResult>> SearchMy([FromBody] SearchPager<ClientSearchCondition> pager)
    {
        var userId = GetUserId();
        return await _clientService.SearchAsync(pager, userId, isAdmin: false);
    }

    /// <summary>
    /// 搜索客户端（管理员，分页）
    /// </summary>
    [HttpPost("search")]
    [Authorize(Roles = "Admin")]
    public async Task<PagerResponseResult<ClientResult>> Search([FromBody] SearchPager<ClientSearchCondition> pager)
    {
        var userId = GetUserId();
        return await _clientService.SearchAsync(pager, userId, isAdmin: true);
    }

    /// <summary>
    /// 客户端详情
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ResponseResult<ClientResult>> GetById(Guid id)
    {
        return await _clientService.GetByIdAsync(id);
    }

    /// <summary>
    /// 更新客户端（仅草稿状态）
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ResponseResult<bool>> Update(Guid id, [FromBody] UpdateClientModel model)
    {
        var userId = GetUserId();
        return await _clientService.UpdateAsync(id, userId, model);
    }

    /// <summary>
    /// 提交审核
    /// </summary>
    [HttpPost("{id}/submit")]
    [Authorize]
    public async Task<ResponseResult<bool>> Submit(Guid id)
    {
        var userId = GetUserId();
        return await _clientService.SubmitAsync(id, userId);
    }

    /// <summary>
    /// 撤回审核
    /// </summary>
    [HttpPost("{id}/withdraw")]
    [Authorize]
    public async Task<ResponseResult<bool>> Withdraw(Guid id)
    {
        var userId = GetUserId();
        return await _clientService.WithdrawAsync(id, userId);
    }

    /// <summary>
    /// 删除客户端
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ResponseResult<bool>> Delete(Guid id)
    {
        var userId = GetUserId();
        return await _clientService.DeleteAsync(id, userId);
    }

    /// <summary>
    /// 审核通过（管理员）
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ResponseResult<bool>> Approve(Guid id)
    {
        var reviewerId = GetUserId();
        return await _clientService.ApproveAsync(id, reviewerId);
    }

    /// <summary>
    /// 审核拒绝（管理员）
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ResponseResult<bool>> Reject(Guid id, [FromBody] RejectClientModel model)
    {
        var reviewerId = GetUserId();
        return await _clientService.RejectAsync(id, reviewerId, model.Reason);
    }

    /// <summary>
    /// 从 Claims 中直接读取当前用户 ID，避免不必要的数据库查询
    /// </summary>
    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.Parse(userIdStr!);
    }
}
