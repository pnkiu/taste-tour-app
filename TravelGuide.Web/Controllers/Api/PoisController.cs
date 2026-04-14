using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TravelGuide.Web.Data; // Đổi lại namespace nếu của bạn khác

namespace TravelGuide.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoisController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PoisController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Pois
        [HttpGet]
        public async Task<IActionResult> GetAllPois()
        {
            // Trích xuất dữ liệu thô gọn nhẹ nhất để App Mobile tải cho lẹ
            var pois = await _context.POIs
                .Select(p => new {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Latitude,
                    p.Longitude,
                    p.Radius,
                    p.ImageUrl,
                    // Kéo theo link Audio của địa điểm này (nếu có)
                    AudioUrl = _context.Audios.Where(a => a.PoiId == p.Id).Select(a => a.FileUrl).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(pois);
        }
    }
}