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

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
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
    public async Task<ResponseResult<UserInfoResult>> Me()
    {
        return await _accountService.GetCurrentUserAsync(User);
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
