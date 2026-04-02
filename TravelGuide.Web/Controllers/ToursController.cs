using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelGuide.Web.Data;
using TravelGuide.Web.Models;
using Microsoft.AspNetCore.Hosting; // Thư viện xử lý môi trường (thư mục root)
using System.IO; // Thư viện xử lý File (Lưu, xóa, tạo thư mục)

namespace TravelGuide.Web.Controllers
{
    public class ToursController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // Khai báo công cụ

        // Constructor mới: Nhận thêm IWebHostEnvironment
        public ToursController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Tours
        public async Task<IActionResult> Index()
        {
            return View(await _context.Tours.ToListAsync());
        }

        // GET: Tours/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tour == null)
            {
                return NotFound();
            }

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
        // ĐỔI Bind: Bỏ ImageUrl đi, thay bằng ImageUpload
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Duration,Distance,ImageUpload")] Tour tour)
        {
            if (ModelState.IsValid)
            {
                // XỬ LÝ UPLOAD FILE
                if (tour.ImageUpload != null)
                {
                    // 1. Tạo đường dẫn đến thư mục wwwroot/uploads/tours
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tours");

                    // 2. Nếu thư mục chưa có thì tạo mới (tránh lỗi)
                    if (!Directory.Exists(uploadsFolder)) { Directory.CreateDirectory(uploadsFolder); }

                    // 3. Tạo tên file độc nhất (tránh trùng tên)
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + tour.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // 4. Lưu file vật lý vào máy chủ
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await tour.ImageUpload.CopyToAsync(fileStream);
                    }

                    // 5. Cập nhật đường link vào thuộc tính ImageUrl để lưu Database
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
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours.FindAsync(id);
            if (tour == null)
            {
                return NotFound();
            }
            return View(tour);
        }

        // POST: Tours/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Duration,Distance,ImageUrl")] Tour tour)
        {
            if (id != tour.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tour);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TourExists(tour.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tour);
        }

        // GET: Tours/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tour == null)
            {
                return NotFound();
            }

            return View(tour);
        }

        // POST: Tours/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour != null)
            {
                _context.Tours.Remove(tour);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TourExists(int id)
        {
            return _context.Tours.Any(e => e.Id == id);
        }

        // ==========================================================
        // --- KHU VỰC XỬ LÝ LỘ TRÌNH (TOUR POI) - ĐƯỢC THÊM MỚI ---
        // ==========================================================

        // 1. Hiển thị giao diện Thiết kế lộ trình
        public async Task<IActionResult> ManageRoute(int? id)
        {
            if (id == null) return NotFound();
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            // Lấy các quán ĐÃ CÓ trong Tour (Sắp xếp theo thứ tự đi)
            ViewBag.CurrentPOIs = await _context.TourPOIs
                .Include(tp => tp.POI)
                .Where(tp => tp.TourId == id)
                .OrderBy(tp => tp.OrderIndex)
                .ToListAsync();

            // Lấy các quán CHƯA CÓ trong Tour để hiển thị bên kho chọn
            var currentPoiIds = await _context.TourPOIs.Where(tp => tp.TourId == id).Select(tp => tp.PoiId).ToListAsync();
            ViewBag.AvailablePOIs = await _context.POIs
                .Where(p => !currentPoiIds.Contains(p.Id))
                .ToListAsync();

            return View(tour);
        }

        // 2. API: Thêm quán vào Tour
        [HttpPost]
        public async Task<IActionResult> AddPoiToTour(int tourId, int poiId)
        {
            var maxOrder = await _context.TourPOIs.Where(tp => tp.TourId == tourId).MaxAsync(tp => (int?)tp.OrderIndex) ?? 0;
            var newTp = new TourPOI { TourId = tourId, PoiId = poiId, OrderIndex = maxOrder + 1 };
            _context.TourPOIs.Add(newTp);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // 3. API: Xóa quán khỏi Tour
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

        // 4. API: Cập nhật lại thứ tự khi người dùng kéo thả
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

        // 5. API: Tối ưu hóa lộ trình bằng Thuật toán Greedy & Công thức Haversine
        [HttpPost]
        public async Task<IActionResult> OptimizeRoute(int tourId)
        {
            var tourPois = await _context.TourPOIs
                .Include(tp => tp.POI)
                .Where(tp => tp.TourId == tourId)
                .ToListAsync();

            if (tourPois.Count <= 2) return Json(new { success = true }); // Ít quá khỏi tối ưu

            var unvisited = tourPois.ToList();
            var optimized = new List<TourPOI>();

            // Lấy điểm xuất phát là điểm đi đầu tiên hiện tại
            var current = unvisited.OrderBy(tp => tp.OrderIndex).First();
            optimized.Add(current);
            unvisited.Remove(current);

            while (unvisited.Any())
            {
                // Thuật toán Tham lam (Greedy): Luôn tìm điểm gần nhất với điểm hiện tại
                var nearest = unvisited.OrderBy(p => CalculateDistance(current.POI.Latitude, current.POI.Longitude, p.POI.Latitude, p.POI.Longitude)).First();
                optimized.Add(nearest);
                current = nearest;
                unvisited.Remove(nearest);
            }

            // Cập nhật lại thứ tự mới vào Database
            for (int i = 0; i < optimized.Count; i++)
            {
                optimized[i].OrderIndex = i + 1;
                _context.Update(optimized[i]);
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Công thức Haversine tính khoảng cách giữa 2 tọa độ GPS (Đơn vị: mét)
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371e3; // Bán kính trái đất
            var p1 = lat1 * Math.PI / 180.0;
            var p2 = lat2 * Math.PI / 180.0;
            var dp = (lat2 - lat1) * Math.PI / 180.0;
            var dl = (lon2 - lon1) * Math.PI / 180.0;

            var a = Math.Sin(dp / 2) * Math.Sin(dp / 2) + Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }
    }
}