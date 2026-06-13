using System.Diagnostics;

namespace TPSSO.Auth.Middleware;

/// <summary>
/// 请求日志中间件：为每个请求生成唯一 CorrelationId，记录请求耗时和状态码
/// 便于在日志中通过 CorrelationId 追踪完整请求链路
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 生成或复用 CorrelationId
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N")[..16];
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Append("X-Correlation-Id", correlationId);

        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response.StatusCode;
            var level = statusCode >= 500 ? LogLevel.Error
                      : statusCode >= 400 ? LogLevel.Warning
                      : LogLevel.Information;

            _logger.Log(level, "[{CorrelationId}] {Method} {Path}{Query} → {StatusCode} ({ElapsedMs}ms)",
                correlationId, method, path, queryString, statusCode, sw.ElapsedMilliseconds);
        }
    }
}
