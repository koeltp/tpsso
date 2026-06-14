using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Options;
using TPSSO.Domain.Entities;
using TPSSO.Infrastructure.Data;
using TPSSO.Infrastructure.Seeding;
using TPSSO.Infrastructure.Services;

namespace TPSSO.Auth.Extensions;

/// <summary>
/// 服务注册扩展方法，将 Program.cs 中的服务配置按职责拆分
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 SsoOptions 配置
    /// </summary>
    public static IServiceCollection AddSsoOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SsoOptions>(configuration.GetSection(SsoOptions.SectionName));
        return services;
    }

    /// <summary>
    /// 注册数据库和缓存
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mysql =>
            {
                mysql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            options.UseOpenIddict();
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        });

        return services;
    }

    /// <summary>
    /// 注册 ASP.NET Core Identity（Cookie 认证 + Claims 映射）
    /// </summary>
    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
    {
        services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // 统一声明类型映射
        services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
            options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
            options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
        });

        // Cookie 配置：未认证时重定向到前端登录页
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        return services;
    }

    /// <summary>
    /// 注册 OpenIddict Server + Core + Validation
    /// </summary>
    public static IServiceCollection AddOpenIddictServer(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
                options.UseQuartz();
            })
            .AddServer(options =>
            {
                var ssoOptions = configuration.GetSection(SsoOptions.SectionName).Get<SsoOptions>()!;
                // 仅在显式配置时设置 Issuer，否则由 OpenIddict 自动从请求推断
                if (!string.IsNullOrEmpty(ssoOptions.Issuer))
                {
                    options.SetIssuer(ssoOptions.Issuer);
                }

                // 端点
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetUserInfoEndpointUris("/connect/userinfo")
                       .SetEndSessionEndpointUris("/connect/logout")
                       .SetIntrospectionEndpointUris("/connect/introspect")
                       .SetRevocationEndpointUris("/connect/revoke")
                       .SetDeviceAuthorizationEndpointUris("/connect/device")
                       .SetEndUserVerificationEndpointUris("/connect/verify");

                // 授权流程
                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange()
                       .AllowRefreshTokenFlow()
                       .AllowClientCredentialsFlow()
                       .AllowDeviceAuthorizationFlow();

                // Refresh Token 配置：滑动过期，7天无活动则过期
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));
                options.SetAccessTokenLifetime(TimeSpan.FromHours(2));

                // 禁用 Access Token 加密：Access Token 需要被资源服务器（Admin）通过 OIDC Discovery 离线验证
                // 加密后资源服务器无法解密，只能走 Introspection 端点在线验证，增加延迟
                options.DisableAccessTokenEncryption();

                // 启用 JWT Access Token（自包含，资源服务器可离线验证）
                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableEndUserVerificationEndpointPassthrough();

                // 开发环境使用开发证书
                if (environment.IsDevelopment())
                {
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();
                }
                else
                {
                    var certPassword = "xxx";
                    var encryption = X509CertificateLoader.LoadPkcs12FromFile("/app/certbot/encryption.pfx", certPassword);
                    var signing = X509CertificateLoader.LoadPkcs12FromFile("/app/certbot/signing.pfx", certPassword);
                    options.AddEncryptionCertificate(encryption)
                           .AddSigningCertificate(signing);
                }

                // 启用 Access Token 加密（防内容泄露）
                // 注意：启用后资源服务器需要配置解密密钥或使用 Introspection 端点
                // options.DisableAccessTokenEncryption();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }

    /// <summary>
    /// 注册 CORS 策略
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
        return services;
    }

    /// <summary>
    /// 注册健康检查（MySQL + Redis）
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddHealthChecks()
            .AddMySql(connectionString!, name: "mysql", tags: ["db"])
            .AddRedis(configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "redis", tags: ["cache"]);
        return services;
    }

    /// <summary>
    /// 注册业务服务
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IVerificationCodeService, VerificationCodeService>();
        services.AddScoped<IConfigService, ConfigService>();

        // 种子数据服务
        services.AddScoped<ScopeSeeder>();
        services.AddScoped<RoleSeeder>();
        services.AddScoped<UserSeeder>();
        services.AddScoped<ClientSeeder>();
        services.AddScoped<DictSeeder>();
        services.AddScoped<DataSeeder>();

        // 仅开发环境注册测试客户端种子数据
        if (environment.IsDevelopment())
        {
            services.AddScoped<TestClientSeeder>();
        }

        return services;
    }
}
