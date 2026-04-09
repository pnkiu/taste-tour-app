using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace TasteTourApp.Models
{
    public class QuanAn
    {
        [PrimaryKey]
        public string Id {  get; set; }

        public string TenQuan { get; set; }
        public string MoTa { get; set; }
        public double ViDo {  get; set; }
        public double KinhDo { get; set; }
        public string HinhAnh { get; set; }
        public string LoaiQuan { get; set; }      // "Oc" / "HaiSan" / "Sushi"
        public double BanKinhMet { get; set; } = 50;  // Geofence radius (mét)
        public int MucUuTien { get; set; } = 1;       // 1-10
        public int ThuTuHienThi { get; set; }         // Thứ tự trong danh sách
        public bool IsSaved { get; set; } = false;    // Trạng thái yêu thích
    }
}
