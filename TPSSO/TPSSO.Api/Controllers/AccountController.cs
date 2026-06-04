using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TPSSO.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken] // 简化跨域 CSRF，生产环境需强化
    public class AccountController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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
    }

    public class LoginMdoel
    {
        public string Username { get; set; }=default!;
        public string Password { get; set; }= default!;
        public bool RememberMe { get; set; }=false;

    }
}
