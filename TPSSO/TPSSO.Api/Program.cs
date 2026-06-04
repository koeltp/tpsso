using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using TPSSO.Api;


var builder = WebApplication.CreateBuilder(args);

var cookieDomain = builder.Configuration["CookieSettings:Domain"];

// Add services to the container.
// 1. 数据库配置 - MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//var serverVersion = ServerVersion.AutoDetect(connectionString);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    options.UseOpenIddict(); // 重要：注册 OpenIddict 存储
});


// 2. Identity 配置
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


// ===== 添加方案一的配置：统一声明类型 =====
builder.Services.Configure<IdentityOptions>(options =>
{
    // 将 Identity 默认的用户 ID 声明类型，映射为 OpenIddict 标准的 "sub"
    options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;  // 对应 "sub"
    // 可选：同时映射用户名和角色声明
    options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;   // 对应 "name"
    options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;       // 对应 "role"
});

// 3. Cookie 配置（跨域必须）
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // 必须 HTTPS
    options.Cookie.Domain = cookieDomain; //".tpsso.com"; // 子域共享，可选
    options.LoginPath = null; // 禁用自动重定向
});

// 4. CORS 配置（允许登录页跨域）
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLoginOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:3007", "http://localhost:3008") // 添加所有前端源
             .AllowCredentials()               // 允许携带 Cookie
             .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


// 5. OpenIddict 核心配置
builder.Services.AddOpenIddict()
    // 存储配置
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
               //.ReplaceDefaultEntities<int>(); // 使用 int 作为主键类型
        options.UseQuartz(); // 用于清理过期令牌
    })
    // 服务端配置
    .AddServer(options =>
    {
        
        // 端点
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserInfoEndpointUris("/connect/userinfo")
               .SetEndSessionEndpointUris("/connect/logout");

        // 授权码流 + PKCE
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();

        // 开发环境使用临时证书（生产环境必须替换）
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // 集成 ASP.NET Core，启用直通模式（关键）
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();
    })
    // 验证配置（供资源服务器使用）
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<ClientSeeder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowLoginOrigin"); // 应用 CORS 策略 必须在 UseAuthentication 之前
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
//    var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
//    await ClientSeeder.SeedAsync(context,applicationManager,scopeManager); // 初始化客户端数据



//    var result= await applicationManager.FindByClientIdAsync("tpsso_spa_client");
//    Console.WriteLine(result != null ? "客户端数据已初始化" : "客户端数据初始化失败");
//}
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<ClientSeeder>();
    await seeder.SeedAsync();
}
app.Run();
