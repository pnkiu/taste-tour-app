using Microsoft.EntityFrameworkCore;
using TravelGuide.Web.Models; // Đổi lại đúng với tên namespace của bạn nếu cần

namespace TravelGuide.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Bảng POIs trong cơ sở dữ liệu
        public DbSet<POI> POIs { get; set; }
        public DbSet<Audio> Audios { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<TourPOI> TourPOIs { get; set; }
        public DbSet<User> Users { get; set; } // Bảng người dùng

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed tài khoản admin mặc định
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Email = "admin@tastetour.com",
                PasswordHash = User.HashPassword("123456"),
                Role = "Admin"
            });
        }
    }
}