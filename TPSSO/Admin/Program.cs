using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Options;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using TPSSO.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 注册 UploadOptions
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));

// 1. 数据库配置 - MySQL（与 Auth 共享同一数据库）
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Redis 缓存
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mysql =>
    {
        mysql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
    options.UseOpenIddict();
});

// 2. Identity 配置（仅用于 UserManager，不启用 Cookie 登录）
builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 统一声明类型映射，让 UserManager.GetUserAsync() 能从 OpenIddict Token 的 sub claim 找到用户
builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
    options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
    options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
});

// 禁用 Cookie 自动重定向，API 统一返回 401
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = null;
});

// 覆盖 AddIdentity 设置的默认认证方案，确保 Bearer Token 验证优先于 Cookie
// 否则 401 时 Cookie handler 会重定向到 /Account/Login
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

// 3. OpenIddict Validation（作为第三方资源服务器，通过 OIDC Discovery 远程验证 Token）
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        // 业务需要：管理 Client 数据（IOpenIddictApplicationManager）
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddValidation(options =>
    {
        // 通过 OIDC Discovery 远程获取 Auth 服务的公钥和配置
        options.UseSystemNetHttp();
        // 设置 Auth 服务的 Issuer，OpenIddict 会自动访问 {Issuer}/.well-known/openid-configuration
        Console.WriteLine("========================Auth:Issue========================================================================");
        Console.WriteLine(builder.Configuration["Auth:Issuer"]);
        options.SetIssuer(new Uri(builder.Configuration["Auth:Issuer"]!));
        options.UseAspNetCore();
    });

// 开发环境：Auth 使用自签名 HTTPS 证书，需要跳过 SSL 验证才能访问 Discovery 端点
if (builder.Environment.IsDevelopment())
{
    builder.Services.ConfigureAll<HttpClientFactoryOptions>(options =>
    {
        options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
        {
            handlerBuilder.PrimaryHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        });
    });
}

// 4. CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 5. 授权策略：只接受 OpenIddict 验证的 Bearer Token
// builder.Services.AddAuthorization(options =>
// {
//     options.DefaultPolicy = new AuthorizationPolicyBuilder()
//         .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
//         .RequireAuthenticatedUser()
//         .Build();
// });


builder.Services.AddAuthorizationBuilder()
.SetDefaultPolicy(new AuthorizationPolicyBuilder()
.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
.RequireAuthenticatedUser()
.Build());

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 注册服务
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDictService, DictService>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddScoped<IEmailService,EmailService>();

var app = builder.Build();

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
