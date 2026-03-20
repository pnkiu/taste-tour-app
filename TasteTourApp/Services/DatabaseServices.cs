using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "TasteTour_VinhKhanh.db3");

            _db = new SQLiteAsyncConnection(databasePath);

            await _db.CreateTableAsync<QuanAn>();

            var soLuong = await _db.Table<QuanAn>().CountAsync();
            if(soLuong == 0)
            {
                await _db.InsertAsync(new QuanAn { Id = "VK_01", TenQuan = "Ốc Phát Vĩnh Khánh", MoTa = "Quán ốc huyền thoại sầm uất nhất con đường. Nổi tiếng với ốc hương nướng muối ớt và càng ghẹ.", ViDo = 10.761967135852936, KinhDo = 106.70209485438174 });
                await _db.InsertAsync(new QuanAn { Id = "VK_02", TenQuan = "Ốc Thảo", MoTa = "Không gian thoáng mát, menu hải sản đa dạng. Sò điệp nướng mỡ hành ở đây là chân ái.", ViDo = 10.761688291527175, KinhDo = 106.7023669506661 });
                await _db.InsertAsync(new QuanAn { Id = "VK_03", TenQuan = "Sushi Viên Vĩnh Khánh", MoTa = "Đổi gió với sushi giá sinh viên ngay giữa phố ốc. Ngon, bổ, rẻ và cực kỳ đông khách.", ViDo = 10.762500, KinhDo = 106.699000 });
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

        public async Task<QuanAn> LayQuanAnTheoId(string idQuan)
        {
            await Init();
            return await _db.Table<QuanAn>().FirstOrDefaultAsync(q => q.Id == idQuan);
        }
    }
}
