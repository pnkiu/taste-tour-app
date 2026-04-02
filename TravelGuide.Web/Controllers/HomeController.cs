using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using TravelGuide.Web.Data;
using TravelGuide.Web.Models;

namespace TravelGuide.Web.Controllers
{
    public class HomeController : Controller
    {
        // 1. Khai báo công cụ gọi Database
        private readonly ApplicationDbContext _context;

        // 2. Tiêm Database vào Controller
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 3. Đếm số lượng thực tế trong Database và truyền ra ngoài qua ViewBag
            ViewBag.TotalPOIs = _context.POIs.Count();
            ViewBag.TotalAudios = _context.Audios.Count();
            ViewBag.TotalTours = _context.Tours.Count();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}