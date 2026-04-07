using TasteTourApp.Models;
using TasteTourApp.Services;

namespace TasteTourApp.Services.Geofence
{
    /// <summary>
    /// Geofence Engine — theo dõi vị trí, so sánh với danh sách QuanAn,
    /// kích hoạt sự kiện khi người dùng vào bán kính của một quán.
    /// Cross-platform: không phụ thuộc Android/iOS.
    /// </summary>
    public class GeofenceEngine
    {
        // ── Sự kiện phát ra khi người dùng vào vùng POI ──────────────
        public event EventHandler<GeofenceTrigger>? PoiTriggered;

        // ── Cấu hình ─────────────────────────────────────────────────
        /// Thời gian chờ trước khi đánh giá lại sau khi nhận GPS mới
        public TimeSpan Debounce { get; set; } = TimeSpan.FromSeconds(3);

        /// Thời gian cooldown: không kích hoạt lại cùng 1 quán trong khoảng này
        public TimeSpan Cooldown { get; set; } = TimeSpan.FromMinutes(10);

        // ── Trạng thái nội bộ ─────────────────────────────────────────
        private readonly DatabaseService _dbService;
        private readonly Dictionary<string, DateTime> _lastTriggered = new();
        private CancellationTokenSource? _debounceCts;
        private bool _isRunning;

        // Cho phép MainPage subscribe để update UI (highlight POI gần nhất)
        public event EventHandler<(string poiId, double distanceMeters)>? NearestPoiChanged;

        public GeofenceEngine(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        // ── Vòng đời ──────────────────────────────────────────────────

        public void Start() => _isRunning = true;

        public void Stop()
        {
            _isRunning = false;
            _debounceCts?.Cancel();
        }

        /// <summary>
        /// Gọi mỗi khi có cập nhật GPS mới (từ MainPage hoặc background service).
        /// Áp dụng debounce trước khi đánh giá POI.
        /// </summary>
        public void OnLocationUpdated(double lat, double lng)
        {
            if (!_isRunning) return;

            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            Task.Delay(Debounce, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                    _ = EvaluatePoisAsync(lat, lng);
            }, TaskScheduler.Default);
        }

        // ── Logic chính ───────────────────────────────────────────────

        private async Task EvaluatePoisAsync(double userLat, double userLng)
        {
            try
            {
                var allQuan = await _dbService.LayDanhSachQuanAn();

                // Tính khoảng cách tới mọi quán
                var withDistance = allQuan
                    .Select(q => new
                    {
                        Quan = q,
                        Distance = Haversine(userLat, userLng, q.ViDo, q.KinhDo)
                    })
                    .OrderBy(x => x.Quan.MucUuTien)
                    .ThenBy(x => x.Distance)
                    .ToList();

                // Phát sự kiện POI gần nhất (để UI highlight, không cần trong bán kính)
                var nearest = withDistance.FirstOrDefault();
                if (nearest != null)
                {
                    NearestPoiChanged?.Invoke(this,
                        (nearest.Quan.Id, nearest.Distance));
                }

                // Chỉ kích hoạt các quán TRONG bán kính, theo thứ tự ưu tiên
                foreach (var item in withDistance)
                {
                    double radius = item.Quan.BanKinhMet > 0 ? item.Quan.BanKinhMet : 50;
                    if (item.Distance > radius) continue;

                    // Kiểm tra cooldown
                    if (_lastTriggered.TryGetValue(item.Quan.Id, out var lastTime) &&
                        DateTime.UtcNow - lastTime < Cooldown)
                        continue;

                    // Ghi log và phát sự kiện
                    _lastTriggered[item.Quan.Id] = DateTime.UtcNow;

                    var trigger = new GeofenceTrigger
                    {
                        Quan = item.Quan,
                        DistanceMeters = item.Distance,
                        Type = GeofenceTriggerType.Enter
                    };

                    // Invoke trên Main thread để UI có thể update trực tiếp
                    MainThread.BeginInvokeOnMainThread(() =>
                        PoiTriggered?.Invoke(this, trigger));

                    break; // Chỉ kích hoạt 1 POI ưu tiên nhất mỗi lần
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[GeofenceEngine] Lỗi EvaluatePois: {ex.Message}");
            }
        }

        /// <summary>Reset cooldown của tất cả POI (dùng khi test)</summary>
        public void ResetCooldowns() => _lastTriggered.Clear();

        // ── Haversine ─────────────────────────────────────────────────

        /// <summary>Tính khoảng cách giữa 2 toạ độ GPS (mét)</summary>
        public static double Haversine(double lat1, double lng1,
                                       double lat2, double lng2)
        {
            const double R = 6_371_000;
            double dLat = ToRad(lat2 - lat1);
            double dLng = ToRad(lng2 - lng1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}
