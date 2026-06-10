using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using TPSSO.Api.Filters;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Options;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using TPSSO.Infrastructure.Seeding;
using TPSSO.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 注册 SsoOptions 到 DI 容器
builder.Services.Configure<SsoOptions>(builder.Configuration.GetSection(SsoOptions.SectionName));

// 注册 JwtOptions
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

// 1. 数据库配置 - MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mysql =>
    {
        mysql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
    options.UseOpenIddict();
});

// 2. Identity 配置
builder.Services.AddIdentity<User, Role>()
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
// 本项目为 Web API，不应自动重定向到登录页。
// 将 LoginPath 设为 null 可阻止 Cookie 中间件返回 302 重定向，
// 使未认证请求直接返回 401 Unauthorized，便于客户端（如 SPA）统一处理。
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
// TODO 待完善配置
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

        // 开发环境使用自签名证书，生产环境必须配置正式证书
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }
        else
        {
            // 生产环境：从文件加载证书（需配置证书路径）
            // 示例：options.AddEncryptionCertificate(File.ReadAllBytes("encryption.pfx"));
            //       options.AddSigningCertificate(File.ReadAllBytes("signing.pfx"));
            throw new InvalidOperationException("生产环境必须配置 OpenIddict 正式加密和签名证书，请参见 appsettings 配置。");
        }

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

builder.Services.AddAuthentication("SmartScheme")
    .AddPolicyScheme("SmartScheme", "SmartScheme", options =>
    {
        // 根据 Authorization 头自动选择认证方案
        // JWT Token（三段式）走 JwtBearer，其他（OAuth Token / Cookie）走 OpenIddict
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (token.Split('.').Length == 3)
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }
            }
            return OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        };
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    })
    .AddGitHub(options =>
    {
        // 占位值，PostConfigure 从数据库动态覆盖
        options.ClientId = "placeholder";
        options.ClientSecret = "placeholder";
        options.CallbackPath = "/api/external/github/callback";
        options.Scope.Add("user:email");
    });

// 从数据库动态配置 GitHub OAuth 的 ClientId/ClientSecret，配置管理页面修改后即时生效
builder.Services.PostConfigure<AspNet.Security.OAuth.GitHub.GitHubAuthenticationOptions>(options =>
{
    using var scope = builder.Services.BuildServiceProvider().CreateScope();
    var configService = scope.ServiceProvider.GetRequiredService<IConfigService>();
    var clientId = configService.GetStringAsync("GitHub", "ClientId").GetAwaiter().GetResult();
    var clientSecret = configService.GetStringAsync("GitHub", "ClientSecret").GetAwaiter().GetResult();

    if (!string.IsNullOrEmpty(clientId))
        options.ClientId = clientId;
    if (!string.IsNullOrEmpty(clientSecret))
        options.ClientSecret = clientSecret;
});
builder.Services.AddAuthorization(options =>
{
    // 全局默认策略：接受 OAuth Bearer Token、JWT Bearer Token 和 Cookie 认证
    // tpssoadmin 通过 OAuth Token 访问，tpssoweb 通过 JWT Token 访问，OAuth 授权流程通过 Cookie
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
            JwtBearerDefaults.AuthenticationScheme,
            IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ModelStateFilter>();
})
.AddJsonOptions(options =>
{
    // 枚举用字符串序列化/反序列化，前端传 "Pending" 而非 1
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddOpenApi();

// 注册应用服务（接口在 Application，实现在 Infrastructure）
builder.Services.AddScoped<ClientSeeder>();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDictService, DictService>();
builder.Services.AddScoped<IConfigService, ConfigService>();

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

// 初始化种子数据
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<ClientSeeder>();
    await seeder.SeedAsync();
}

app.Run();
