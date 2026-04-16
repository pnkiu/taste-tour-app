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
        // v6: Thêm cột IsSaved vào QuanAn
        // v9: Hỗ trợ SyncTuApi — không cần seed cứng nữa nếu có API
        private const int DATA_VERSION = 9;

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
            var sentinel = await _db.Table<QuanAn>()
                .FirstOrDefaultAsync(q => q.Id == "VK_VERSION");

            if (sentinel == null || sentinel.TenQuan != DATA_VERSION.ToString())
            {
                // Xóa toàn bộ data cũ (kể cả IsSaved — chỉ xảy ra khi nâng cấp version)
                await _db.DeleteAllAsync<QuanAn>();

                // Ghi sentinel version mới
                await _db.InsertAsync(new QuanAn
                {
                    Id = "VK_VERSION",
                    TenQuan = DATA_VERSION.ToString(),
                    MoTa = "",
                    ViDo = 0,
                    KinhDo = 0
                });

                // Seed dữ liệu mặc định (dự phòng khi chưa có API)
                await SeedDuLieu();
            }
        }

        private async Task SeedDuLieu()
        {
            // --- Dữ liệu dự phòng khi offline / chưa có API ---
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
                ThuTuHienThi = 1,
                BanKinhMet = 15
            });

            await _db.InsertAsync(new QuanAn
            {
                Id = "VK_02",
                TenQuan = "Ốc Thảo",
                MoTa = "Không gian thoáng mát, menu hải sản đa dạng. Sò điệp nướng mỡ hành ở đây là chân ái.",
                ViDo = 10.761688291527175,
                KinhDo = 106.7023669506661,
                HinhAnh = "quan_oc_thao.jpg",
                LoaiQuan = "HaiSan",
                MucUuTien = 2,
                ThuTuHienThi = 2,
                BanKinhMet = 15
            });

            await _db.InsertAsync(new QuanAn
            {
                Id = "VK_03",
                TenQuan = "Quán Nước SINZIEN",
                MoTa = "Trạm dừng chân lý tưởng với không gian cực chill. Chuyên các dòng trà trái cây giải nhiệt siêu mát lạnh. Món tủ: Trà hoa quả nhiệt đới.",
                ViDo = 10.761772317225041,
                KinhDo = 106.70227311017166,
                HinhAnh = "sinzien.jpg",
                LoaiQuan = "DoUong",
                MucUuTien = 1,
                ThuTuHienThi = 3,
                BanKinhMet = 15
            });

            await _db.InsertAsync(new QuanAn
            {
                Id = "VK_04",
                TenQuan = "Nướng Ngói Ti Ti",
                MoTa = "Ngon",
                ViDo = 10.761588480289126,
                KinhDo = 106.70254198833904,
                HinhAnh = "nuong_ngoi_titi.jpg",
                LoaiQuan = "HaiSan",
                MucUuTien = 5,
                ThuTuHienThi = 4,
                BanKinhMet = 15
            });

            await _db.InsertAsync(new QuanAn
            {
                Id = "VK_05",
                TenQuan = "Ốc 35k",
                MoTa = "Ngon",
                ViDo = 10.761538616423437,
                KinhDo = 106.7026227999941,
                HinhAnh = "oc_35k.jpg",
                LoaiQuan = "Oc",
                MucUuTien = 6,
                ThuTuHienThi = 5,
                BanKinhMet = 15
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

        /// <summary>Toggle trạng thái yêu thích của một quán ăn</summary>
        public async Task LuuYeuThich(string idQuan, bool isSaved)
        {
            await Init();
            var quan = await _db.Table<QuanAn>()
                .FirstOrDefaultAsync(q => q.Id == idQuan);
            if (quan != null)
            {
                quan.IsSaved = isSaved;
                await _db.UpdateAsync(quan);
            }
        }

        /// <summary>Lấy danh sách các quán đã được lưu yêu thích</summary>
        public async Task<List<QuanAn>> LayDanhSachYeuThich()
        {
            await Init();
            return await _db.Table<QuanAn>()
                .Where(q => q.IsSaved && q.Id != "VK_VERSION")
                .ToListAsync();
        }

        // ============================================================
        //  SYNC TỪ API — merge theo Id, giữ IsSaved
        // ============================================================
        /// <summary>
        /// Merge danh sách POI từ API vào DB local.
        /// - Nếu Id đã có: cập nhật thông tin nhưng GIỮ NGUYÊN IsSaved.
        /// - Nếu Id chưa có: chèn mới.
        /// - Không bao giờ xóa quán cũ (để offline vẫn hoạt động).
        /// </summary>
        /// <returns>(added, updated) — số quán thêm mới và số quán được cập nhật.</returns>
        public async Task<(int added, int updated)> SyncTuApi(List<QuanAn> apiData)
        {
            await Init();

            int added = 0;
            int updated = 0;

            foreach (var apiQuan in apiData)
            {
                // Bỏ qua nếu Id rỗng hoặc là sentinel
                if (string.IsNullOrEmpty(apiQuan.Id) || apiQuan.Id == "VK_VERSION")
                    continue;

                var existing = await _db.Table<QuanAn>()
                    .FirstOrDefaultAsync(q => q.Id == apiQuan.Id);

                if (existing != null)
                {
                    // ── CẬP NHẬT: giữ IsSaved của người dùng ──────────
                    apiQuan.IsSaved = existing.IsSaved;       // bảo toàn yêu thích
                    apiQuan.ThuTuHienThi = existing.ThuTuHienThi; // giữ thứ tự nếu không có từ API

                    await _db.UpdateAsync(apiQuan);
                    updated++;
                }
                else
                {
                    // ── THÊM MỚI ──────────────────────────────────────
                    await _db.InsertAsync(apiQuan);
                    added++;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[DB] SyncTuApi: +{added} mới, ~{updated} cập nhật");
            return (added, updated);
        }
    }
}
