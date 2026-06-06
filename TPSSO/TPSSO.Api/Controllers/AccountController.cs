using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TPSSO.Api.Models;
using TPSSO.Api.Services;

namespace TPSSO.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class AccountController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly VerificationCodeService _verificationCodeService;
        private readonly EmailService _emailService;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            VerificationCodeService verificationCodeService,
            EmailService emailService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _verificationCodeService = verificationCodeService;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginMdoel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { error = "无效的用户或密码" });

            await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
            return Ok(new { success = true });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok();
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            return Ok(new
            {
                username = user.UserName,
                email = user.Email,
                avatarUrl = ""
            });
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
                var code = await _verificationCodeService.GenerateAsync(model.Email, model.Purpose);
                await _emailService.SendVerificationCodeAsync(model.Email, code, 10);
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

            // 校验验证码
            var isValid = await _verificationCodeService.VerifyAsync(model.Email, model.Code, purpose: 0);
            if (!isValid)
                return BadRequest(new { error = "验证码无效或已过期" });

            // 创建用户
            var user = new IdentityUser
            {
                UserName = model.Username,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { error = string.Join("；", errors) });
            }

            return Ok(new { success = true, message = "注册成功" });
        }
    }

    public class LoginMdoel
    {
        public string Username { get; set; }=default!;
        public string Password { get; set; }= default!;
        public bool RememberMe { get; set; }=false;

    }
}
