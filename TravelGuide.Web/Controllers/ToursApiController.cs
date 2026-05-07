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
        // Lấy TOÀN BỘ danh sách Tour
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tour>>> GetTours()
        {
            return await _context.Tours.ToListAsync();
        }

        // GET: api/ToursApi/5
        // Lấy thông tin của MỘT Tour cụ thể
        [HttpGet("{id}")]
        public async Task<ActionResult<Tour>> GetTour(int id)
        {
            var tour = await _context.Tours.FindAsync(id);

            if (tour == null)
            {
                return NotFound("Không tìm thấy Tour này!");
            }

            return tour;
        }

        // GET: api/ToursApi/5/pois
        // Lấy danh sách POI theo thứ tự lộ trình của một Tour
        // Response shape khớp với QuanAn model của MAUI app
        [HttpGet("{id}/pois")]
        public async Task<IActionResult> GetTourPois(int id)
        {
            var tourExists = await _context.Tours.AnyAsync(t => t.Id == id);
            if (!tourExists) return NotFound("Tour không tồn tại.");

            var pois = await _context.TourPOIs
                .Include(tp => tp.POI)
                .Where(tp => tp.TourId == id)
                .OrderBy(tp => tp.OrderIndex)
                .Select(tp => new
                {
                    id          = tp.POI!.Id.ToString(),
                    name        = tp.POI.Name,
                    description = tp.POI.Description,
                    descriptionEn = tp.POI.DescriptionEn,
                    latitude    = tp.POI.Latitude,
                    longitude   = tp.POI.Longitude,
                    imageUrl    = tp.POI.ImageUrl,
                    audioContent   = tp.POI.AudioContent,
                    audioContentEn = tp.POI.AudioContentEn,
                    radius      = tp.POI.Radius,
                    priority    = tp.POI.Priority,
                    orderIndex  = tp.OrderIndex
                })
                .ToListAsync();

            return Ok(pois);
        }
    }
}