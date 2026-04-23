using Microsoft.Extensions.DependencyInjection;
using TasteTourApp.Models;
using TasteTourApp.Services;
using TasteTourApp.Services.Geofence;
using Microsoft.AspNetCore.SignalR.Client;

#if ANDROID
using Android.Media;
#endif

namespace TasteTourApp.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly DatabaseService _dbService;
        private readonly GeofenceEngine _geofenceEngine;
        private readonly SyncService _syncService;
        private List<QuanAn> _danhSachQuan = new();
        private QuanAn? _quanDangChon = null;
        private bool _sheetDangMo = false;
        private bool _isMapReady = false;
        private HubConnection _hubConnection;

        // TTS (Text-to-Speech)
        private CancellationTokenSource? _ttsCts = null;
        private bool _dangPhatTTS = false;

    // Android MediaPlayer — phát file audio từ server
#if ANDROID
    private Android.Media.MediaPlayer? _mediaPlayer = null;
#endif

    // Bottom sheet drag states
    private double _sheetMinHeight = 180;     // Peek height (chỉ thấy 1-2 cards)
    private double _sheetMaxHeight = 420;     // Expanded height
    private bool _isSheetExpanded = false;

    // User location
    private double _userLat = 0;
    private double _userLng = 0;
    private bool _hasUserLocation = false;
    private string? _nearestPoiId = null;

    // FIX BLACK SCREEN: Đo chiều cao thật của sheet sau khi layout xong
    private double _sheetHeight => DeviceDisplay.MainDisplayInfo.Height
        / DeviceDisplay.MainDisplayInfo.Density * 0.65;

    // Emoji và màu theo loại quán
    private static readonly Dictionary<string, (string emoji, string bg, string label)> _loaiQuanMap = new()
    {
        { "Oc",     ("🦪", "#1B4332", "🦪 Ốc") },
        { "HaiSan", ("🦑", "#1A3A5C", "🦑 Hải sản") },
        { "Sushi",  ("🍱", "#4A1942", "🍱 Sushi") },
    };

    // ============================================================
    //  HTML BẢN ĐỒ LEAFLET (có user location + nearest POI)
    // ============================================================
    private static string TaoHtmlBanDo(List<QuanAn> danhSach)
    {
        var jsGhim = new System.Text.StringBuilder();
        foreach (var q in danhSach)
        {
            var tenEscaped = q.TenQuan.Replace("'", "\\'").Replace("\n", " ");
            jsGhim.AppendLine(
                $"themGhim('{q.Id}', '{tenEscaped}', " +
                $"{q.ViDo.ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                $"{q.KinhDo.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
        }

        double lat = danhSach.Count > 0 ? danhSach[0].ViDo : 10.7619;
        double lng = danhSach.Count > 0 ? danhSach[0].KinhDo : 106.7021;

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
    <style>
        * {{ margin:0; padding:0; box-sizing:border-box; }}
        body {{ background:#f0ede8; }}
        #map {{ width:100vw; height:100vh; }}

        .marker-pin {{
            width: 32px; height: 32px;
            border-radius: 50% 50% 50% 0;
            background: #2D6A4F;
            transform: rotate(-45deg);
            border: 3px solid white;
            box-shadow: 0 2px 8px rgba(0,0,0,0.3);
            transition: all 0.3s ease;
        }}
        .marker-pin::after {{
            content: '';
            width: 12px; height: 12px;
            background: white;
            border-radius: 50%;
            position: absolute;
            top: 50%; left: 50%;
            transform: translate(-50%, -50%);
        }}

        /* Ghim được chọn */
        .marker-pin.selected {{
            background: #FF6F00;
            width: 40px; height: 40px;
            box-shadow: 0 4px 16px rgba(255,111,0,0.5);
        }}

        /* Ghim gần nhất - xanh lá pulse */
        .marker-pin.nearest {{
            background: #00C853;
            width: 38px; height: 38px;
            box-shadow: 0 4px 16px rgba(0,200,83,0.5);
            animation: pulse-green 2s infinite;
        }}
        @keyframes pulse-green {{
            0% {{ box-shadow: 0 0 0 0 rgba(0,200,83,0.5); }}
            70% {{ box-shadow: 0 0 0 14px rgba(0,200,83,0); }}
            100% {{ box-shadow: 0 0 0 0 rgba(0,200,83,0); }}
        }}

        .marker-wrapper {{ width: 40px; height: 50px; }}

        /* User location marker - Chấm xanh dương */
        .user-loc {{
            width: 22px; height: 22px;
            background: #1A73E8;
            border: 4px solid white;
            border-radius: 50%;
            box-shadow: 0 0 0 8px rgba(26,115,232,0.30), 0 2px 8px rgba(0,0,0,0.35);
            animation: u-pulse 2s infinite;
        }}
        @keyframes u-pulse {{
            0% {{ box-shadow: 0 0 0 8px rgba(26,115,232,0.30), 0 2px 8px rgba(0,0,0,0.35); }}
            50% {{ box-shadow: 0 0 0 20px rgba(26,115,232,0.08), 0 2px 8px rgba(0,0,0,0.35); }}
            100% {{ box-shadow: 0 0 0 8px rgba(26,115,232,0.30), 0 2px 8px rgba(0,0,0,0.35); }}
        }}
        .user-loc-wrap {{ width: 30px; height: 30px; }}

        .leaflet-popup-content-wrapper {{
            border-radius: 14px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.15);
            border: none;
        }}
        .leaflet-popup-content {{
            margin: 12px 16px;
            font-family: -apple-system, sans-serif;
        }}
        .popup-ten {{ font-weight: 700; font-size: 14px; color: #1A1A1A; margin-bottom: 2px; }}
        .popup-sub {{ font-size: 11px; color: #2D6A4F; font-weight: 600; }}
        .leaflet-popup-tip {{ background: white; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <script>
        var map = L.map('map', {{ zoomControl: false, attributionControl: false }})
            .setView([{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}], 17);

        L.tileLayer('https://{{s}}.basemaps.cartocdn.com/light_all/{{z}}/{{x}}/{{y}}{{r}}.png', {{
            subdomains: 'abcd', maxZoom: 20
        }}).addTo(map);

        L.control.zoom({{ position: 'topright' }}).addTo(map);

        var allMarkers = {{}};
        var selectedId = null;
        var nearestId = null;
        var userMarker = null;

        function taoIcon(state) {{
            var cls = 'marker-pin';
            if (state === 'selected') cls += ' selected';
            else if (state === 'nearest') cls += ' nearest';
            return L.divIcon({{
                className: 'marker-wrapper',
                html: '<div class=""' + cls + '""></div>',
                iconSize: [40, 50],
                iconAnchor: [20, 50],
                popupAnchor: [0, -54]
            }});
        }}

        function highlightMarker(id) {{
            if (selectedId && allMarkers[selectedId]) {{
                var st = (selectedId === nearestId) ? 'nearest' : 'normal';
                allMarkers[selectedId].setIcon(taoIcon(st));
            }}
            selectedId = id;
            if (allMarkers[id]) {{
                allMarkers[id].setIcon(taoIcon('selected'));
            }}
        }}

        function setNearestMarker(id) {{
            if (nearestId && allMarkers[nearestId] && nearestId !== selectedId) {{
                allMarkers[nearestId].setIcon(taoIcon('normal'));
            }}
            nearestId = id;
            if (allMarkers[id] && id !== selectedId) {{
                allMarkers[id].setIcon(taoIcon('nearest'));
            }}
        }}

        function setUserLocation(lat, lng) {{
            if (userMarker) {{
                userMarker.setLatLng([lat, lng]);
            }} else {{
                var icon = L.divIcon({{
                    className: 'user-loc-wrap',
                    html: '<div class=""user-loc""></div>',
                    iconSize: [24, 24],
                    iconAnchor: [12, 12]
                }});
                userMarker = L.marker([lat, lng], {{ icon: icon, zIndexOffset: 1000 }}).addTo(map);
            }}
        }}

        function centerOnUser(lat, lng) {{
            setUserLocation(lat, lng);
            map.setView([lat, lng], 17, {{ animate: true }});
        }}

        function themGhim(id, ten, lat, lng) {{
            var marker = L.marker([lat, lng], {{ icon: taoIcon('normal') }}).addTo(map);
            marker.bindPopup(
                '<div class=""popup-ten"">' + ten + '</div>' +
                '<div class=""popup-sub"">📍 Vĩnh Khánh, Q.4</div>'
            );
            marker.on('click', function() {{
                highlightMarker(id);
                window.location.href = 'tappin://' + id;
            }});
            allMarkers[id] = marker;
        }}

        {jsGhim}
    </script>
</body>
</html>";
    }

    // ============================================================
    //  CONSTRUCTOR
    // ============================================================
    public MainPage(DatabaseService dbService, GeofenceEngine geofenceEngine, SyncService syncService)
    {
        InitializeComponent();

        _dbService = dbService;
        _geofenceEngine = geofenceEngine;
        _syncService = syncService;
            _hubConnection = new HubConnectionBuilder().WithUrl("http://192.168.1.207:5220/deviceHub") // Hỏi Vũ cái đuôi URL của Hub là gì
                .Build();

            // FIX: Ẩn sheet ngay khi khởi tạo bằng cách đẩy xuống ngoài màn hình
            TheChiTiet.SizeChanged += OnSheetSizeChanged;

        // Set bottom sheet ở peek height ban đầu
        BottomSheetDanhSach.HeightRequest = _sheetMinHeight;
        BanDoWebView.Navigated += BanDoWebView_Navigated;

        }
        // ============================================================
        //  SỰ KIỆN WEBVIEW TẢI XONG HTML
        // ============================================================
        private async void BanDoWebView_Navigated(object? sender, WebNavigatedEventArgs e)
        {
            _isMapReady = true; // Bật cờ cho phép C# giao tiếp với JS

            // Bắn bù tọa độ user nếu C# đã lấy được trước đó
            if (_hasUserLocation)
            {
                //var latStr = _userLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                //var lngStr = _userLng.ToString(System.Globalization.CultureInfo.InvariantCulture);
                //await BanDoWebView.EvaluateJavaScriptAsync($"setUserLocation({latStr}, {lngStr})");

                
                if (_isMapReady)
                {
                    var latStr = _userLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var lngStr = _userLng.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    await BanDoWebView.EvaluateJavaScriptAsync($"setUserLocation({latStr}, {lngStr})");
                }

                // Bắn bù luôn highlight điểm gần nhất
                if (_isMapReady)
                {
                    await BanDoWebView.EvaluateJavaScriptAsync($"setNearestMarker('{_nearestPoiId}')");
                }
            }
        }

        // ── GeofenceEngine event handlers ────────────────────────────────

        /// Tự động phát TTS khi người dùng VÀO vùng POI (Enter), chỉ notify khi Nearby
        private async void OnPoiTriggered(object? sender, GeofenceTrigger trigger)
    {
            System.Diagnostics.Debug.WriteLine($"[Geofence] {trigger.Type}: {trigger.Quan.TenQuan}");

            if (trigger.Type == GeofenceTriggerType.Nearby) return;
            if (_sheetDangMo) return;

            // 👇 BỎ dòng if (_dangPhatTTS) return; đi, thay bằng đoạn này:
            if (_dangPhatTTS)
            {
                // Nếu phát hiện quán mới xịn hơn/hoặc đang trùng lặp, ta TẮT thằng cũ đi
                await StopTts();
                System.Diagnostics.Debug.WriteLine($"[Audio] Đã tắt âm thanh cũ để nhường chỗ cho: {trigger.Quan.TenQuan}");
            }

            _quanDangChon = trigger.Quan;
            await PhatTts();
        }

    /// Cập nhật highlight POI gần nhất trên bản đồ
    private async void OnNearestPoiChanged(object? sender, (string poiId, double distanceMeters) e)
    {
        _nearestPoiId = e.poiId;
        var quan = _danhSachQuan.FirstOrDefault(q => q.Id == e.poiId);
        if (quan == null) return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (_isMapReady)
            {
                await BanDoWebView.EvaluateJavaScriptAsync($"setNearestMarker(\'{e.poiId}\')");
            }
            NearestPoiCard.IsVisible = true;
            LblNearestName.Text = quan.TenQuan;
            LblNearestDistance.Text = e.distanceMeters < 1000
                ? AppLanguage.T($"📍 {e.distanceMeters:F0}m cách bạn", $"📍 {e.distanceMeters:F0}m away")
                : AppLanguage.T($"📍 {e.distanceMeters / 1000:F1}km cách bạn", $"📍 {e.distanceMeters / 1000:F1}km away");
            RenderPoiCards(_danhSachQuan);
        });
    }

    private void OnSheetSizeChanged(object? sender, EventArgs e)
    {
        // Chỉ ẩn lần đầu tiên (khi sheet chưa mở)
        if (!_sheetDangMo && TheChiTiet.Height > 0)
        {
            TheChiTiet.TranslationY = TheChiTiet.Height + 20;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ── Wire events ───────────────────────────────────────────
        _geofenceEngine.PoiTriggered += OnPoiTriggered;
        _geofenceEngine.NearestPoiChanged += OnNearestPoiChanged;
        _syncService.SyncStatusChanged += OnSyncStatusChanged;

        // Áp dụng ngôn ngữ giao diện
        ApplyLanguage();

        // Load dữ liệu local trước (hiển thị ngay lập tức)
        await LoadDuLieuTuKho();
        _ = GetUserLocationAsync(); // Fire and forget

        // Đồng bộ từ API sau (không chặn UI)
        _ = _syncService.SyncAsync();
            try
            {
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                    System.Diagnostics.Debug.WriteLine("[SignalR] Kết nối Hub thành công!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Lỗi kết nối: {ex.Message}");
            }
        }

    // ============================================================
    //  ÁP DỤNG NGÔN NGỮ GIAO DIỆN
    // ============================================================
    private void ApplyLanguage()
    {
        LblSearchPlaceholder.Text   = AppLanguage.T("Tìm quán, địa điểm...", "Search places...");
        LblBottomSheetHeader.Text   = AppLanguage.T("ĐIỂM THUYẾT MINH GẦN BẠN", "NEARBY POINTS OF INTEREST");
        LblNearestLabel.Text        = AppLanguage.T("Gần bạn nhất", "Nearest to you");
        LblChiDuong.Text            = AppLanguage.T("🗺️  Chỉ đường", "🗺️  Directions");
        LblNgheThuyetMinh.Text      = AppLanguage.T("▶  Nghe thuyết minh", "▶  Audio Guide");
    }

    // ============================================================
    //  LOAD DỮ LIỆU
    // ============================================================
    private async Task LoadDuLieuTuKho()
    {
        _danhSachQuan = await _dbService.LayDanhSachQuanAn();
        var html = TaoHtmlBanDo(_danhSachQuan);
        BanDoWebView.Source = new HtmlWebViewSource { Html = html };
        RenderPoiCards(_danhSachQuan);
        LblPoiCount.Text = AppLanguage.IsEnglish
            ? $"{_danhSachQuan.Count} places"
            : $"{_danhSachQuan.Count} điểm";
    }

    // ============================================================
    //  LẤY VỊ TRÍ NGƯỜI DÙNG (MOCK ĐỂ TEST)
    // ============================================================

    // 🧪 TỌA ĐỘ MẪU — thay đổi tại đây để test các vị trí khác nhau
    // Vị trí này nằm trên đường Vĩnh Khánh, gần các quán ốc
    private const double MOCK_LAT = 10.761615; // 10.761615, 106.702392
        private const double MOCK_LNG = 106.702392;

        private async Task GetUserLocationAsync()
    {
        // ── DÙNG TỌA ĐỘ MẪU ĐỂ TEST ──
        _userLat = MOCK_LAT;
        _userLng = MOCK_LNG;
        _hasUserLocation = true;

        // Hiển thị chấm xanh trên bản đồ
        var latStr = _userLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lngStr = _userLng.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await BanDoWebView.EvaluateJavaScriptAsync($"setUserLocation({latStr}, {lngStr})");

        // ── Feed vào GeofenceEngine (thay thế HighlightNearestPoi thủ công) ──
        _geofenceEngine.Start();
        _geofenceEngine.OnLocationUpdated(_userLat, _userLng);

        // Vẫn giữ HighlightNearestPoi để UI update ngay lập tức (không chờ debounce)
        await HighlightNearestPoi();
            try
            {
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    // Lấy device ID thật: GUID được tạo 1 lần và lưu vào Preferences
                    var deviceId = Preferences.Get("device_id", null);
                    if (string.IsNullOrEmpty(deviceId))
                    {
                        deviceId = Guid.NewGuid().ToString("N")[..8].ToUpper(); // 8 ký tự đầu cho gọn
                        Preferences.Set("device_id", deviceId);
                    }
                    var deviceLabel = $"{DeviceInfo.Current.Name} [{deviceId}]";
                    var platform = DeviceInfo.Current.Platform.ToString();

                    await _hubConnection.SendAsync("DeviceJoined", deviceLabel, platform, _userLat, _userLng);
                    System.Diagnostics.Debug.WriteLine($"[SignalR] Đã đăng ký thiết bị: {deviceLabel}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Lỗi gửi tọa độ: {ex.Message}");
            }

            // ── BỎ COMMENT ĐOẠN DƯỚI ĐỂ DÙNG GPS THẬT ──
            // try
            // {
            //     var location = await Geolocation.GetLocationAsync(new GeolocationRequest
            //     {
            //         DesiredAccuracy = GeolocationAccuracy.High,
            //         Timeout = TimeSpan.FromSeconds(10)
            //     });
            //     if (location != null)
            //     {
            //         _userLat = location.Latitude;
            //         _userLng = location.Longitude;
            //         _hasUserLocation = true;
            //         var lat = _userLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            //         var lng = _userLng.ToString(System.Globalization.CultureInfo.InvariantCulture);
            //         await BanDoWebView.EvaluateJavaScriptAsync($"setUserLocation({lat}, {lng})");
            //         await HighlightNearestPoi();
            //     }
            // }
            // catch (Exception ex)
            // {
            //     System.Diagnostics.Debug.WriteLine($"Lỗi GPS: {ex.Message}");
            // }
        }

    // ============================================================
    //  TÌM & HIGHLIGHT POI GẦN NHẤT
    // ============================================================
    private async Task HighlightNearestPoi()
    {
        if (!_hasUserLocation || _danhSachQuan.Count == 0) return;

        QuanAn? nearest = null;
        double minDist = double.MaxValue;

        foreach (var q in _danhSachQuan)
        {
            double dist = TinhKhoangCach(_userLat, _userLng, q.ViDo, q.KinhDo);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = q;
            }
        }

        if (nearest != null)
        {
            _nearestPoiId = nearest.Id;

            // Highlight ghim xanh lá pulse trên bản đồ
            await BanDoWebView.EvaluateJavaScriptAsync($"setNearestMarker('{nearest.Id}')");

            // Hiển thị card nearest POI trong bottom sheet
            NearestPoiCard.IsVisible = true;
            LblNearestName.Text = nearest.TenQuan;
            LblNearestDistance.Text = minDist < 1000
                ? AppLanguage.T($"📍 {minDist:F0}m cách bạn", $"📍 {minDist:F0}m away")
                : AppLanguage.T($"📍 {minDist / 1000:F1}km cách bạn", $"📍 {minDist / 1000:F1}km away");

            // Re-render POI cards để highlight card gần nhất
            RenderPoiCards(_danhSachQuan);
        }
    }

    /// <summary>
    /// Tính khoảng cách Haversine (mét)
    /// </summary>
    private static double TinhKhoangCach(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    // ============================================================
    //  NHẬN SỰ KIỆN TỪ LEAFLET
    // ============================================================
    private async void BanDoWebView_Navigating(object sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("tappin://"))
        {
            e.Cancel = true;
            string idQuan = e.Url.Replace("tappin://", "");
            await MoChiTiet(idQuan);
        }
    }

    // ============================================================
    //  RENDER POI CARDS (horizontal, highlight nearest)
    // ============================================================
    private void RenderPoiCards(List<QuanAn> danhSach)
    {
        PoiCardRow.Children.Clear();

        // Sắp xếp: POI gần nhất lên đầu nếu có vị trí
        var sortedList = _hasUserLocation
            ? danhSach.OrderBy(q => TinhKhoangCach(_userLat, _userLng, q.ViDo, q.KinhDo)).ToList()
            : danhSach;

        for (int i = 0; i < sortedList.Count; i++)
        {
            var quan = sortedList[i];
            var (emoji, bgColor, label) = LayThongTinLoai(quan.LoaiQuan ?? "");
            bool isNearest = quan.Id == _nearestPoiId;
            double distance = _hasUserLocation ? TinhKhoangCach(_userLat, _userLng, quan.ViDo, quan.KinhDo) : -1;

            var card = new Border
            {
                WidthRequest = 155,
                BackgroundColor = isNearest ? Color.FromArgb("#F0FFF4") : Color.FromArgb("#F8F6F3"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                Stroke = isNearest ? Color.FromArgb("#A7F3D0") : Colors.Transparent,
                StrokeThickness = isNearest ? 2 : 0,
            };
            card.Shadow = new Shadow { Brush = Colors.Black, Offset = new Point(0, 2), Radius = 6, Opacity = isNearest ? 0.12f : 0.07f };

            var stack = new VerticalStackLayout();

            // Hero image area
            var hero = new Border { HeightRequest = 85, StrokeThickness = 0, BackgroundColor = Color.FromArgb(bgColor) };
            var heroContent = new Grid();
            heroContent.Children.Add(new Label { Text = emoji, FontSize = 32, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center });

            // Badge "Gần nhất" / "Nearest" trên hero
            if (isNearest)
            {
                var badge = new Border
                {
                    BackgroundColor = Color.FromArgb("#00C853"),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(6, 2),
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Start,
                    Margin = new Thickness(6, 6, 0, 0),
                };
                badge.Content = new Label
                {
                    Text = AppLanguage.T("⭐ Gần nhất", "⭐ Nearest"),
                    FontSize = 8,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                };
                heroContent.Children.Add(badge);
            }

            hero.Content = heroContent;
            stack.Children.Add(hero);

            // Info area
            var info = new VerticalStackLayout { Padding = new Thickness(10, 8, 10, 10), Spacing = 3 };
            info.Children.Add(new Label
            {
                Text = quan.TenQuan,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1A1A1A"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            });

            // Sub info: loại + khoảng cách
            var subRow = new HorizontalStackLayout { Spacing = 4 };
            subRow.Children.Add(new Label
            {
                Text = "📍",
                FontSize = 9,
                VerticalOptions = LayoutOptions.Center,
            });
            if (distance >= 0)
            {
                subRow.Children.Add(new Label
                {
                    Text = distance < 1000 ? $"{distance:F0}m" : $"{distance / 1000:F1}km",
                    FontSize = 10,
                    TextColor = isNearest ? Color.FromArgb("#2D6A4F") : Color.FromArgb("#888888"),
                    FontAttributes = isNearest ? FontAttributes.Bold : FontAttributes.None,
                    VerticalOptions = LayoutOptions.Center,
                });
            }
            else
            {
                subRow.Children.Add(new Label
                {
                    Text = AppLanguage.T("Vĩnh Khánh", "Vinh Khanh"),
                    FontSize = 10,
                    TextColor = Color.FromArgb("#2D6A4F"),
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center,
                });
            }
            info.Children.Add(subRow);

            stack.Children.Add(info);
            card.Content = stack;

            var tapId = quan.Id;
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) => await MoChiTiet(tapId);
            card.GestureRecognizers.Add(tap);
            PoiCardRow.Children.Add(card);
        }
    }

    // ============================================================
    //  BOTTOM SHEET DRAG (PanGesture)
    // ============================================================
    private void BottomSheet_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Kéo lên = TotalY âm -> tăng chiều cao
                double newH = BottomSheetDanhSach.HeightRequest - e.TotalY * 0.25;
                newH = Math.Clamp(newH, _sheetMinHeight, _sheetMaxHeight);
                BottomSheetDanhSach.HeightRequest = newH;
                break;

            case GestureStatus.Completed:
                double midPoint = (_sheetMinHeight + _sheetMaxHeight) / 2;
                if (BottomSheetDanhSach.HeightRequest > midPoint)
                {
                    _isSheetExpanded = true;
                    BottomSheetDanhSach.HeightRequest = _sheetMaxHeight;
                }
                else
                {
                    _isSheetExpanded = false;
                    BottomSheetDanhSach.HeightRequest = _sheetMinHeight;
                }
                break;
        }
    }

    // ============================================================
    //  MỞ SHEET CHI TIẾT
    // ============================================================
    private async Task MoChiTiet(string idQuan)
    {
        var quan = await _dbService.LayQuanAnTheoId(idQuan);
        if (quan == null) return;

        // Dừng TTS nếu đang phát POI trước đó
        await StopTts();

        _quanDangChon = quan;
        _sheetDangMo = true;

        // Điền dữ liệu (theo ngôn ngữ hiện tại)
        LblTenQuan.Text = quan.TenQuan;
        LblMoTa.Text    = AppLanguage.PoiText(quan.MoTa, quan.MoTaEn);
        LblAudioTen.Text = quan.TenQuan;
        string distLabel = AppLanguage.T("Vĩnh Khánh, Q.4", "Vinh Khanh, District 4");
        LblKhoangCach.Text = _hasUserLocation
            ? $"{TinhKhoangCach(_userLat, _userLng, quan.ViDo, quan.KinhDo):F0}m · {distLabel}"
            : distLabel;
        var (_, currentLangName, _) = GetCurrentTtsLang();
        // Xác định nguồn audio hiện có dựa vào ngôn ngữ
        string? activeAudio = AppLanguage.IsEnglish ? quan.AudioContentEn : quan.AudioContent;
        string? fallbackAudio = AppLanguage.IsEnglish ? quan.AudioContent : null; // fallback nếu EN chưa có file
        LblAudioSub.Text = !string.IsNullOrWhiteSpace(activeAudio)
            ? AppLanguage.T("File Audio · MP3", "Audio File · MP3")
            : !string.IsNullOrWhiteSpace(fallbackAudio)
                ? AppLanguage.T("File Audio · MP3", "Audio File · MP3")
                : $"{currentLangName} · TTS";
        LblPlayIcon.Text = "▶";
        LblRating.Text = "4.5";

        // Cập nhật trạng thái tim theo DB
        BtnHeartPoi.Text = quan.IsSaved ? "❤️" : "🤍";

        // Cập nhật hero image theo loại quán
        var (emoji, bgColor, label) = LayThongTinLoai(quan.LoaiQuan ?? "");
        HeroImage.BackgroundColor = Color.FromArgb(bgColor);
        LblLoaiQuan.Text = label;

        if (!string.IsNullOrEmpty(quan.HinhAnh))
        {
            // HinhAnh là path tương đối "/uploads/pois/xxx.jpg" (từ web)
            // hoặc URL đầy đủ — cần build URL đầy đủ để MAUI load được
            string imageUrl = quan.HinhAnh.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? quan.HinhAnh
                : $"http://192.168.1.207:5220{quan.HinhAnh}";

            ImgQuan.Source = ImageSource.FromUri(new Uri(imageUrl));
            ImgQuan.IsVisible = true;
        }
        else
        {
            ImgQuan.IsVisible = false;
        }

        // Highlight marker trên bản đồ
        await BanDoWebView.EvaluateJavaScriptAsync($"highlightMarker('{idQuan}')");

        // FIX: Đảm bảo sheet nằm dưới màn hình trước khi animate
        double sheetH = TheChiTiet.Height > 0 ? TheChiTiet.Height + 20 : _sheetHeight;
        if (TheChiTiet.TranslationY < sheetH / 2)
            TheChiTiet.TranslationY = sheetH;

        // Ẩn danh sách + FAB
        await Task.WhenAll(
            BottomSheetDanhSach.FadeTo(0, 150),
            FabLocateMe.FadeTo(0, 150)
        );
        BottomSheetDanhSach.IsVisible = false;
        FabLocateMe.IsVisible = false;

        // Trượt sheet lên
        await TheChiTiet.TranslateTo(0, 0, 350, Easing.CubicOut);
    }

    // ============================================================
    //  ĐÓNG SHEET
    // ============================================================
    private async void BtnDong_Tapped(object sender, EventArgs e)
    {
        _sheetDangMo = false;

        // Tự động dừng TTS khi đóng thẻ chi tiết
        await StopTts();

        double sheetH = TheChiTiet.Height > 0 ? TheChiTiet.Height + 20 : _sheetHeight;
        await TheChiTiet.TranslateTo(0, sheetH, 280, Easing.CubicIn);

        // Reset highlight ghim trên bản đồ
        await BanDoWebView.EvaluateJavaScriptAsync("highlightMarker('')");

        // Hiện lại bottom sheet + FAB
        BottomSheetDanhSach.IsVisible = true;
        FabLocateMe.IsVisible = true;
        await Task.WhenAll(
            BottomSheetDanhSach.FadeTo(1, 200),
            FabLocateMe.FadeTo(1, 200)
        );
    }

    // ============================================================
    //  NÚT ĐỊNH VỊ (FAB)
    // ============================================================
    private async void BtnLocateMe_Tapped(object sender, EventArgs e)
    {
        // 🧪 MOCK: Dùng tọa độ mẫu để test
        _userLat = MOCK_LAT;
        _userLng = MOCK_LNG;
        _hasUserLocation = true;

        var latStr = _userLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lngStr = _userLng.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await BanDoWebView.EvaluateJavaScriptAsync($"centerOnUser({latStr}, {lngStr})");

        // Reset cooldown để test lại geofence ngay lập tức
        _geofenceEngine.ResetCooldowns();
        _geofenceEngine.OnLocationUpdated(_userLat, _userLng);

        await HighlightNearestPoi();
    }

    // ============================================================
    //  NEAREST POI CARD TAP
    // ============================================================
    private async void NearestPoi_Tapped(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_nearestPoiId))
        {
            await MoChiTiet(_nearestPoiId);
        }
    }

    // ============================================================
    //  HELPER
    // ============================================================
    private static (string emoji, string bg, string label) LayThongTinLoai(string loai)
    {
        return _loaiQuanMap.TryGetValue(loai, out var v) ? v : ("🍴", "#2D3A2E", "Quán ăn");
    }

    // ============================================================
    //  TEXT-TO-SPEECH
    // ============================================================

    // Mapping ngôn ngữ code → (tên hiển thị, language prefix cho locale)
    private static readonly Dictionary<string, (string name, string langPrefix)> _ttsLangMap = new()
    {
        { "vi", ("Tiếng Việt", "vi") },
        { "en", ("English", "en") },
        { "ko", ("한국어", "ko") },
        { "zh", ("中文", "zh") },
    };

    /// <summary>
    /// Lấy tên ngôn ngữ TTS hiện tại từ Preferences
    /// </summary>
    private static (string code, string name, string langPrefix) GetCurrentTtsLang()
    {
        string code = Preferences.Get("tts_language", "vi");
        if (_ttsLangMap.TryGetValue(code, out var info))
            return (code, info.name, info.langPrefix);
        return ("vi", "Tiếng Việt", "vi");
    }

    /// <summary>
    /// Dừng toàn bộ audio (cả file lẫn TTS) đang phát
    /// </summary>
    private async Task StopTts()
    {
        // Dừng MediaPlayer file audio nếu đang phát
#if ANDROID
        if (_mediaPlayer != null)
        {
            try { _mediaPlayer.Stop(); } catch { }
            _mediaPlayer.Release();
            _mediaPlayer = null;
        }
#endif

        // Dừng TTS nếu đang phát
        if (_ttsCts != null)
        {
            _ttsCts.Cancel();
            _ttsCts.Dispose();
            _ttsCts = null;
        }
        _dangPhatTTS = false;
        LblPlayIcon.Text = "▶";

        var (_, langName, _) = GetCurrentTtsLang();
        LblAudioSub.Text = AppLanguage.T($"{langName} · TTS", $"{langName} · TTS");
    }

    /// <summary>
    /// Phát audio: ưu tiên file audio từ server (theo ngôn ngữ), fallback TTS nếu không có
    /// </summary>
    private async Task PhatTts()
    {
        if (_quanDangChon == null) return;

        _dangPhatTTS = true;
        LblPlayIcon.Text = "⏸";

        // ── TRƯỜNG HỢP 1: Có file audio theo ngôn ngữ hiện tại ──
        string? audioByLang = AppLanguage.IsEnglish
            ? _quanDangChon.AudioContentEn
            : _quanDangChon.AudioContent;

        if (!string.IsNullOrWhiteSpace(audioByLang))
        {
            await PhatFileAudio(audioByLang);
            return;
        }

        // ── TRƯỜNG HỢP 1b: Fallback sang audio ngôn ngữ kia nếu chưa có bản EN ──
        string? audioFallback = AppLanguage.IsEnglish
            ? _quanDangChon.AudioContent  // EN không có → dùng VI
            : null;

        if (!string.IsNullOrWhiteSpace(audioFallback))
        {
            await PhatFileAudio(audioFallback);
            return;
        }

        // ── TRƯỜNG HỢP 2: Không có file audio → fallback TTS từ mô tả ──
        string? ttsText = AppLanguage.PoiText(_quanDangChon.MoTa, _quanDangChon.MoTaEn);
        if (string.IsNullOrWhiteSpace(ttsText))
        {
            _dangPhatTTS = false;
            LblPlayIcon.Text = "▶";
            LblAudioSub.Text = AppLanguage.T("Chưa có nội dung audio", "No audio content available");
            return;
        }

        var (langCode, langName, langPrefix) = GetCurrentTtsLang();
        LblAudioSub.Text = AppLanguage.T($"▶ Đang đọc · {langName}...", $"▶ Reading · {langName}...");

        _ttsCts = new CancellationTokenSource();
        try
        {
            var options = new SpeechOptions { Pitch = 1.0f, Volume = 1.0f };
            var locales = await TextToSpeech.GetLocalesAsync();
            var matchedLocale = locales?.FirstOrDefault(l =>
                l.Language.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase));
            if (matchedLocale != null) options.Locale = matchedLocale;

            // Dùng nội dung theo ngôn ngữ hiện tại
            await TextToSpeech.SpeakAsync(ttsText!, options, _ttsCts.Token);

            if (_dangPhatTTS)
            {
                _dangPhatTTS = false;
                LblPlayIcon.Text = "▶";
                LblAudioSub.Text = AppLanguage.T($"Đã phát xong · {langName}", $"Done · {langName}");
            }
        }
        catch (OperationCanceledException) { /* bị hủy bởi StopTts */ }
        catch (Exception ex)
        {
            _dangPhatTTS = false;
            LblPlayIcon.Text = "▶";
            LblAudioSub.Text = AppLanguage.T($"Lỗi TTS: {ex.Message}", $"TTS error: {ex.Message}");
        }
    }

        /// <summary>
        /// Phát file audio URL từ server qua Android MediaPlayer
        /// </summary>
        /// <summary>
        /// Phát file audio URL từ server qua Android MediaPlayer
        /// </summary>
        private async Task PhatFileAudio(string audioPath)
        {
            string baseUrl = "http://192.168.1.207:5220";

            // 1. DỌN DẸP LINK: Cắt bỏ khoảng trắng thừa, thay khoảng trắng giữa chữ thành %20
            string cleanPath = audioPath.Trim().Replace(" ", "%20");
            string audioUrl = cleanPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? cleanPath
                : $"{baseUrl}/{cleanPath.TrimStart('/')}";

            System.Diagnostics.Debug.WriteLine($"[TEST AUDIO URL]: Đang kéo file từ -> {audioUrl}");
            LblAudioSub.Text = "▶ Đang chuẩn bị audio...";

#if ANDROID
            var tcs = new TaskCompletionSource<bool>();

            // BẮT BUỘC: Phải gọi Android.Media.MediaPlayer trên Main UI Thread
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    // Dọn player cũ an toàn
                    if (_mediaPlayer != null)
                    {
                        try { if (_mediaPlayer.IsPlaying) _mediaPlayer.Stop(); } catch { }
                        _mediaPlayer.Reset();
                        _mediaPlayer.Release();
                        _mediaPlayer = null;
                    }

                    _mediaPlayer = new Android.Media.MediaPlayer();
                    _mediaPlayer.SetAudioAttributes(
                        new Android.Media.AudioAttributes.Builder()
                            .SetContentType(Android.Media.AudioContentType.Music)
                            .SetUsage(Android.Media.AudioUsageKind.Media)
                            .Build());

                    // 2. DÙNG URI THAY VÌ STRING: Giúp Android phân giải mạng ổn định hơn
                    var uri = Android.Net.Uri.Parse(audioUrl);
                    await _mediaPlayer.SetDataSourceAsync(Android.App.Application.Context, uri);

                    _mediaPlayer.Prepared += (s, e) =>
                    {
                        LblAudioSub.Text = "▶ Đang phát audio...";
                        _mediaPlayer.Start();
                    };

                    _mediaPlayer.Completion += (s, e) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _dangPhatTTS = false;
                            LblPlayIcon.Text = "▶";
                            LblAudioSub.Text = AppLanguage.T("Đã phát xong", "Playback complete");
                        });
                        tcs.TrySetResult(true);
                    };

                    _mediaPlayer.Error += (s, e) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _dangPhatTTS = false;
                            LblPlayIcon.Text = "▶";
                            LblAudioSub.Text = AppLanguage.T($"Lỗi phát audio ({e.What})", $"Audio error ({e.What})");
                        });
                        tcs.TrySetResult(false);
                    };

                    _mediaPlayer.PrepareAsync(); // Bắt đầu buffer mạng
                }
                catch (Exception innerEx)
                {
                    _dangPhatTTS = false;
                    LblPlayIcon.Text = "▶";
                    LblAudioSub.Text = $"Lỗi Setup: {innerEx.Message}";
                    tcs.TrySetResult(false);
                }
            });

            await tcs.Task;
