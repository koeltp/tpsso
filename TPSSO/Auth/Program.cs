using Serilog;
using TPSSO.Auth.Extensions;
using TPSSO.Auth.Middleware;
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
    builder.Services.AddApplicationServices();
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddHttpsRedirection(x => x.HttpsPort = 443);

    var app = builder.Build();

    // 中间件管道
    app.UseExceptionHandling();
    app.UseRequestLogging();
    app.UseForwardedHeadersConfiguration();

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
    app.MapHealthCheckEndpoints();

    // 初始化种子数据 + 启动配置校验
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<ClientSeeder>();
        await seeder.SeedAsync();

        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await StartupConfigValidator.ValidateAsync(app.Services, startupLogger);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TPSSO.Auth 服务异常终止");
}
finally
{
    Log.CloseAndFlush();
}
