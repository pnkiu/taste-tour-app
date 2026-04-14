using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelGuide.Web.Models
{
    public class Audio
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên file/tiêu đề")]
        [Display(Name = "Tiêu đề Audio")]
        public string Title { get; set; }

        [Display(Name = "Đường dẫn file")]
        public string? FileUrl { get; set; } // Sẽ lưu đường dẫn như: /audios/file_name.mp3

        [Display(Name = "Ngôn ngữ")]
        public string Language { get; set; } = "VN"; // Mặc định là Tiếng Việt

        // --- TẠO LIÊN KẾT VỚI BẢNG POI ---
        [Display(Name = "Thuộc Địa điểm (POI)")]
        public int PoiId { get; set; }

        [ForeignKey("PoiId")]
        public POI? POI { get; set; }
    }
}