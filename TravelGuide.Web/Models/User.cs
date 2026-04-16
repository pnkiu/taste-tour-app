using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace TravelGuide.Web.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        public string? FullName { get; set; } // Thêm dấu ? để cho phép rỗng (nếu cần)
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string Role { get; set; } = "User"; // "Admin" hoặc "User"

        // Helper tĩnh để dùng chung khi cần băm mật khẩu
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToHexString(hash).ToLower();
            }
        }
    }
}
