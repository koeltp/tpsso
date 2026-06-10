using Microsoft.EntityFrameworkCore;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using TPSSO.Infrastructure.Utils;

namespace TPSSO.Infrastructure.Services;

public class DictService : IDictService
{
    private readonly ApplicationDbContext _context;

    public DictService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseResult<List<DictTypeResult>>> GetAllAsync()
    {
        // 一次性查出所有类型和配置项，在内存中构建树
        var types = await _context.DictTypes
            .Include(t => t.Items)
            .OrderBy(t => t.Sort)
            .ToListAsync();

        var allResults = types.Select(t => new DictTypeResult
        {
            Id = t.Id,
            Code = t.Code,
            Name = t.Name,
            Description = t.Description,
            Sort = t.Sort,
            IsEnabled = t.IsEnabled,
            ParentId = t.ParentId,
            Items = t.Items
                .OrderBy(i => i.Sort)
                .Select(i => new DictItemResult
                {
                    Id = i.Id,
                    TypeId = i.TypeId,
                    Key = i.Key,
                    Value = i.IsSensitive ? "******" : i.Value,
                    IsSensitive = i.IsSensitive,
                    Description = i.Description,
                    Sort = i.Sort,
                    IsEnabled = i.IsEnabled,
                }).ToList()
        }).ToList();

        // 构建树形结构
        var tree = BuildTree(allResults);

        return new ResponseResult<List<DictTypeResult>>(tree) { Code = 200 };
    }

    /// <summary>
    /// 将扁平列表构建为树形结构
    /// </summary>
    private static List<DictTypeResult> BuildTree(List<DictTypeResult> all)
    {
        var lookup = all.ToDictionary(t => t.Id);
        var roots = new List<DictTypeResult>();

        foreach (var item in all)
        {
            if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
            {
                parent.Children.Add(item);
            }
            else
            {
                roots.Add(item);
            }
        }

        return roots;
    }

    public async Task<ResponseResult<DictTypeResult>> SaveTypeAsync(DictTypeDto dto)
    {
        // 防止循环引用：父分类不能是自身或自身的子级
        if (dto.ParentId.HasValue && dto.Id.HasValue)
        {
            if (dto.ParentId == dto.Id)
                return ResponseResult<DictTypeResult>.BadRequest("父分类不能是自身");

            if (await IsDescendantAsync(dto.Id.Value, dto.ParentId.Value))
                return ResponseResult<DictTypeResult>.BadRequest("父分类不能是自身的子级");
        }

        DictType entity;

        if (dto.Id.HasValue)
        {
            entity = await _context.DictTypes.FindAsync(dto.Id.Value)
                ?? throw new KeyNotFoundException("字典类型不存在");
            entity.Code = dto.Code;
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.Sort = dto.Sort;
            entity.IsEnabled = dto.IsEnabled;
            entity.ParentId = dto.ParentId;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            if (await _context.DictTypes.AnyAsync(t => t.Code == dto.Code))
                return ResponseResult<DictTypeResult>.BadRequest($"类型编码 {dto.Code} 已存在");

            entity = new DictType
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                Sort = dto.Sort,
                IsEnabled = dto.IsEnabled,
                ParentId = dto.ParentId,
            };
            _context.DictTypes.Add(entity);
        }

        await _context.SaveChangesAsync();