#else
        // Fallback cho các platform khác: dùng TTS
        _dangPhatTTS = false;
        LblAudioSub.Text = "(Audio chỉ hỗ trợ trên Android)";
        LblPlayIcon.Text = "▶";
        await Task.CompletedTask;
#endif
        }

        // ============================================================
        //  CÁC NÚT
        // ============================================================
        private async void BtnPlay_Tapped(object sender, EventArgs e)
    {
        if (_quanDangChon == null) return;

        if (_dangPhatTTS)
        {
            // Đang phát → dừng
            await StopTts();
        }
        else
        {
            // Chưa phát → bắt đầu đọc MoTa
            await PhatTts();
        }
    }

    private async void BtnNgheThuyetMinh_Tapped(object sender, EventArgs e)
    {
        BtnPlay_Tapped(sender, e);
    }

    private async void BtnChiDuong_Tapped(object sender, EventArgs e)
    {
        if (_quanDangChon == null) return;
        var url = $"https://www.google.com/maps/dir/?api=1" +
                  $"&destination={_quanDangChon.ViDo.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $",{_quanDangChon.KinhDo.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&travelmode=walking";
        await Launcher.OpenAsync(url);
    }

    private void NavDanhSach_Tapped(object sender, EventArgs e)
    {
        if (_sheetDangMo) BtnDong_Tapped(sender, e);
    }

    private void NavSaved_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new SavedPage(_dbService, _geofenceEngine, _syncService));
        }
    }

    private void NavTours_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new TourPage(_dbService, _geofenceEngine, _syncService));
        }
    }

    private void NavProfile_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            var deviceService = IPlatformApplication.Current.Services.GetService<DeviceService>();
            Application.Current.MainPage = new NavigationPage(new ProfilePage(_dbService, _geofenceEngine, _syncService, deviceService));
        }
    }

    // ============================================================
    //  NÚT TIM (YÊU THÍCH)
    // ============================================================
    private async void BtnHeartPoi_Tapped(object sender, EventArgs e)
    {
        if (_quanDangChon == null) return;

        // Toggle trạng thái
        _quanDangChon.IsSaved = !_quanDangChon.IsSaved;

        // Lưu vào DB
        await _dbService.LuuYeuThich(_quanDangChon.Id, _quanDangChon.IsSaved);

        // Cập nhật icon tim với animation nhỏ
        BtnHeartPoi.Text = _quanDangChon.IsSaved ? "❤️" : "🤍";
        await BtnHeartPoi.ScaleTo(1.3, 100);
        await BtnHeartPoi.ScaleTo(1.0, 100);
    }

        // ============================================================
        //  SYNC SERVICE HANDLERS
        // ============================================================
        private async void OnSyncStatusChanged(object? sender, SyncResult result)
        {
            await ShowSyncBadge(result);

            // Nếu sync thành công → reload danh sách và bản đồ
            if (result.IsSuccess)
            {
                await LoadDuLieuTuKho();
                // Cập nhật lại khoảng cách gần nhất nếu đã có GPS
                if (_hasUserLocation)
                    await HighlightNearestPoi();
            }
        }

        /// <summary>
        /// Hiển thị badge trạng thái sync và tự ẩn sau 3 giây khi thành công.
        /// </summary>
        private async Task ShowSyncBadge(SyncResult result)
        {
            // Cập nhật màu sắc và text theo trạng thái
            (string bgColor, string strokeColor, string textColor) = result.Status switch
            {
                SyncStatus.Syncing => ("#F0FFF4", "#A7F3D0", "#2D6A4F"),
                SyncStatus.Success => ("#F0FFF4", "#A7F3D0", "#1B7340"),
                SyncStatus.Offline => ("#FFF8E1", "#FFE082", "#8B6914"),
                SyncStatus.Error   => ("#FFF0F0", "#FFCDD2", "#C62828"),
                _                  => ("#F0FFF4", "#A7F3D0", "#2D6A4F")
            };

            SyncStatusBadge.BackgroundColor = Color.FromArgb(bgColor);
            SyncStatusBadge.Stroke = Color.FromArgb(strokeColor);
            LblSyncStatus.TextColor = Color.FromArgb(textColor);
            LblSyncStatus.Text = result.StatusText;
            SyncStatusBadge.IsVisible = true;
            SyncStatusBadge.Opacity = 0;
            await SyncStatusBadge.FadeTo(1, 200);

            // Nút sync: disable khi đang sync, enable sau
            BtnSyncManual.Opacity = result.Status == SyncStatus.Syncing ? 0.4 : 1.0;
            LblSyncIcon.Text = result.Status == SyncStatus.Syncing ? "⏳" : "↻";

            // Tự ẩn sau 3 giây nếu không phải trạng thái lỗi
            if (result.Status != SyncStatus.Syncing && result.Status != SyncStatus.Error)
            {
                await Task.Delay(3000);
                await SyncStatusBadge.FadeTo(0, 400);
                SyncStatusBadge.IsVisible = false;
            }
        }

        private async void BtnSyncManual_Tapped(object sender, EventArgs e)
        {
            // Không cho phép nhấn lại khi đang sync
            if (BtnSyncManual.Opacity < 1.0) return;

            // Animation nhỏ
            await BtnSyncManual.ScaleTo(0.9, 80);
            await BtnSyncManual.ScaleTo(1.0, 80);

            _ = _syncService.SyncAsync();
        }

        // ── Hủy đăng ký sự kiện khi trang bị đóng/chuyển (chống memory leak) ──
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _geofenceEngine.PoiTriggered -= OnPoiTriggered;
            _geofenceEngine.NearestPoiChanged -= OnNearestPoiChanged;
            _syncService.SyncStatusChanged -= OnSyncStatusChanged;
        }
    }
}
