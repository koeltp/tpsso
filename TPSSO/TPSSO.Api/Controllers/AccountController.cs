using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;

namespace TPSSO.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IWebHostEnvironment _env;

    public AccountController(IAccountService accountService, IWebHostEnvironment env)
    {
        _accountService = accountService;
        _env = env;
    }

    [HttpPost("login")]
    public async Task<ResponseResult<bool>> Login([FromBody] LoginModel model)
    {
        return await _accountService.LoginAsync(model);
    }

    [HttpPost("logout")]
    public async Task<ResponseResult<bool>> Logout()
    {
        return await _accountService.LogoutAsync();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ResponseResult<UserInfoResult>> Me()
    {
        return await _accountService.GetCurrentUserAsync(User);
    }

    /// <summary>
    /// 修改个人信息
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ResponseResult<bool>> UpdateProfile([FromBody] UpdateProfileModel model)
    {
        return await _accountService.UpdateProfileAsync(User, model);
    }

    /// <summary>
    /// 上传头像
    /// </summary>
    [HttpPost("avatar")]
    [Authorize]
    [RequestSizeLimit(2 * 1024 * 1024)] // 限制 2MB
    public async Task<ResponseResult<string>> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return ResponseResult<string>.BadRequest("请选择文件");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return ResponseResult<string>.BadRequest("仅支持 JPG/PNG/GIF/WebP 格式");

        // 获取当前用户 ID 用于生成文件名
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var ext = Path.GetExtension(file.FileName);
        var newFileName = $"{userId}_{Guid.NewGuid():N}{ext}";
        // WebRootPath 在 wwwroot 目录不存在时为 null，需确保目录存在
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var avatarDir = Path.Combine(webRoot, "avatars");

        if (!Directory.Exists(avatarDir))
            Directory.CreateDirectory(avatarDir);

        var filePath = Path.Combine(avatarDir, newFileName);
        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fs);
        }

        var avatarUrl = $"/avatars/{newFileName}";
        return await _accountService.UpdateAvatarUrlAsync(User, avatarUrl);
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    [HttpPut("password")]
    [Authorize]
    public async Task<ResponseResult<bool>> ChangePassword([FromBody] ChangePasswordModel model)
    {
        return await _accountService.ChangePasswordAsync(User, model);
    }

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    [HttpPost("send-code")]
    public async Task<ResponseResult<bool>> SendCode([FromBody] SendCodeModel model)
    {
        return await _accountService.SendCodeAsync(model);
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    public async Task<ResponseResult<bool>> Register([FromBody] RegisterModel model)
    {
        return await _accountService.RegisterAsync(model);
    }
}
