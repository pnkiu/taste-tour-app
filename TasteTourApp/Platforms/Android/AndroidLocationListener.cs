// File này chỉ compile trên Android
#if ANDROID
using Android.Gms.Location;
using Android.OS;
using TasteTourApp.Services.Geofence;

namespace TasteTourApp.Platforms.Android
{
    /// <summary>
    /// Lắng nghe GPS bằng FusedLocationProviderClient (Google Play Services).
    /// Gọi GeofenceEngine.OnLocationUpdated() mỗi khi có vị trí mới.
    /// </summary>
    public class AndroidLocationListener
    {
        private IFusedLocationProviderClient? _client;
        private LocationCallback? _callback;
        private readonly GeofenceEngine _engine;

        public AndroidLocationListener(GeofenceEngine engine)
        {
            _engine = engine;
        }

        public void Start()
        {
            _client = LocationServices.GetFusedLocationProviderClient(
                          Microsoft.Maui.ApplicationModel.Platform.AppContext);

            var request = new LocationRequest.Builder(
                              Priority.PriorityHighAccuracy,
                              5_000L)        // 5 giây / lần
                              .SetMinUpdateDistanceMeters(10f) // > 10m mới update
                              .Build();

            _callback = new GeofenceLocationCallback(_engine);

            _client?.RequestLocationUpdates(request, _callback, Looper.MainLooper);
            _engine.Start();

            System.Diagnostics.Debug.WriteLine("[AndroidLocationListener] Đã bắt đầu GPS");
        }

        public void Stop()
        {
            _engine.Stop();
            if (_callback != null)
                _client?.RemoveLocationUpdates(_callback);
            System.Diagnostics.Debug.WriteLine("[AndroidLocationListener] Đã dừng GPS");
        }

        // ── Inner callback ────────────────────────────────────────────
        private class GeofenceLocationCallback : LocationCallback
        {
            private readonly GeofenceEngine _engine;

            public GeofenceLocationCallback(GeofenceEngine engine)
            {
                _engine = engine;
            }

            public override void OnLocationResult(LocationResult result)
            {
                if (result?.LastLocation is { } loc)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[GPS] lat={loc.Latitude:F6}  lng={loc.Longitude:F6}");
                    _engine.OnLocationUpdated(loc.Latitude, loc.Longitude);
                }
            }
        }
    }
}
#endif
