using System.Text.Json;
using System.Text.Json.Serialization;
using Taipi.Core.RQRS;
using TPSSO.Application.Exceptions;

namespace TPSSO.Auth.Middleware;

/// <summary>
/// 全局异常处理中间件：
/// - AppException（业务异常）→ HTTP 200 + 业务错误码
/// - 框架异常 → HTTP 4xx/5xx + 系统错误码
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(ex, "响应已开始发送，无法写入异常响应：{Message}", ex.Message);
                return;
            }

            _logger.LogError(ex, "未处理的异常：{Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (httpStatusCode, result) = exception switch
        {
            // 业务异常：HTTP 200 + 业务错误码，前端在 .then 中判断 code
            AppException appEx => (StatusCodes.Status200OK,
                StatusResponseResult.Error(appEx.Code, appEx.Message)),

            // 框架异常：HTTP 4xx/5xx + 系统错误码，前端在 axios 拦截器中处理
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,
                StatusResponseResult.Error(AppCodes.SystemUnauthorized, "未授权")),
            ArgumentException => (StatusCodes.Status400BadRequest,
                StatusResponseResult.Error(AppCodes.SystemBadRequest, exception.Message)),
            KeyNotFoundException => (StatusCodes.Status404NotFound,
                StatusResponseResult.Error(AppCodes.SystemNotFound, "资源不存在")),

            // 未知异常：HTTP 500
            _ => (StatusCodes.Status500InternalServerError,
                StatusResponseResult.Error(AppCodes.SystemError, _environment.IsProduction() ? "服务器内部错误" : exception.ToString()))
        };

        // 附加 correlationId 便于前端/日志关联
        result.CorrelationId = context.Items["CorrelationId"]?.ToString();

        context.Response.StatusCode = httpStatusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(result, _jsonOptions));
    }
}
