using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using SQLite;

namespace TasteTourApp.Models
{
    public class QuanAn
    {
        [PrimaryKey]
        [JsonPropertyName("id")]
        public string Id {  get; set; }

        [JsonPropertyName("name")]
        public string TenQuan { get; set; }

        [JsonPropertyName("description")]
        public string MoTa { get; set; }

        [JsonPropertyName("descriptionEn")]
        public string MoTaEn { get; set; }

        [JsonPropertyName("latitude")]
        public double ViDo {  get; set; }

        [JsonPropertyName("longitude")]
        public double KinhDo { get; set; }

        [JsonPropertyName("imageUrl")]
        public string HinhAnh { get; set; }

        [JsonPropertyName("audioContent")]
        public string AudioContent { get; set; }
        [JsonPropertyName("audioContentEn")]
        public string AudioContentEn { get; set; }
        public string LoaiQuan { get; set; }      // "Oc" / "HaiSan" / "Sushi"
        [JsonPropertyName("radius")]
        public double BanKinhMet { get; set; } = 50;  // Geofence radius (mét)

        [JsonPropertyName("priority")]
        public int MucUuTien { get; set; } = 1;       // Số càng lớn càng được ưu tiên
        public int ThuTuHienThi { get; set; }         // Thứ tự trong danh sách
        public bool IsSaved { get; set; } = false;    // Trạng thái yêu thích
    }
}
