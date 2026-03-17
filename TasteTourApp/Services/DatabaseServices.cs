using System;
using System.Collections.Generic;
using System.Text;
using SQLite;
using TasteTourApp.Models; // Gọi khuôn đúc QuanAn ra

namespace TasteTourApp.Services
{
    public class DatabaseService
    {
        // Biến kết nối đến kho chứa SQLite
        private SQLiteAsyncConnection _db;
        private async Task Init()
        {
            // Nếu kho đã mở rồi thì không cần mở lại
            if (_db != null)
                return;

            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "TasteTour.db3");
            
            _db = new SQLiteAsyncConnection(databasePath);

            // Tạo table dựa trên khuôn đúc QuanAn 
            await _db.CreateTableAsync<QuanAn>();

            // Nạp dữ liệu mẫu
            // Đếm xem trong bảng đã có quán ăn nào chưa

            var soLuong = await _db.Table<QuanAn>().CountAsync();
            if (soLuong == 0)
            {
                // Nếu kho trống (lần đầu cài app), thủ kho sẽ tự động xếp 2 món này vào kho
                await _db.InsertAsync(new QuanAn { Id = "POI_01", TenQuan = "Bún Bò Huế Oanh", MoTa = "Đặc sản bún bò chuẩn vị Huế, nước dùng thanh ngọt mắm ruốc.", ViDo = 10.762622, KinhDo = 106.660172 });
                await _db.InsertAsync(new QuanAn { Id = "POI_02", TenQuan = "Chè Thái Ý Phương", MoTa = "Quán chè sầu riêng nổi tiếng nhất khu phố, luôn tấp nập khách.", ViDo = 10.763000, KinhDo = 106.660500 });
            }
        }

        // HÀM XUẤT DỮ LIỆU RA CHO GIAO DIỆN
        public async Task<List<QuanAn>> LayDanhSachQuanAn()
        {
            // Đảm bảo kho đã được khởi tạo trước khi lấy đồ
            await Init();
            // Nhặt toàn bộ dữ liệu trong bảng QuanAn và trả về dưới dạng Danh sách (List)
            return await _db.Table<QuanAn>().ToListAsync();
        }
    }
}
