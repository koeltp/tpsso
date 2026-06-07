using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using TPSSO.Api.Filters;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Options;
using TPSSO.Infrastructure.Data;
using TPSSO.Infrastructure.Seeding;
using TPSSO.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 注册 SsoOptions 到 DI 容器
builder.Services.Configure<SsoOptions>(builder.Configuration.GetSection(SsoOptions.SectionName));

// 1. 数据库配置 - MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    options.UseOpenIddict();
});

// 2. Identity 配置
builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 统一声明类型映射
builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
    options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
    options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
});

// 3. Cookie 配置
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = null; // 禁用自动重定向，API 统一返回 401
});

// 4. CORS 配置
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 5. OpenIddict 核心配置
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
        .UseDbContext<ApplicationDbContext>();
        options.UseQuartz();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserInfoEndpointUris("/connect/userinfo")
               .SetEndSessionEndpointUris("/connect/logout");

        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ModelStateFilter>();
});
builder.Services.AddOpenApi();

// 注册应用服务（接口在 Application，实现在 Infrastructure）
builder.Services.AddScoped<ClientSeeder>();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddScoped<IAccountService, AccountService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 初始化种子数据
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<ClientSeeder>();
    await seeder.SeedAsync();
}

app.Run();
