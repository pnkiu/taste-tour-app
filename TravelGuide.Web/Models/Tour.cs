using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Thêm dòng này để xài được [NotMapped]
using Microsoft.AspNetCore.Http; // Thêm dòng này để xài được IFormFile

namespace TravelGuide.Web.Models
{
    public class Tour
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên hành trình")]
        [Display(Name = "Tên Hành trình (Tour)")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Mô tả hấp dẫn")]
        public string? Description { get; set; }

        [Display(Name = "Thời gian dự kiến")]
        public string? Duration { get; set; } // Ví dụ: "2 giờ", "1 buổi tối"

        [Display(Name = "Tổng quãng đường")]
        public string? Distance { get; set; } // Ví dụ: "1.5 km"

        [Display(Name = "Link ảnh bìa")]
        public string? ImageUrl { get; set; }

        
        [NotMapped] 
        [Display(Name = "Chọn ảnh tải lên")]
        public IFormFile? ImageUpload { get; set; }
    }
}