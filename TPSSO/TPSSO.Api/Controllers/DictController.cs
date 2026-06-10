using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;

namespace TPSSO.Api.Controllers;

[Route("api/dict")]
[ApiController]
[Authorize(Roles = "Admin")]
public class DictController : ControllerBase
{
    private readonly IDictService _dictService;

    public DictController(IDictService dictService)
    {
        _dictService = dictService;
    }

    /// <summary>
    /// 获取所有字典类型（含字典项）
    /// </summary>
    [HttpGet]
    public async Task<ResponseResult<List<DictTypeResult>>> GetAll()
    {
        return await _dictService.GetAllAsync();
    }

    /// <summary>
    /// 创建或更新字典类型
    /// </summary>
    [HttpPost("types")]
    public async Task<ResponseResult<DictTypeResult>> SaveType([FromBody] DictTypeDto dto)
    {
        return await _dictService.SaveTypeAsync(dto);
    }

    /// <summary>
    /// 删除字典类型
    /// </summary>
    [HttpDelete("types/{id}")]
    public async Task<ResponseResult<bool>> DeleteType(Guid id)
    {
        return await _dictService.DeleteTypeAsync(id);
    }

    /// <summary>
    /// 创建或更新字典项
    /// </summary>
    [HttpPost("types/{typeId}/items")]
    public async Task<ResponseResult<DictItemResult>> SaveItem(Guid typeId, [FromBody] DictItemDto dto)
    {
        return await _dictService.SaveItemAsync(typeId, dto);
    }

    /// <summary>
    /// 删除字典项
    /// </summary>
    [HttpDelete("items/{id}")]
    public async Task<ResponseResult<bool>> DeleteItem(Guid id)
    {
        return await _dictService.DeleteItemAsync(id);
    }
}
