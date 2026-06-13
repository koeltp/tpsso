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
                options.SetIssuer(ssoOptions.Issuer);
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetUserInfoEndpointUris("/connect/userinfo")
                       .SetEndSessionEndpointUris("/connect/logout");

                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange();

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
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IVerificationCodeService, VerificationCodeService>();
        services.AddScoped<IConfigService, ConfigService>();
        services.AddScoped<ClientSeeder>();
        return services;
    }
}
