using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelGuide.Web.Data;
using TravelGuide.Web.Models;

namespace TravelGuide.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        //  LOGIN
        // ============================================================
        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, message = "Email và mật khẩu không được để trống." });
            }

            var hash = TravelGuide.Web.Models.User.HashPassword(request.Password);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.PasswordHash == hash);

            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Tài khoản hoặc mật khẩu không đúng." });
            }

            return Ok(new
            {
                success = true,
                email = user.Email,
                role = user.Role
            });
        }

        // ============================================================
        //  REGISTER
        // ============================================================
        public class RegisterRequest
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validate rỗng
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Phone) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, message = "Vui lòng điền đầy đủ thông tin." });
            }

            // Validate độ dài mật khẩu
            if (request.Password.Length < 6)
            {
                return BadRequest(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự." });
            }

            // Kiểm tra email đã tồn tại chưa
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email);

            if (emailExists)
            {
                return Conflict(new { success = false, message = "Email này đã được sử dụng." });
            }

            // Tạo user mới — role mặc định là "User"
            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                PasswordHash = TravelGuide.Web.Models.User.HashPassword(request.Password),
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Tài khoản đã được tạo thành công.",
                email = newUser.Email,
                role = newUser.Role
            });
        }
    }
}