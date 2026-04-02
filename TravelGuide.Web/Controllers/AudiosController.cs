using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelGuide.Web.Data;
using TravelGuide.Web.Models;

namespace TravelGuide.Web.Controllers
{
    public class AudiosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AudiosController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Audios
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Audios.Include(a => a.POI);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Audios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var audio = await _context.Audios
                .Include(a => a.POI)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (audio == null)
            {
                return NotFound();
            }

            return View(audio);
        }

        // GET: Audios/Create
        public IActionResult Create()
        {
            ViewData["PoiId"] = new SelectList(_context.POIs, "Id", "Name");
            return View();
        }

        // POST: Audios/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Language,PoiId")] Audio audio, IFormFile? audioFile)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem người dùng có chọn file MP3 không
                if (audioFile != null && audioFile.Length > 0)
                {
                    // Tạo tên file độc nhất (tránh bị trùng tên file cũ)
                    string extension = Path.GetExtension(audioFile.FileName);
                    string newFileName = Guid.NewGuid().ToString() + extension;

                    //  Lấy đường dẫn tới thư mục wwwroot/audios
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string path = Path.Combine(wwwRootPath, "audios", newFileName);

                    // Copy file từ Form lên ổ cứng Server
                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await audioFile.CopyToAsync(fileStream);
                    }

                    // Lưu đường dẫn tương đối vào Database (để App Mobile dễ gọi)
                    audio.FileUrl = "/audios/" + newFileName;
                }

                // Lưu thông tin vào Database
                _context.Add(audio);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

        
            ViewData["PoiId"] = new SelectList(_context.POIs, "Id", "Name", audio.PoiId);
            return View(audio);
        }

        // GET: Audios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var audio = await _context.Audios.FindAsync(id);
            if (audio == null)
            {
                return NotFound();
            }
            ViewData["PoiId"] = new SelectList(_context.POIs, "Id", "Name", audio.PoiId);
            return View(audio);
        }

        // POST: Audios/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,FileUrl,Language,PoiId")] Audio audio)
        {
            if (id != audio.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(audio);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AudioExists(audio.Id))
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
            ViewData["PoiId"] = new SelectList(_context.POIs, "Id", "Name", audio.PoiId);
            return View(audio);
        }

        // GET: Audios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var audio = await _context.Audios
                .Include(a => a.POI)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (audio == null)
            {
                return NotFound();
            }

            return View(audio);
        }

        // POST: Audios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var audio = await _context.Audios.FindAsync(id);
            if (audio != null)
            {
                _context.Audios.Remove(audio);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AudioExists(int id)
        {
            return _context.Audios.Any(e => e.Id == id);
        }
    }
}
