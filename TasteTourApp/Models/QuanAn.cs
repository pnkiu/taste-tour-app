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
    }
}
