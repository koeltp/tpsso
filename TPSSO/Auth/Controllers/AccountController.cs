using Microsoft.AspNetCore.Mvc;
using Taipi.Core.RQRS;
using TPSSO.Application.Interfaces;
using TPSSO.Application.Models;

namespace TPSSO.Auth.Controllers;

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

    /// <summary>
    /// 用户登录（Cookie 认证）
    /// </summary>
    [HttpPost("login")]
    public async Task<ResponseResult<UserInfoResult>> Login([FromBody] LoginModel model)
    {
        return await _accountService.LoginAsync(model);
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    [HttpPost("logout")]
    public async Task<ResponseResult<bool>> Logout()
    {
        return await _accountService.LogoutAsync();
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ResponseResult<UserInfoResult>> Me()
    {
        return await _accountService.GetCurrentUserAsync(User);
    }
}
