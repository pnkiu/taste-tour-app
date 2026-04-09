using TasteTourApp.Models;
using TasteTourApp.Services;

namespace TasteTourApp.Services.Geofence
{
    /// <summary>
    /// Geofence Engine — theo dõi vị trí, so sánh với danh sách POI (QuanAn),
    /// kích hoạt sự kiện khi người dùng vào bán kính (Enter) hoặc đến gần (Nearby).
    ///
    /// Luồng hoạt động:
    /// 1. App tải danh sách POI (lat/lng, bán kính, ưu tiên, nội dung thuyết minh).
    /// 2. Khi người dùng di chuyển, gọi OnLocationUpdated() → áp dụng debounce.
    /// 3. EvaluatePoisAsync: tính khoảng cách tới mọi POI.
    ///    - POI gần nhất (bất kể bán kính) → NearestPoiChanged (UI highlight).
    ///    - POI TRONG bán kính + ưu tiên cao + cooldown OK → PoiTriggered Enter.
    /// 4. Narration Engine (MainPage) nhận PoiTriggered → quyết định phát TTS/Audio.
    /// 5. _lastTriggered ghi log đã phát, tránh lặp trong Cooldown time.
    /// Cross-platform: không phụ thuộc Android/iOS native API.
    /// </summary>
    public class GeofenceEngine
    {
        // ── Sự kiện ─────────────────────────────────────────────────

        /// <summary>
        /// Phát ra khi người dùng VÀO bán kính của một POI (Enter).
        /// Narration Engine lắng nghe để phát TTS.
        /// </summary>
        public event EventHandler<GeofenceTrigger>? PoiTriggered;

        /// <summary>
        /// Phát ra khi POI gần nhất thay đổi (dù chưa trong bán kính).
        /// UI dùng để highlight marker trên bản đồ và NearestPoiCard.
        /// </summary>
        public event EventHandler<(string poiId, double distanceMeters)>? NearestPoiChanged;

        // ── Cấu hình ─────────────────────────────────────────────────

        /// <summary>Thời gian chờ sau GPS mới trước khi đánh giá POI (chống jitter)</summary>
        public TimeSpan Debounce { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Cooldown: không kích hoạt lại cùng 1 POI trong khoảng thời gian này.
        /// Chống spam/replay nội dung thuyết minh.
        /// </summary>
        public TimeSpan Cooldown { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Bán kính "Nearby" — nếu user trong khoảng này nhưng ngoài BanKinhMet
        /// của POI, phát GeofenceTriggerType.Nearby (chỉ notify, không phát TTS).
        /// </summary>
        public double NearbyRadiusMultiplier { get; set; } = 3.0; // ×BanKinhMet

        // ── Trạng thái nội bộ ─────────────────────────────────────────
        private readonly DatabaseService _dbService;

        /// <summary>Log thời điểm cuối cùng mỗi POI được trigger Enter (chống spam)</summary>
        private readonly Dictionary<string, DateTime> _lastTriggered = new();

        private CancellationTokenSource? _debounceCts;
        private bool _isRunning;
        private string? _lastNearestId; // Tránh phát NearestPoiChanged liên tục cùng 1 POI

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
        /// Áp dụng debounce trước khi đánh giá POI để tránh đánh giá quá nhiều.
        /// </summary>
        public void OnLocationUpdated(double lat, double lng)
        {
            if (!_isRunning) return;

            // Hủy chu kỳ debounce trước (nếu có), bắt đầu chu kỳ mới
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

        /// <summary>
        /// Kiểm tra tất cả POI, xác định POI gần nhất và POI cần kích hoạt.
        /// Chỉ kích hoạt POI ưu tiên cao nhất trong bán kính mỗi lần đánh giá.
        /// </summary>
        private async Task EvaluatePoisAsync(double userLat, double userLng)
        {
            try
            {
                var allQuan = await _dbService.LayDanhSachQuanAn();

                // Tính khoảng cách tới mọi POI, sắp xếp: ưu tiên cao → khoảng cách gần
                var withDistance = allQuan
                    .Select(q => new
                    {
                        Quan = q,
                        Distance = Haversine(userLat, userLng, q.ViDo, q.KinhDo)
                    })
                    .OrderBy(x => x.Quan.MucUuTien)
                    .ThenBy(x => x.Distance)
                    .ToList();

                // ── Bước 1: Phát NearestPoiChanged (UI highlight, không cần trong bán kính) ──
                var nearest = withDistance.MinBy(x => x.Distance);
                if (nearest != null && nearest.Quan.Id != _lastNearestId)
                {
                    _lastNearestId = nearest.Quan.Id;
                    MainThread.BeginInvokeOnMainThread(() =>
                        NearestPoiChanged?.Invoke(this, (nearest.Quan.Id, nearest.Distance)));
                }

                // ── Bước 2: Xét các POI trong bán kính Enter, chọn POI ưu tiên nhất ──
                foreach (var item in withDistance)
                {
                    double enterRadius = item.Quan.BanKinhMet > 0 ? item.Quan.BanKinhMet : 50;
                    double nearbyRadius = enterRadius * NearbyRadiusMultiplier;

                    if (item.Distance > nearbyRadius)
                        continue; // Quá xa — bỏ qua hoàn toàn

                    // Kiểm tra cooldown (chống spam cho cả Enter lẫn Nearby)
                    if (_lastTriggered.TryGetValue(item.Quan.Id, out var lastTime) &&
                        DateTime.UtcNow - lastTime < Cooldown)
                        continue;

                    GeofenceTriggerType triggerType;
                    if (item.Distance <= enterRadius)
                    {
                        // Người dùng VÀO vùng POI → Enter → kích hoạt TTS
                        triggerType = GeofenceTriggerType.Enter;
                    }
                    else
                    {
                        // Người dùng ĐẾN GẦN POI (ngoài Enter, trong Nearby) → chỉ notify
                        triggerType = GeofenceTriggerType.Nearby;
                    }

                    // Ghi log và phát sự kiện
                    _lastTriggered[item.Quan.Id] = DateTime.UtcNow;

                    var trigger = new GeofenceTrigger
                    {
                        Quan = item.Quan,
                        DistanceMeters = item.Distance,
                        Type = triggerType
                    };

                    // Invoke trên Main thread để UI update trực tiếp
                    MainThread.BeginInvokeOnMainThread(() =>
                        PoiTriggered?.Invoke(this, trigger));

                    break; // Chỉ kích hoạt 1 POI ưu tiên nhất mỗi chu kỳ
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[GeofenceEngine] Lỗi EvaluatePois: {ex.Message}");
            }
        }

        /// <summary>Reset cooldown của tất cả POI (dùng khi test)</summary>
        public void ResetCooldowns()
        {
            _lastTriggered.Clear();
            _lastNearestId = null;
        }

        // ── Haversine ─────────────────────────────────────────────────

        /// <summary>Tính khoảng cách giữa 2 toạ độ GPS (mét) bằng công thức Haversine</summary>
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
