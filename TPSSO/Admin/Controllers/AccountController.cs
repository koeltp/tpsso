using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Taipi.Core.RQRS;
using TPSSO.Application.Exceptions;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;
using TPSSO.Application.Options;

namespace TPSSO.Admin.Controllers;

[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IWebHostEnvironment _env;
    private readonly UploadOptions _uploadOptions;

    public AccountController(IAccountService accountService, IWebHostEnvironment env, IOptions<UploadOptions> uploadOptions)
    {
        _accountService = accountService;
        _env = env;
        _uploadOptions = uploadOptions.Value;
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ResponseResult<UserInfoResult>> Me()
    {
        var data = await _accountService.GetCurrentUserAsync(User);
        return new ResponseResult<UserInfoResult>(data);
    }

    /// <summary>
    /// 修改个人信息
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<StatusResponseResult> UpdateProfile([FromBody] UpdateProfileModel model)
    {
        await _accountService.UpdateProfileAsync(User, model);
        return StatusResponseResult.Success("修改成功");
    }

    /// <summary>
    /// 上传头像
    /// </summary>
    [HttpPost("avatar")]
    [Authorize]
    public async Task<StatusResponseResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new BadRequestException(AppCodes.UploadEmpty, "请选择文件");

        var allowedTypes = _uploadOptions.AvatarAllowedTypes.Split(',');
        if (!allowedTypes.Contains(file.ContentType))
        {
            allowedTypes = [.. allowedTypes.Select(x => x.Split('/')[1])];
            throw new BadRequestException(AppCodes.UploadInvalidType, $"仅支持 {string.Join("、", allowedTypes)} 格式");
        }
        if (file.Length > _uploadOptions.AvatarMaxSizeMB * 1024 * 1024)
            throw new BadRequestException(AppCodes.UploadTooLarge, $"文件大小不能超过 {_uploadOptions.AvatarMaxSizeMB}MB");

        var userId = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value ?? "unknown";
        var ext = Path.GetExtension(file.FileName);
        var newFileName = $"{userId}_{Guid.NewGuid():N}{ext}";
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
        await _accountService.UpdateAvatarUrlAsync(User, avatarUrl);
        return StatusResponseResult.Success("上传成功");
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    [HttpPut("password")]
    [Authorize]
    public async Task<StatusResponseResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        await _accountService.ChangePasswordAsync(User, model);
        return StatusResponseResult.Success("密码修改成功");
    }
}
