using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var success = await _accountService.LoginAsync(model);
        if (!success)
            return Unauthorized(new { error = "无效的用户或密码" });

        return Ok(new { success = true });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _accountService.LogoutAsync();
        return Ok();
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userInfo = await _accountService.GetCurrentUserAsync(User);
        if (userInfo == null)
            return Unauthorized();

        return Ok(userInfo);
    }

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    [HttpPost("send-code")]
    public async Task<IActionResult> SendCode([FromBody] SendCodeModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _accountService.SendCodeAsync(model);
            return Ok(new { message = "验证码已发送" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"发送验证码失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var error = await _accountService.RegisterAsync(model);
        if (error != null)
            return BadRequest(new { error });

        return Ok(new { success = true, message = "注册成功" });
    }
}
