using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using TasteTourApp.Models;

namespace TasteTourApp.Services
{
    public class DatabaseService
    {
        // Tăng số này lên mỗi khi muốn reset data (ví dụ đổi tọa độ, thêm quán)
        private const int DATA_VERSION = 4;

        private SQLiteAsyncConnection _db;
        private bool _daKhoiTao = false;

        private async Task Init()
        {
            // Chỉ khởi tạo 1 lần duy nhất trong suốt vòng đời app
            if (_daKhoiTao) return;
            _daKhoiTao = true;

            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "TasteTour_VinhKhanh.db3");
            _db = new SQLiteAsyncConnection(databasePath);
            await _db.CreateTableAsync<QuanAn>();

            // Kiểm tra version — nếu data cũ thì xóa và seed lại
            var soLuong = await _db.Table<QuanAn>().CountAsync();
            bool canSeedLai = soLuong == 0;

            // Dùng 1 quán "sentinel" để kiểm tra version
            // Nếu VK_VERSION không tồn tại hoặc Id khác → data cũ → seed lại
            var sentinel = await _db.Table<QuanAn>()
                .FirstOrDefaultAsync(q => q.Id == "VK_VERSION");
            if (sentinel == null || sentinel.TenQuan != DATA_VERSION.ToString())
            {
                // Xóa toàn bộ data cũ
                await _db.DeleteAllAsync<QuanAn>();
                canSeedLai = true;
            }

            if (canSeedLai)
            {
                await SeedDuLieu();
            }
        }

        private async Task SeedDuLieu()
        {
            // --- Sentinel version record ---
            await _db.InsertAsync(new QuanAn
            {
                Id = "VK_VERSION",
                TenQuan = DATA_VERSION.ToString(),
                MoTa = "",
                ViDo = 0,
                KinhDo = 0
            });

            // --- Dữ liệu thật ---
            // Tọa độ kiểm tra tại: https://www.google.com/maps → chuột phải → copy tọa độ
            // Định dạng Google Maps: (vĩ độ, kinh độ) — ĐÚNG với ViDo/KinhDo bên dưới

            await _db.InsertAsync(new QuanAn
            {
                Id = "VK_01",
                TenQuan = "Ốc Phát Vĩnh Khánh",
                MoTa = "Quán ốc huyền thoại sầm uất nhất con đường. Nổi tiếng với ốc hương nướng muối ớt và càng ghẹ.",
                ViDo = 10.761967135852936,
                KinhDo = 106.70209485438174,
                HinhAnh = "quan_oc_phat.jpg",
                LoaiQuan = "Oc",
                MucUuTien = 3,
                ThuTuHienThi = 1
            });

            await _db.InsertAsync(new QuanAn
            {
                Id = "VK_02",
                TenQuan = "Ốc Thảo",
                MoTa = "Không gian thoáng mát, menu hải sản đa dạng. Sò điệp nướng mỡ hành ở đây là chân ái.",
                ViDo = 10.761688291527175,
                KinhDo = 106.7023669506661,
                HinhAnh= "quan_oc_thao.jpg",
                LoaiQuan = "HaiSan",    // ← thêm
                MucUuTien = 2,
                ThuTuHienThi = 2
            });

            await _db.InsertAsync(new QuanAn
            {
                Id = "VK_03",
                TenQuan = "Sushi Viên Vĩnh Khánh",
                MoTa = "Đổi gió với sushi giá sinh viên ngay giữa phố ốc. Ngon, bổ, rẻ và cực kỳ đông khách.",
                // TODO: Thay tọa độ đúng bên dưới
                // Cách lấy: mở Google Maps → tìm quán → chuột phải vào đúng vị trí → copy tọa độ
                ViDo = 10.762142,
                KinhDo = 106.702456,
                LoaiQuan = "Sushi",     // ← thêm
                MucUuTien = 1,
                ThuTuHienThi = 3
            });
        }

        // ============================================================
        //  API PUBLIC
        // ============================================================
        public async Task<List<QuanAn>> LayDanhSachQuanAn()
        {
            await Init();
            // Lọc bỏ record sentinel version
            return await _db.Table<QuanAn>()
                .Where(q => q.Id != "VK_VERSION")
                .ToListAsync();
        }

        public async Task<QuanAn> LayQuanAnTheoId(string idQuan)
        {
            await Init();
            return await _db.Table<QuanAn>()
                .FirstOrDefaultAsync(q => q.Id == idQuan);
        }
    }
}
