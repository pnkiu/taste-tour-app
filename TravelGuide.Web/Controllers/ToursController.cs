using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelGuide.Web.Data;
using TravelGuide.Web.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace TravelGuide.Web.Controllers
{
    public class ToursController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ToursController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Tours
        public async Task<IActionResult> Index()
        {
            //return View(await _context.Tours.ToListAsync());
            var tours = await _context.Tours.ToListAsync();

            var poiCounts = await _context.TourPOIs
                .GroupBy(tp => tp.TourId)
                .Select(g => new
                {
                    TourId = g.Key,
                    Count = g.Count() * 2 
                })
                .ToDictionaryAsync(x => x.TourId, x => x.Count);

            ViewBag.PoiCounts = poiCounts;

            return View(tours);
        }

        // GET: Tours/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var tour = await _context.Tours.FirstOrDefaultAsync(m => m.Id == id);
            if (tour == null) return NotFound();

            return View(tour);
        }

        // GET: Tours/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tours/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Duration,Distance,ImageUpload")] Tour tour)
        {
            if (ModelState.IsValid)
            {
                if (tour.ImageUpload != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tours");
                    if (!Directory.Exists(uploadsFolder)) { Directory.CreateDirectory(uploadsFolder); }
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + tour.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await tour.ImageUpload.CopyToAsync(fileStream);
                    }
                    tour.ImageUrl = "/uploads/tours/" + uniqueFileName;
                }

                _context.Add(tour);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tour);
        }

        // GET: Tours/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            return View(tour);
        }

        // POST: Tours/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Duration,Distance,ImageUrl")] Tour tour)
        {
            if (id != tour.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tour);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TourExists(tour.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tour);
        }

        // GET: Tours/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var tour = await _context.Tours.FirstOrDefaultAsync(m => m.Id == id);
            if (tour == null) return NotFound();

            return View(tour);
        }

        // POST: Tours/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour != null) _context.Tours.Remove(tour);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TourExists(int id)
        {
            return _context.Tours.Any(e => e.Id == id);
        }

        // ==========================================================
        // --- KHU VỰC XỬ LÝ LỘ TRÌNH (TOUR POI) ---
        // ==========================================================

        public async Task<IActionResult> ManageRoute(int? id)
        {
            if (id == null) return NotFound();
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            ViewBag.CurrentPOIs = await _context.TourPOIs
                .Include(tp => tp.POI)
                .Where(tp => tp.TourId == id)
                .OrderBy(tp => tp.OrderIndex)
                .ToListAsync();

            var currentPoiIds = await _context.TourPOIs.Where(tp => tp.TourId == id).Select(tp => tp.PoiId).ToListAsync();
            ViewBag.AvailablePOIs = await _context.POIs
                .Where(p => !currentPoiIds.Contains(p.Id))
                .ToListAsync();

            return View(tour);
        }

        [HttpPost]
        public async Task<IActionResult> AddPoiToTour(int tourId, int poiId)
        {
            var maxOrder = await _context.TourPOIs.Where(tp => tp.TourId == tourId).MaxAsync(tp => (int?)tp.OrderIndex) ?? 0;
            var newTp = new TourPOI { TourId = tourId, PoiId = poiId, OrderIndex = maxOrder + 1 };
            _context.TourPOIs.Add(newTp);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemovePoiFromTour(int tourPoiId)
        {
            var tp = await _context.TourPOIs.FindAsync(tourPoiId);
            if (tp != null)
            {
                _context.TourPOIs.Remove(tp);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrder([FromBody] List<int> tourPoiIds)
        {
            int order = 1;
            foreach (var id in tourPoiIds)
            {
                var tp = await _context.TourPOIs.FindAsync(id);
                if (tp != null) tp.OrderIndex = order++;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // --- API MỚI: Cập nhật thứ tự kèm thống kê Quãng đường & Thời gian ---
        [HttpPost]
        public async Task<IActionResult> UpdateOrderWithStats([FromBody] UpdateRouteStatsRequest request)
        {
            if (request == null || request.OrderedIds == null)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            try
            {
                // 1. Cập nhật lại thứ tự các địa điểm (POI)
                for (int i = 0; i < request.OrderedIds.Count; i++)
                {
                    var tourPoi = await _context.TourPOIs.FindAsync(request.OrderedIds[i]);
                    if (tourPoi != null)
                    {
                        tourPoi.OrderIndex = i + 1;
                        _context.Update(tourPoi);
                    }
                }

                // 2. Chốt số Quãng đường và Thời gian vào bảng Tour
                var tour = await _context.Tours.FindAsync(request.TourId);
                if (tour != null)
                {
                    tour.Distance = request.Distance;
                    tour.Duration = request.Duration;
                    _context.Update(tour);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> OptimizeRoute(int tourId)
        {
            var tourPois = await _context.TourPOIs
                .Include(tp => tp.POI)
                .Where(tp => tp.TourId == tourId)
                .ToListAsync();

            if (tourPois.Count <= 2) return Json(new { success = true });

            var unvisited = tourPois.ToList();
            var optimized = new List<TourPOI>();

            var current = unvisited.OrderBy(tp => tp.OrderIndex).First();
            optimized.Add(current);
            unvisited.Remove(current);

            while (unvisited.Any())
            {
                var nearest = unvisited.OrderBy(p => CalculateDistance(current.POI.Latitude, current.POI.Longitude, p.POI.Latitude, p.POI.Longitude)).First();
                optimized.Add(nearest);
                current = nearest;
                unvisited.Remove(nearest);
            }

            for (int i = 0; i < optimized.Count; i++)
            {
                optimized[i].OrderIndex = i + 1;
                _context.Update(optimized[i]);
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371e3;
            var p1 = lat1 * Math.PI / 180.0;
            var p2 = lat2 * Math.PI / 180.0;
            var dp = (lat2 - lat1) * Math.PI / 180.0;
            var dl = (lon2 - lon1) * Math.PI / 180.0;

            var a = Math.Sin(dp / 2) * Math.Sin(dp / 2) + Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }
    }

    // --- KHUÔN HỨNG DỮ LIỆU TỪ TRANG THIẾT KẾ LỘ TRÌNH GỬI VỀ ---
    public class UpdateRouteStatsRequest
    {
        public List<int> OrderedIds { get; set; }
        public string Distance { get; set; }
        public string Duration { get; set; }
        public int TourId { get; set; }
    }
}