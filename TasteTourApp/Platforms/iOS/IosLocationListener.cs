// File này chỉ compile trên iOS
#if IOS
using CoreLocation;
using Foundation;
using TasteTourApp.Services.Geofence;

namespace TasteTourApp.Platforms.iOS
{
    /// <summary>
    /// Lắng nghe GPS bằng CLLocationManager (native iOS).
    /// Gọi GeofenceEngine.OnLocationUpdated() mỗi khi có vị trí mới.
    /// </summary>
    public class IosLocationListener : NSObject, ICLLocationManagerDelegate
    {
        private readonly CLLocationManager _manager = new();
        private readonly GeofenceEngine _engine;

        public IosLocationListener(GeofenceEngine engine)
        {
            _engine = engine;
        }

        public void Start()
        {
            _manager.Delegate = this;
            _manager.DesiredAccuracy = CLLocation.AccuracyBest;
            _manager.DistanceFilter = 10; // mét — chỉ update khi di chuyển > 10m
            _manager.RequestWhenInUseAuthorization();
            _manager.StartUpdatingLocation();
            _engine.Start();

            System.Diagnostics.Debug.WriteLine("[IosLocationListener] Đã bắt đầu GPS");
        }

        public void Stop()
        {
            _engine.Stop();
            _manager.StopUpdatingLocation();
            System.Diagnostics.Debug.WriteLine("[IosLocationListener] Đã dừng GPS");
        }

        [Export("locationManager:didUpdateLocations:")]
        public void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            var last = locations.LastOrDefault();
            if (last == null) return;

            double lat = last.Coordinate.Latitude;
            double lng = last.Coordinate.Longitude;

            System.Diagnostics.Debug.WriteLine($"[GPS] lat={lat:F6}  lng={lng:F6}");
            _engine.OnLocationUpdated(lat, lng);
        }

        [Export("locationManager:didFailWithError:")]
        public void Failed(CLLocationManager manager, NSError error)
        {
            System.Diagnostics.Debug.WriteLine($"[IosLocationListener] Lỗi GPS: {error.LocalizedDescription}");
        }
    }
}
#endif
