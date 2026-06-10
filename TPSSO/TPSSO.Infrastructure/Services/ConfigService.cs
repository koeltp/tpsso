using Microsoft.EntityFrameworkCore;
using TPSSO.Application.Interfaces;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using TPSSO.Infrastructure.Utils;

namespace TPSSO.Infrastructure.Services;

/// <summary>
/// 配置读取服务，供业务代码按 typeCode + key 读取配置
/// 优先从数据库读取，未找到则返回默认值
/// </summary>
public class ConfigService : IConfigService
{
    private readonly ApplicationDbContext _context;

    public ConfigService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetStringAsync(string typeCode, string key, string defaultValue = "")
    {
        var item = await QueryItemAsync(typeCode, key);
        if (item == null) return defaultValue;

        var value = item.IsSensitive ? AesEncryption.Decrypt(item.Value) : item.Value;
        return value;
    }

    public async Task<int> GetIntAsync(string typeCode, string key, int defaultValue = 0)
    {
        var value = await GetStringAsync(typeCode, key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<bool> GetBoolAsync(string typeCode, string key, bool defaultValue = false)
    {
        var value = await GetStringAsync(typeCode, key);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<double> GetDoubleAsync(string typeCode, string key, double defaultValue = 0)
    {
        var value = await GetStringAsync(typeCode, key);
        return double.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// 按 typeCode + key 查询字典项
    /// </summary>
    private async Task<DictItem?> QueryItemAsync(string typeCode, string key)
    {
        return await _context.DictItems
            .Include(i => i.Type)
            .FirstOrDefaultAsync(i =>
                i.Type.Code == typeCode &&
                i.Key == key &&
                i.IsEnabled &&
                i.Type.IsEnabled);
    }
}
