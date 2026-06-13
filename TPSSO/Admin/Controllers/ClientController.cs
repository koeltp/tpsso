using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Taipi.Core.RQRS;
using Taipi.Core.Exceptions;
using TPSSO.Application.Exceptions;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;
using TPSSO.Application.Options;

namespace TPSSO.Admin.Controllers;

[ApiController]
[Route("api/client")]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly IWebHostEnvironment _env;
    private readonly UploadOptions _uploadOptions;

    public ClientController(IClientService clientService, IWebHostEnvironment env, IOptions<UploadOptions> uploadOptions)
    {
        _clientService = clientService;
        _env = env;
        _uploadOptions = uploadOptions.Value;
    }

    /// <summary>
    /// 创建客户端
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ResponseResult<ClientCreatedResult>> Create([FromBody] CreateClientModel model)
    {
        var userId = GetUserId();
        var data = await _clientService.CreateAsync(model, userId);
        return new ResponseResult<ClientCreatedResult>(data) { Message = "客户端创建成功" };
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
        var data = await _clientService.GetByIdAsync(id);
        return new ResponseResult<ClientResult>(data);
    }

    /// <summary>
    /// 更新客户端（仅草稿状态）
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<StatusResponseResult> Update(Guid id, [FromBody] UpdateClientModel model)
    {
        var userId = GetUserId();
        await _clientService.UpdateAsync(id, userId, model);
        return StatusResponseResult.Success("更新成功");
    }

    /// <summary>
    /// 提交审核
    /// </summary>
    [HttpPost("{id}/submit")]
    [Authorize]
    public async Task<StatusResponseResult> Submit(Guid id)
    {
        var userId = GetUserId();
        await _clientService.SubmitAsync(id, userId);
        return StatusResponseResult.Success("已提交审核");
    }

    /// <summary>
    /// 撤回审核
    /// </summary>
    [HttpPost("{id}/withdraw")]
    [Authorize]
    public async Task<StatusResponseResult> Withdraw(Guid id)
    {
        var userId = GetUserId();
        await _clientService.WithdrawAsync(id, userId);
        return StatusResponseResult.Success("已撤回，可重新编辑");
    }

    /// <summary>
    /// 删除客户端
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<StatusResponseResult> Delete(Guid id)
    {
        var userId = GetUserId();
        await _clientService.DeleteAsync(id, userId);
        return StatusResponseResult.Success("已删除");
    }

    /// <summary>
    /// 审核通过（管理员）
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<StatusResponseResult> Approve(Guid id)
    {
        var reviewerId = GetUserId();
        await _clientService.ApproveAsync(id, reviewerId);
        return StatusResponseResult.Success("审核通过");
    }

    /// <summary>
    /// 审核拒绝（管理员）
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<StatusResponseResult> Reject(Guid id, [FromBody] RejectClientModel model)
    {
        var reviewerId = GetUserId();
        await _clientService.RejectAsync(id, reviewerId, model.Reason);
        return StatusResponseResult.Success("已拒绝");
    }

    /// <summary>
    /// 重置客户端密钥（仅机密类型）
    /// </summary>
    [HttpPost("{id}/regenerate-secret")]
    [Authorize]
    public async Task<ResponseResult<ClientCreatedResult>> RegenerateSecret(Guid id)
    {
        var userId = GetUserId();
        var data = await _clientService.RegenerateSecretAsync(id, userId);
        return new ResponseResult<ClientCreatedResult>(data) { Message = "密钥已重置" };
    }

    /// <summary>
    /// 上传客户端 Logo
    /// </summary>
    [HttpPost("{id}/logo")]
    [Authorize]
    public async Task<ResponseResult<string>> UploadLogo(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new BadRequestException(AppCodes.UploadEmpty, "请选择文件");

        var allowedTypes = _uploadOptions.LogoAllowedTypes.Split(',');
        if (!allowedTypes.Contains(file.ContentType))
        {
            var extensions = allowedTypes.Select(x => x.Split('/')[1]).ToArray();
            throw new BadRequestException(AppCodes.UploadInvalidType, $"仅支持 {string.Join("、", extensions)} 格式");
        }
        if (file.Length > _uploadOptions.LogoMaxSizeMB * 1024 * 1024)
            throw new BadRequestException(AppCodes.UploadTooLarge, $"文件大小不能超过 {_uploadOptions.LogoMaxSizeMB}MB");

        var userId = GetUserId();
        var ext = Path.GetExtension(file.FileName);
        var newFileName = $"{id}_{Guid.NewGuid():N}{ext}";
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var logoDir = Path.Combine(webRoot, "logos");

        if (!Directory.Exists(logoDir))
            Directory.CreateDirectory(logoDir);

        var filePath = Path.Combine(logoDir, newFileName);
        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fs);
        }

        var logoUrl = $"/logos/{newFileName}";
        await _clientService.UpdateLogoAsync(id, userId, logoUrl);
        return new ResponseResult<string>(logoUrl) { Message = "Logo 上传成功" };
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
