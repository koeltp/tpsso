using Taipi.Core.RQRS;
using TPSSO.Application.Models;

namespace TPSSO.Application.Interfaces;

/// <summary>
/// 字典配置服务接口
/// </summary>
public interface IDictService
{
    /// <summary>获取所有字典类型（含字典项）</summary>
    Task<List<DictTypeResult>> GetAllAsync();

    /// <summary>创建或更新字典类型</summary>
    Task<DictTypeResult> SaveTypeAsync(DictTypeDto dto);

    /// <summary>删除字典类型</summary>
    Task DeleteTypeAsync(Guid id);

    /// <summary>创建或更新字典项</summary>
    Task<DictItemResult> SaveItemAsync(Guid typeId, DictItemDto dto);

    /// <summary>删除字典项</summary>
    Task DeleteItemAsync(Guid id);
}

/// <summary>
/// 配置读取服务接口（供业务代码调用，按 typeCode + key 读取配置）
/// </summary>
public interface IConfigService
{
    /// <summary>获取配置值（字符串）</summary>
    Task<string> GetStringAsync(string typeCode, string key, string defaultValue = "");

    /// <summary>获取配置值（整数）</summary>
    Task<int> GetIntAsync(string typeCode, string key, int defaultValue = 0);

    /// <summary>获取配置值（布尔值）</summary>
    Task<bool> GetBoolAsync(string typeCode, string key, bool defaultValue = false);

    /// <summary>获取配置值（双精度浮点数）</summary>
    Task<double> GetDoubleAsync(string typeCode, string key, double defaultValue = 0);
}
