# TPSSO 后端项目规范

## 一、项目结构（Clean Architecture）

```
TPSSO/
├── TPSSO.Domain/              # 最内层：纯领域实体，零外部依赖
│   └── Entities/              # 实体类（如 VerificationCode）
│
├── TPSSO.Application/         # 第二层：接口、DTO、配置选项
│   ├── Interfaces/            # 服务接口（IAccountService、IEmailService 等）
│   ├── Models/                # 请求/响应 DTO（LoginModel、RegisterModel 等）
│   └── Options/               # 配置选项类（SmtpOptions、SsoOptions）
│
├── TPSSO.Infrastructure/      # 第三层：接口实现、数据库、外部服务
│   ├── Data/                  # DbContext + Migrations
│   ├── Seeding/               # 种子数据（ClientSeeder）
│   └── Services/              # 服务实现（AccountService、EmailService 等）
│
└── TPSSO.Api/                 # 最外层：HTTP 入口
    ├── Controllers/           # 控制器（AccountController、AuthorizationController）
    └── Program.cs             # 启动配置、DI 注册
```

## 二、依赖方向规则

依赖只能**从外向内**，内层不知道外层的存在：

```
Api → Application, Infrastructure
Infrastructure → Application
Application → Domain
Domain → 无（零外部依赖）
```

**禁止**：
- Domain 引用任何 NuGet 包（除 NET.Sdk）
- Application 引用 Infrastructure 或 Api
- Infrastructure 引用 Api
- 内层接口的返回类型/参数引用外层类型

## 三、各层职责与放置规则

### Domain 层（TPSSO.Domain）
| 放什么 | 示例 |
|--------|------|
| 领域实体（含行为） | `VerificationCode` |
| 枚举（如用途类型） | `VerificationPurpose` |

**规则**：
- 实体包含行为方法，封装业务规则（如 `IsExpired()`、`IsValid()`、`MarkAsUsed()`）
- 属性用 `public set`，不过度封装
- 不允许引用任何外部 NuGet 包
- 命名空间：`TPSSO.Domain.Entities`

### Application 层（TPSSO.Application）
| 放什么 | 示例 |
|--------|------|
| 服务接口 | `IAccountService`、`IEmailService` |
| 请求/响应 Model | `LoginModel`、`RegisterModel`、`UserInfoResult` |
| 配置选项类 | `SmtpOptions`、`SsoOptions` |

**规则**：
- 接口方法接收 Model 对象，不拆成单个参数
- Options 类必须定义 `const string SectionName` 用于 DI 注册
- 返回类型用 Result 模式：成功返回数据/null，失败返回错误信息字符串
- 命名空间：`TPSSO.Application.Interfaces`、`TPSSO.Application.Models`、`TPSSO.Application.Options`

### Infrastructure 层（TPSSO.Infrastructure）
| 放什么 | 示例 |
|--------|------|
| 服务实现 | `AccountService : IAccountService` |
| DbContext | `ApplicationDbContext` |
| 数据库迁移 | `Data/Migrations/` |
| 种子数据 | `ClientSeeder` |
| 外部服务集成 | `EmailService`（MailKit） |

**规则**：
- 每个接口实现必须放在 `Services/` 目录
- DbContext 放在 `Data/` 目录
- 迁移命令：`dotnet ef migrations add <名称> --project TPSSO.Infrastructure --startup-project TPSSO.Api --output-dir Data\Migrations`
- 命名空间：`TPSSO.Infrastructure.Services`、`TPSSO.Infrastructure.Data`、`TPSSO.Infrastructure.Seeding`

### Api 层（TPSSO.Api）
| 放什么 | 示例 |
|--------|------|
| Controller | `AccountController`、`AuthorizationController` |
| 启动配置 | `Program.cs` |
| 应用配置 | `appsettings.json`、`appsettings.Development.json` |

**规则**：
- Controller 只做 HTTP 层的事：ModelState 校验 → 调用 Service → 返回 HTTP 响应
- 业务逻辑全部在 Service 中，Controller 不直接操作 UserManager/SignInManager（AuthorizationController 除外）
- AuthorizationController 因 OpenIddict 协议深度绑定 HttpContext，逻辑直接写在 Controller 中
- 路由约定：业务 API 用 `api/[controller]`，OIDC 端点用 `connect/` 前缀

## 四、DI 注册规则

在 `Program.cs` 中统一注册，按以下顺序：

```csharp
// 1. Options 配置
builder.Services.Configure<SsoOptions>(builder.Configuration.GetSection(SsoOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));

// 2. 应用服务（接口在 Application，实现在 Infrastructure）
builder.Services.AddScoped<ClientSeeder>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddScoped<IAccountService, AccountService>();
```

**规则**：
- 新增服务必须同时创建接口（Application）和实现（Infrastructure）
- Options 用 `Configure<T>` 注册，必须有 `SectionName` 常量
- 种子数据在 `app.Run()` 之前执行

## 五、Controller 编码规范

### AccountController（业务 API）
```csharp
[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var success = await _accountService.LoginAsync(model);
        if (!success)
            return Unauthorized(new { error = "无效的用户或密码" });

        return Ok(new { success = true });
    }
}
```

**要点**：
- 只注入 Service 接口，不直接注入 UserManager/SignInManager
- 请求参数用 `[FromBody] Model`
- 响应用匿名对象或 UserInfoResult 等 Application 层定义的类型
- 错误返回格式：`new { error = "错误信息" }`

### AuthorizationController（OIDC 协议）
- 路由前缀 `connect/`（不是 `api/`）
- 直接注入 OpenIddict Manager 和 UserManager/SignInManager
- 不抽 Service（协议逻辑深度依赖 HttpContext）

## 六、数据库规范

### DbContext
- 继承 `IdentityDbContext<IdentityUser>`
- `OnModelCreating` 中配置：
  - `builder.UseOpenIddict()` 必须
  - 字符串字段必须设 `HasMaxLength`（MySQL 索引长度限制）
  - OpenIddict 实体字段也要限制长度
  - 自定义实体用 `entity.ToTable("表名")` 指定表名

### 实体
- 放在 Domain 层 `Entities/` 目录
- 字符串属性用 `= string.Empty!` 初始化
- 必须有中文 XML 注释

### 迁移
```bash
# 添加迁移
dotnet ef migrations add <名称> --project TPSSO.Infrastructure --startup-project TPSSO.Api --output-dir Data\Migrations

# 移除迁移
dotnet ef migrations remove --project TPSSO.Infrastructure --startup-project TPSSO.Api
```

## 七、配置文件规范

### appsettings.json 结构
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "SsoOptions": {
    "LoginBaseUrl": "http://localhost:3008",
    "LoginPath": "/login",
    "ConsentPath": "/authorize"
  },
  "Smtp": {
    "Host": "smtp.qiye.aliyun.com",
    "Port": 587,
    "Username": "...",
    "Password": "",
    "FromEmail": "...",
    "FromName": "TPSSO",
    "UseSsl": true
  }
}
```

- 生产配置放 `docker/config/tpssoapi/appsettings.Production.json`
- Options 的 SectionName 必须与 JSON 键名一致

## 八、代码卫生规则

1. **每次改动后主动清理**：删除空目录、未使用的文件、过时的文档
2. **不创建无用文件**：`.http` 测试文件、`.csproj.user`、过时 README 不入库
3. **中文注释**：所有公开类/方法/属性必须有中文 XML 注释，说明"为什么"而非"做什么"
4. **命名空间与目录一致**：文件放在什么目录，命名空间就是什么
5. **新增文件前检查目录**：遵循现有目录结构，不随意创建新目录
