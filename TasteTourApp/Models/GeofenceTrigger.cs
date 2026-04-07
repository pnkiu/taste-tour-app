using TasteTourApp.Models;

namespace TasteTourApp.Models
{
    public enum GeofenceTriggerType { Enter, Nearby }

    public class GeofenceTrigger
    {
        public QuanAn Quan { get; set; } = null!;
        public double DistanceMeters { get; set; }
        public GeofenceTriggerType Type { get; set; }
    }
}
