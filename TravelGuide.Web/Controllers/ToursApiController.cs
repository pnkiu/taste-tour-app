using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelGuide.Web.Data; // Load database
using TravelGuide.Web.Models; // Load các Model như Tour, POI

namespace TravelGuide.Web.Controllers
{
    // Cài đặt đường dẫn cho Phát gọi: localhost:.../api/ToursApi
    [Route("api/[controller]")]
    [ApiController]
    public class ToursApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ToursApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ToursApi
        // Lệnh này giúp Phát lấy TOÀN BỘ danh sách Tour
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tour>>> GetTours()
        {
            // Trả về dữ liệu dạng JSON cho App Mobile
            return await _context.Tours.ToListAsync();
        }

        // GET: api/ToursApi/5
        // Lệnh này giúp Phát lấy thông tin của MỘT Tour cụ thể khi user bấm vào
        [HttpGet("{id}")]
        public async Task<ActionResult<Tour>> GetTour(int id)
        {
            var tour = await _context.Tours.FindAsync(id);

            if (tour == null)
            {
                return NotFound("Không tìm thấy Tour này nha Phát ơi!");
            }

            return tour;
        }
    }
}