        return new ResponseResult<DictTypeResult>(new DictTypeResult
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            Sort = entity.Sort,
            IsEnabled = entity.IsEnabled,
            ParentId = entity.ParentId,
        }) { Code = 200, Message = dto.Id.HasValue ? "更新成功" : "创建成功" };
    }

    /// <summary>
    /// 检查 targetId 是否是 parentId 的后代
    /// </summary>
    private async Task<bool> IsDescendantAsync(Guid parentId, Guid targetId)
    {
        var allTypes = await _context.DictTypes.ToListAsync();
        var current = allTypes.FirstOrDefault(t => t.Id == targetId);
        while (current?.ParentId != null)
        {
            if (current.ParentId == parentId) return true;
            current = allTypes.FirstOrDefault(t => t.Id == current.ParentId);
        }
        return false;
    }

    public async Task<ResponseResult<bool>> DeleteTypeAsync(Guid id)
    {
        var entity = await _context.DictTypes
            .Include(t => t.Items)
            .Include(t => t.Children)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (entity == null)
            return ResponseResult<bool>.NotFound("字典类型不存在");

        // 递归删除子分类及其配置项
        await DeleteTypeRecursiveAsync(entity);

        await _context.SaveChangesAsync();

        return ResponseResult<bool>.Success("删除成功");
    }

    /// <summary>
    /// 递归删除分类及子分类
    /// </summary>
    private async Task DeleteTypeRecursiveAsync(DictType entity)
    {
        // 先递归删除子分类
        foreach (var child in entity.Children.ToList())
        {
            var loaded = await _context.DictTypes
                .Include(t => t.Items)
                .Include(t => t.Children)
                .FirstOrDefaultAsync(t => t.Id == child.Id);
            if (loaded != null)
                await DeleteTypeRecursiveAsync(loaded);
        }

        _context.DictItems.RemoveRange(entity.Items);
        _context.DictTypes.Remove(entity);
    }

    public async Task<ResponseResult<DictItemResult>> SaveItemAsync(Guid typeId, DictItemDto dto)
    {
        var typeExists = await _context.DictTypes.AnyAsync(t => t.Id == typeId);
        if (!typeExists)
            return ResponseResult<DictItemResult>.BadRequest("字典类型不存在");

        DictItem entity;

        if (dto.Id.HasValue)
        {
            entity = await _context.DictItems.FindAsync(dto.Id.Value)
                ?? throw new KeyNotFoundException("字典项不存在");

            if (entity.Key != dto.Key && await _context.DictItems.AnyAsync(i => i.TypeId == typeId && i.Key == dto.Key))
                return ResponseResult<DictItemResult>.BadRequest($"键 {dto.Key} 已存在");

            entity.Key = dto.Key;
            entity.Value = dto.IsSensitive ? AesEncryption.Encrypt(dto.Value) : dto.Value;
            entity.Description = dto.Description;
            entity.IsSensitive = dto.IsSensitive;
            entity.Sort = dto.Sort;
            entity.IsEnabled = dto.IsEnabled;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            if (await _context.DictItems.AnyAsync(i => i.TypeId == typeId && i.Key == dto.Key))
                return ResponseResult<DictItemResult>.BadRequest($"键 {dto.Key} 已存在");

            entity = new DictItem
            {
                TypeId = typeId,
                Key = dto.Key,
                Value = dto.IsSensitive ? AesEncryption.Encrypt(dto.Value) : dto.Value,
                Description = dto.Description,
                IsSensitive = dto.IsSensitive,
                Sort = dto.Sort,
                IsEnabled = dto.IsEnabled,
            };
            _context.DictItems.Add(entity);
        }

        await _context.SaveChangesAsync();

        return new ResponseResult<DictItemResult>(new DictItemResult
        {
            Id = entity.Id,
            TypeId = entity.TypeId,
            Key = entity.Key,
            Value = entity.IsSensitive ? "******" : entity.Value,
            IsSensitive = entity.IsSensitive,
            Description = entity.Description,
            Sort = entity.Sort,
            IsEnabled = entity.IsEnabled,
        }) { Code = 200, Message = dto.Id.HasValue ? "更新成功" : "创建成功" };
    }

    public async Task<ResponseResult<bool>> DeleteItemAsync(Guid id)
    {
        var entity = await _context.DictItems.FindAsync(id);
        if (entity == null)
            return ResponseResult<bool>.NotFound("字典项不存在");

        _context.DictItems.Remove(entity);
        await _context.SaveChangesAsync();

        return ResponseResult<bool>.Success("删除成功");
    }
}
