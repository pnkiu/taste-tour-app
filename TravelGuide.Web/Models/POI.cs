using System.ComponentModel.DataAnnotations;

namespace TravelGuide.Web.Models
{
    public class POI
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên điểm thuyết minh")]
        [Display(Name = "Tên địa điểm")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        // --- Dữ liệu phục vụ Geofencing ---
        [Required]
        [Display(Name = "Vĩ độ (Lat)")]
        public double Latitude { get; set; }

        [Required]
        [Display(Name = "Kinh độ (Lng)")]
        public double Longitude { get; set; }

        [Display(Name = "Bán kính kích hoạt (m)")]
        public double Radius { get; set; } = 50;

        [Display(Name = "Mức ưu tiên")]
        public int Priority { get; set; } = 1;

        // --- Dữ liệu đa phương tiện ---
        [Display(Name = "Ảnh minh họa (Link)")]
        public string? ImageUrl { get; set; }

        [Display(Name = "File Audio hoặc Script TTS")]
        public string? AudioContent { get; set; }

        // --- Dữ liệu Đa ngôn ngữ (Tiếng Anh) ---
        [Display(Name = "Mô tả chi tiết (Tiếng Anh)")]
        public string? DescriptionEn { get; set; }

        [Display(Name = "File Audio Tiếng Anh (Link)")]
        public string? AudioContentEn { get; set; }

        [Display(Name = "Link bản đồ")]
        public string? MapLink { get; set; }

        // --- MỚI: Thẻ phân loại (Tags) ---
        [Display(Name = "Thẻ phân loại (Tags)")]
        [StringLength(500)]
        public string? Tags { get; set; }

        // Mối quan hệ: 1 POI có thể nằm trong nhiều Hành trình (Tour)
        public virtual ICollection<TourPOI> TourPOIs { get; set; } = new List<TourPOI>();
    }
}