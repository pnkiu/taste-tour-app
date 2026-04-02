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
    }
}