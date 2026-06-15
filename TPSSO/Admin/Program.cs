using Serilog;
using Taipi.Core.Extensions;
using TPSSO.Admin.Extensions;
using TPSSO.Application.Models;
using TPSSO.Application.Exceptions;

// Serilog 引导日志：在 Host 构建前初始化，确保启动阶段的日志也能写入
SerilogExtensions.CreateBootstrapLogger();

try
{
    Log.Information("正在启动 TPSSO.Admin 服务...");

    var builder = WebApplication.CreateBuilder(args);

    // 日志
    builder.Host.UseSerilogFromConfiguration();

    // 服务注册
    builder.Services.AddUploadOptions(builder.Configuration);
    builder.Services.AddDatabase(builder.Configuration);
    builder.Services.AddIdentityConfiguration();
    builder.Services.AddOpenIddictValidation(builder.Configuration, builder.Environment);
    builder.Services.AddCorsPolicy();
    builder.Services.AddAuthorizationPolicy();
    builder.Services.AddApplicationServices();
    builder.Services.AddTaiPiExceptionHandling(options =>
    {
        options.UnauthorizedCode = AppCodes.SystemUnauthorized;
        options.UnauthorizedMessage = "未授权，请先登录";
        options.BadRequestCode = AppCodes.SystemBadRequest;
        options.NotFoundCode = AppCodes.SystemNotFound;
        options.NotFoundMessage = "请求的资源不存在";
        options.UnknownErrorCode = AppCodes.SystemError;
        options.LogException = true;
    });
    builder.Services.AddTaiPiRequestLogging(options =>
    {
        options.LogRequestBodyEnabled = builder.Environment.IsDevelopment();
        options.LogResponseBodyForErrorDetection = true;
        options.SuccessCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "0" };
        options.BusinessErrorLogLevel = LogLevel.Warning;
        options.SensitiveFields = ["password", "token", "secret", "authorization", "apiKey", "clientSecret"];
    });
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new DateTimeUtcConverter());
        });
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // 中间件管道
    app.UseTaiPiExceptionHandling();
    app.UseCorrelationId();
    app.UseTaiPiRequestLogging();
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "TPSSO.Admin 服务异常终止");
}
finally
{
    Log.CloseAndFlush();
}
