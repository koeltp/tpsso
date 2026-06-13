using Serilog;
using TPSSO.Admin.Extensions;
using TPSSO.Admin.Middleware;

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
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // 中间件管道
    app.UseExceptionHandling();
    app.UseRequestLogging();

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
catch (Exception ex)
{
    Log.Fatal(ex, "TPSSO.Admin 服务异常终止");
}
finally
{
    Log.CloseAndFlush();
}
