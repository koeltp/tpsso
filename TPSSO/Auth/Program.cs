using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Options;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using TPSSO.Infrastructure.Seeding;
using TPSSO.Infrastructure.Services;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 注册 SsoOptions
builder.Services.Configure<SsoOptions>(builder.Configuration.GetSection(SsoOptions.SectionName));

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

// Redis 缓存
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
});
builder.Services.AddHttpsRedirection(x=>{
    x.HttpsPort=443;
});
// 2. Identity 配置（Cookie 认证）
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

// Cookie 配置：未认证时重定向到前端登录页
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.Cookie.Domain = ".taipi.top";
    options.Cookie.SameSite=SameSiteMode.Lax;
    options.Cookie.SecurePolicy=CookieSecurePolicy.Always;
});

// 3. OpenIddict 完整配置（Server + Core + Validation）
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
        options.UseQuartz();
    })
    .AddServer(options =>
    {
        options.SetIssuer("https://authapi.taipi.top");
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserInfoEndpointUris("/connect/userinfo")
               .SetEndSessionEndpointUris("/connect/logout");

        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();

        // 开发环境使用自签名证书
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }
        else
        {
            // app/certbot 目录下有加密和签名证书文件 通过Docker挂载到容器中
            var encryptionPath = "/app/certbot/encryption.pfx";
            var signingPath = "/app/certbot/signing.pfx";
            var certPassword = "xxx";

            var encryption = X509CertificateLoader.LoadPkcs12FromFile(encryptionPath, certPassword);
            var signing = X509CertificateLoader.LoadPkcs12FromFile(signingPath, certPassword);
            options.AddEncryptionCertificate(encryption)
                   .AddSigningCertificate(signing);
        }

        // Access Token 只签名不加密，允许第三方资源服务器通过 JWKS 公钥验证
        // Identity Token 仍然加密保护用户隐私
        options.DisableAccessTokenEncryption();

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

// 4. CORS：tpssoauth 同源，其他客户端允许跨域
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 注册服务
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<ClientSeeder>();

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks =
    {
        new IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12),
        new IPNetwork(System.Net.IPAddress.Parse("192.168.0.0"), 16),
        new IPNetwork(System.Net.IPAddress.Parse("127.0.0.0"), 8)
    }
});
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
