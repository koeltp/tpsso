namespace TPSSO.Auth.Middleware;

/// <summary>
/// 中间件注册扩展方法，简化 Program.cs 中的中间件配置
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// 注册全局异常处理中间件
    /// </summary>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    /// <summary>
    /// 注册请求日志中间件（CorrelationId + 耗时 + 状态码）
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
