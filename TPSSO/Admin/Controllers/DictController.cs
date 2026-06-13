using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;

namespace TPSSO.Admin.Controllers;

[ApiController]
[Route("api/dict")]
[Authorize(Roles = "Admin")]
public class DictController : ControllerBase
{
    private readonly IDictService _dictService;

    public DictController(IDictService dictService)
    {
        _dictService = dictService;
    }

    /// <summary>
    /// 获取所有字典配置（树形）
    /// </summary>
    [HttpGet]
    public async Task<ResponseResult<List<DictTypeResult>>> GetAll()
    {
        var data = await _dictService.GetAllAsync();
        return new ResponseResult<List<DictTypeResult>>(data);
    }

    /// <summary>
    /// 创建或更新字典类型
    /// </summary>
    [HttpPost("types")]
    public async Task<ResponseResult<DictTypeResult>> SaveType([FromBody] DictTypeDto dto)
    {
        var data = await _dictService.SaveTypeAsync(dto);
        return new ResponseResult<DictTypeResult>(data) { Message = dto.Id.HasValue ? "更新成功" : "创建成功" };
    }

    /// <summary>
    /// 删除字典类型
    /// </summary>
    [HttpDelete("types/{id}")]
    public async Task<StatusResponseResult> DeleteType(Guid id)
    {
        await _dictService.DeleteTypeAsync(id);
        return StatusResponseResult.Success("删除成功");
    }

    /// <summary>
    /// 创建或更新字典项
    /// </summary>
    [HttpPost("types/{typeId}/items")]
    public async Task<ResponseResult<DictItemResult>> SaveItem(Guid typeId, [FromBody] DictItemDto dto)
    {
        var data = await _dictService.SaveItemAsync(typeId, dto);
        return new ResponseResult<DictItemResult>(data) { Message = dto.Id.HasValue ? "更新成功" : "创建成功" };
    }

    /// <summary>
    /// 删除字典项
    /// </summary>
    [HttpDelete("items/{id}")]
    public async Task<StatusResponseResult> DeleteItem(Guid id)
    {
        await _dictService.DeleteItemAsync(id);
        return StatusResponseResult.Success("删除成功");
    }
}
