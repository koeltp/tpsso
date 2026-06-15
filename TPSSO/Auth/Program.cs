using Serilog;
using Taipi.Core;
using Taipi.Core.Extensions;
using TPSSO.Application.Models;
using TPSSO.Auth.Extensions;
using TPSSO.Auth.Middleware;
using TPSSO.Application.Exceptions;
using TPSSO.Infrastructure.Seeding;

// Serilog 引导日志：在 Host 构建前初始化，确保启动阶段的日志也能写入
SerilogExtensions.CreateBootstrapLogger();

try
{
    Log.Information("正在启动 TPSSO.Auth 服务...");

    var builder = WebApplication.CreateBuilder(args);

    // 日志
    builder.Host.UseSerilogFromConfiguration();

    // 服务注册
    builder.Services.AddSsoOptions(builder.Configuration);
    builder.Services.AddDatabase(builder.Configuration);
    builder.Services.AddIdentityConfiguration();
    builder.Services.AddAuthentication().AddExternalLogins();
    builder.Services.AddOpenIddictServer(builder.Configuration, builder.Environment);
    builder.Services.AddCorsPolicy();
    builder.Services.AddHealthCheckServices(builder.Configuration);
    builder.Services.AddApplicationServices(builder.Environment);
    builder.Services.AddTaiPiExceptionHandling(options =>
    {
        options.UnauthorizedCode = AppCodes.SystemUnauthorized;
        options.UnauthorizedMessage = "未授权，请先登录";
        options.BadRequestCode = AppCodes.SystemBadRequest;
        options.NotFoundCode = AppCodes.SystemNotFound;
        options.NotFoundMessage = "请求的资源不存在";
        options.UnknownErrorCode = AppCodes.SystemError;
        // 请求日志中间件已记录请求信息，避免异常中间件重复记录
        options.LogException = false;
    });
    builder.Services.AddTaiPiRequestLogging(options =>
    {
        // 生产环境不记录请求体，避免敏感信息泄露和日志膨胀
        options.LogRequestBodyEnabled = builder.Environment.IsDevelopment();
        // 响应体业务错误检测：HTTP 200 但 code 非成功码时提升日志级别
        options.LogResponseBodyForErrorDetection = true;
        options.SuccessCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "0" };
        options.BusinessErrorLogLevel = LogLevel.Warning;
        // 脱敏字段
        options.SensitiveFields = ["password", "token", "secret", "authorization", "apiKey", "clientSecret"];
    });
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // EF Core 从数据库读取的 DateTime.Kind 为 Unspecified，序列化时不会带 Z 后缀
            // 配置为将 Unspecified 的 DateTime 视为 UTC，确保前端能正确转为本地时间
            options.JsonSerializerOptions.Converters.Add(new DateTimeUtcConverter());
        });
    builder.Services.AddOpenApi();
    // 速率限制
    builder.Services.AddTaiPiRateLimiting();
    //当环境变量不是 "true" 时添加（包括未设置或为 "false"）
    if (Environment.GetEnvironmentVariable("DOTNET_HOSTBUILDER__DESIGN_TIME") != "true")
    {
        builder.Services.AddHttpsRedirection(x => x.HttpsPort = 443);
    }

    var app = builder.Build();

    // 中间件管道
    app.UseTaiPiExceptionHandling();
    app.UseCorrelationId();
    app.UseTaiPiRequestLogging();
    app.UseForwardedHeadersConfiguration();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthCheckEndpoints();

    // 初始化种子数据 + 启动配置校验
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync(app.Environment.IsDevelopment());

        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await StartupConfigValidator.ValidateAsync(app.Services, startupLogger);
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "TPSSO.Auth 服务异常终止");
}
finally
{
    Log.CloseAndFlush();
}
