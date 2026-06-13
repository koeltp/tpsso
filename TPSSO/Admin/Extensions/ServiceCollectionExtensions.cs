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

namespace TPSSO.Admin.Extensions;

/// <summary>
/// 服务注册扩展方法，将 Program.cs 中的服务配置按职责拆分
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 UploadOptions 配置
    /// </summary>
    public static IServiceCollection AddUploadOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<UploadOptions>(configuration.GetSection(UploadOptions.SectionName));
        return services;
    }

    /// <summary>
    /// 注册数据库和缓存
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        });

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mysql =>
            {
                mysql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            options.UseOpenIddict();
        });

        return services;
    }

    /// <summary>
    /// 注册 ASP.NET Core Identity（仅用于 UserManager，不启用 Cookie 登录）
    /// </summary>
    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
    {
        services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // 统一声明类型映射，让 UserManager.GetUserAsync() 能从 OpenIddict Token 的 sub claim 找到用户
        services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
            options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
            options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
        });

        // 禁用 Cookie 自动重定向，API 统一返回 401
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = null;
        });

        return services;
    }

    /// <summary>
    /// 注册 OpenIddict Validation（作为资源服务器，通过 OIDC Discovery 远程验证 Token）
    /// </summary>
    public static IServiceCollection AddOpenIddictValidation(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // 覆盖 AddIdentity 设置的默认认证方案，确保 Bearer Token 验证优先于 Cookie
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddOpenIddict()
            .AddCore(options =>
            {
                // 业务需要：管理 Client 数据（IOpenIddictApplicationManager）
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
            })
            .AddValidation(options =>
            {
                options.UseSystemNetHttp();
                options.SetIssuer(new Uri(configuration["Auth:Issuer"]!));
                options.UseAspNetCore();
            });

        // 开发环境：Auth 使用自签名 HTTPS 证书，需要跳过 SSL 验证才能访问 Discovery 端点
        if (environment.IsDevelopment())
        {
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
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
    /// 注册授权策略：只接受 OpenIddict 验证的 Bearer Token
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicy(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build());
        return services;
    }

    /// <summary>
    /// 注册业务服务
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDictService, DictService>();
        services.AddScoped<IConfigService, ConfigService>();
        services.AddScoped<IVerificationCodeService, VerificationCodeService>();
        services.AddScoped<IEmailService, EmailService>();
        return services;
    }
}
