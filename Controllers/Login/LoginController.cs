using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewAppErp.Models;
using NewAppErp.Models.Login;
using NewAppErp.Services.Login;
namespace NewAppErp.Controllers.Login
{
    [AllowAnonymous]
    public class LoginController : Controller
    {

        public readonly ILoginService _loginService;

        public LoginController(ILoginService loginservice)
        {
            _loginService = loginservice;
        }
        public IActionResult Index()
        {
            return View(new Account());
        }

        [HttpPost]
        public async Task<IActionResult> checkedlogin(Account user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }
            var sid = await _loginService.checkedlogin(user);

            if (sid != null)
            {
                // Stocker le SID dans les cookies et la session
                Response.Cookies.Append("sid", sid, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax
                });

                HttpContext.Session.SetString("AuthToken", sid);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.usr),
                };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");

                await HttpContext.SignInAsync(
                    "CookieAuth",
                    new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("SalaireChart", "Salary");
            }

            ModelState.AddModelError(string.Empty, "Identifiants incorrects");
            return View("Index", user);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            HttpContext.Session.Remove("AuthToken");
            return RedirectToAction("Index","Login");
        }
    }
}