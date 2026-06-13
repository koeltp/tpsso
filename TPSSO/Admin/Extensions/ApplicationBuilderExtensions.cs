namespace TPSSO.Admin.Extensions;

/// <summary>
/// 应用构建扩展方法，将 Program.cs 中的中间件管道按职责拆分
/// </summary>
public static class ApplicationBuilderExtensions
{
    // Admin 项目目前没有额外的中间件配置需要抽取
    // 如需添加（如 ForwardedHeaders、HealthCheck 等），在此扩展
}
