using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelGuide.Web.Models
{
    public class TourPOI
    {
        [Key]
        public int Id { get; set; }

        // Móc nối với Tour
        public int TourId { get; set; }
        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }

        // Móc nối với Quán ăn (POI)
        public int PoiId { get; set; }
        [ForeignKey("PoiId")]
        public POI? POI { get; set; }

        // Thứ tự đi trong lộ trình (1, 2, 3...)
        [Display(Name = "Thứ tự ghé thăm")]
        public int OrderIndex { get; set; }
    }
}