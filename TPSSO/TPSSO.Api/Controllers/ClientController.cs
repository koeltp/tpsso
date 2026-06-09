using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;
using TPSSO.Domain.Entities;

namespace TPSSO.Api.Controllers;

[ApiController]
[Route("api/client")]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly UserManager<User> _userManager;

    public ClientController(IClientService clientService, UserManager<User> userManager)
    {
        _clientService = clientService;
        _userManager = userManager;
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
    /// 我创建的客户端
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<ResponseResult<List<ClientResult>>> GetMyClients()
    {
        var userId = GetUserId();
        return await _clientService.GetMyClientsAsync(userId);
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
    /// 待审核列表（管理员）
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<ResponseResult<List<ClientResult>>> GetPending()
    {
        return await _clientService.GetPendingAsync();
    }

    /// <summary>
    /// 所有客户端列表（管理员）
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ResponseResult<List<ClientResult>>> GetAll()
    {
        return await _clientService.GetAllAsync();
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

    private Guid GetUserId()
    {
        var userIdStr = _userManager.GetUserId(User);
        return Guid.Parse(userIdStr!);
    }
}
