using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelGuide.Web.Data;
using TravelGuide.Web.Models;

namespace TravelGuide.Web.Controllers
{
    public class POIsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public POIsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: POIs
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách POI, bao gồm luôn dữ liệu từ bảng trung gian TourPOI và bảng Tour
            var applicationDbContext = _context.POIs
                .Include(p => p.TourPOIs)
                .ThenInclude(tp => tp.Tour); // Kéo luôn tên Tour ra

            return View(await applicationDbContext.ToListAsync());
        }

        // GET: POIs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pOI = await _context.POIs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pOI == null)
            {
                return NotFound();
            }

            return View(pOI);
        }

        // GET: POIs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: POIs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,DescriptionEn,Latitude,Longitude,Radius,Priority,ImageUrl,AudioContent,AudioContentEn,MapLink,Tags")] POI pOI)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pOI);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pOI);
        }

        // GET: POIs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pOI = await _context.POIs.FindAsync(id);
            if (pOI == null)
            {
                return NotFound();
            }
            return View(pOI);
        }

        // POST: POIs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
    [Bind("Id,Name,Description,DescriptionEn,Latitude,Longitude,Radius,Priority,ImageUrl,AudioContent,AudioContentEn,MapLink,Tags")] POI pOI,
            IFormFile? imageFile,
            IFormFile? audioFile,
            IFormFile? audioFileEn) // <-- Thêm tham số nhận file tiếng Anh
        {
            if (id != pOI.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Xử lý upload ảnh (Giữ nguyên cấu trúc của bạn)
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "pois");
                        Directory.CreateDirectory(uploadsFolder);
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create)) { await imageFile.CopyToAsync(stream); }
                        pOI.ImageUrl = $"/uploads/pois/{uniqueFileName}";
                    }

                    // 2. Xử lý upload audio TIẾNG VIỆT & Đồng bộ sang bảng Audios
                    if (audioFile != null && audioFile.Length > 0)
                    {
                        var audiosFolder = Path.Combine(_env.WebRootPath, "uploads", "audios");
                        Directory.CreateDirectory(audiosFolder);
                        var uniqueAudioName = $"vn_{Guid.NewGuid()}_{Path.GetFileName(audioFile.FileName)}";
                        var audioPath = Path.Combine(audiosFolder, uniqueAudioName);
                        using (var stream = new FileStream(audioPath, FileMode.Create)) { await audioFile.CopyToAsync(stream); }
                        pOI.AudioContent = $"/uploads/audios/{uniqueAudioName}";

                        // -- MA THUẬT: Đẩy dữ liệu sang bảng Audio --
                        var existingVnAudio = await _context.Audios.FirstOrDefaultAsync(a => a.PoiId == pOI.Id && a.Language == "Tiếng Việt (VN)");
                        if (existingVnAudio != null)
                        {
                            existingVnAudio.FileUrl = pOI.AudioContent;
                            existingVnAudio.Title = $"Giọng thuyết minh Tiếng Việt - {pOI.Name}";
                            _context.Update(existingVnAudio);
                        }
                        else
                        {
                            _context.Add(new Audio { PoiId = pOI.Id, Language = "Tiếng Việt (VN)", FileUrl = pOI.AudioContent, Title = $"Giọng thuyết minh Tiếng Việt - {pOI.Name}" });
                        }
                    }

                    // 3. Xử lý upload audio TIẾNG ANH & Đồng bộ sang bảng Audios
                    if (audioFileEn != null && audioFileEn.Length > 0)
                    {
                        var audiosFolder = Path.Combine(_env.WebRootPath, "uploads", "audios");
                        Directory.CreateDirectory(audiosFolder);
                        var uniqueAudioName = $"en_{Guid.NewGuid()}_{Path.GetFileName(audioFileEn.FileName)}";
                        var audioPath = Path.Combine(audiosFolder, uniqueAudioName);
                        using (var stream = new FileStream(audioPath, FileMode.Create)) { await audioFileEn.CopyToAsync(stream); }
                        pOI.AudioContentEn = $"/uploads/audios/{uniqueAudioName}";

                        // -- MA THUẬT: Đẩy dữ liệu sang bảng Audio --
                        var existingEnAudio = await _context.Audios.FirstOrDefaultAsync(a => a.PoiId == pOI.Id && a.Language == "English (EN)");
                        if (existingEnAudio != null)
                        {
                            existingEnAudio.FileUrl = pOI.AudioContentEn;
                            existingEnAudio.Title = $"English Audio - {pOI.Name}";
                            _context.Update(existingEnAudio);
                        }
                        else
                        {
                            _context.Add(new Audio { PoiId = pOI.Id, Language = "English (EN)", FileUrl = pOI.AudioContentEn, Title = $"English Audio - {pOI.Name}" });
                        }
                    }

                    _context.Update(pOI);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!POIExists(pOI.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(pOI);
        }

        // GET: POIs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pOI = await _context.POIs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pOI == null)
            {
                return NotFound();
            }

            return View(pOI);
        }

        // POST: POIs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pOI = await _context.POIs.FindAsync(id);
            if (pOI != null)
            {
                _context.POIs.Remove(pOI);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool POIExists(int id)
        {
            return _context.POIs.Any(e => e.Id == id);
        }
    }
}
