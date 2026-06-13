using System.Net;
using System.Text.Json;

namespace TPSSO.Auth.Middleware;

/// <summary>
/// 全局异常处理中间件：捕获未处理的异常，返回统一的 JSON 响应
/// 避免向客户端暴露 500 状态码和堆栈信息
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理的异常：{Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, error, message) = exception switch
        {
            UnauthorizedAccessException _ => (HttpStatusCode.Unauthorized, "unauthorized", "未授权"),
            ArgumentException _ => (HttpStatusCode.BadRequest, "bad_request", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "internal_error", "服务器内部错误，请稍后重试")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error,
            message
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
