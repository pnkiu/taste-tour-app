using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TravelGuide.Web.Controllers
{
    public class AccountController : Controller
    {
        // Hiện trang Đăng nhập
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập rồi thì cho vô thẳng trang chủ
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Tours");
            return View();
        }

        // Xử lý khi bấm nút Đăng nhập
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Kiểm tra mật khẩu (Vũ có thể đổi tùy ý)
            if (username == "admin" && password == "123456")
            {
                // Tạo giấy thông hành (Cookie)
                var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // Đăng nhập thành công -> Về trang Quản lý Tour
                return RedirectToAction("Index", "Tours");
            }

            // Sai mật khẩu
            ViewBag.Error = "Tài khoản hoặc mật khẩu không đúng!";
            return View();
        }

        // Đăng xuất
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}