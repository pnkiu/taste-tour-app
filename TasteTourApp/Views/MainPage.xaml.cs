using TasteTourApp.Models;
using TasteTourApp.Services;

namespace TasteTourApp.Views;

public partial class MainPage : ContentPage
{
    private DatabaseService _dbService = new DatabaseService();
    private List<QuanAn> _danhSachQuan = new();
    private QuanAn? _quanDangChon = null;
    private bool _sheetDangMo = false;

    // TTS (Text-to-Speech)
    private CancellationTokenSource? _ttsCts = null;
    private bool _dangPhatTTS = false;

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
    public MainPage()
    {
        InitializeComponent();

        // FIX: Ẩn sheet ngay khi khởi tạo bằng cách đẩy xuống ngoài màn hình
        TheChiTiet.SizeChanged += OnSheetSizeChanged;

        // Set bottom sheet ở peek height ban đầu
        BottomSheetDanhSach.HeightRequest = _sheetMinHeight;
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
        await LoadDuLieuTuKho();
        _ = GetUserLocationAsync(); // Fire and forget
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
        LblPoiCount.Text = $"{_danhSachQuan.Count} điểm";
    }

    // ============================================================
    //  LẤY VỊ TRÍ NGƯỜI DÙNG (MOCK ĐỂ TEST)
    // ============================================================

    // 🧪 TỌA ĐỘ MẪU — thay đổi tại đây để test các vị trí khác nhau
    // Vị trí này nằm trên đường Vĩnh Khánh, gần các quán ốc
    private const double MOCK_LAT = 10.76185;
    private const double MOCK_LNG = 106.70230;

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

        // Tìm và highlight POI gần nhất
        await HighlightNearestPoi();

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
                ? $"📍 {minDist:F0}m cách bạn"
                : $"📍 {minDist / 1000:F1}km cách bạn";

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

            // Badge "Gần nhất" trên hero
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
                    Text = "⭐ Gần nhất",
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
                    Text = "Vĩnh Khánh",
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

        // Điền dữ liệu
        LblTenQuan.Text = quan.TenQuan;
        LblMoTa.Text = quan.MoTa;
        LblAudioTen.Text = quan.TenQuan;
        LblKhoangCach.Text = _hasUserLocation
            ? $"{TinhKhoangCach(_userLat, _userLng, quan.ViDo, quan.KinhDo):F0}m · Vĩnh Khánh, Q.4"
            : "Vĩnh Khánh, Q.4";
        var (_, currentLangName, _) = GetCurrentTtsLang();
        LblAudioSub.Text = $"{currentLangName} · TTS";
        LblPlayIcon.Text = "▶";
        LblRating.Text = "4.5";

        // Cập nhật hero image theo loại quán
        var (emoji, bgColor, label) = LayThongTinLoai(quan.LoaiQuan ?? "");
        HeroImage.BackgroundColor = Color.FromArgb(bgColor);
        LblLoaiQuan.Text = label;

        if (!string.IsNullOrEmpty(quan.HinhAnh))
        {
            ImgQuan.Source = quan.HinhAnh;
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
    /// Dừng TTS nếu đang phát
    /// </summary>
    private async Task StopTts()
    {
        if (_ttsCts != null)
        {
            _ttsCts.Cancel();
            _ttsCts.Dispose();
            _ttsCts = null;
        }
        _dangPhatTTS = false;
        LblPlayIcon.Text = "▶";

        var (_, langName, _) = GetCurrentTtsLang();
        LblAudioSub.Text = $"{langName} · TTS";
    }

    /// <summary>
    /// Phát TTS nội dung MoTa của POI đang chọn
    /// </summary>
    private async Task PhatTts()
    {
        if (_quanDangChon == null || string.IsNullOrWhiteSpace(_quanDangChon.MoTa)) return;

        // Tạo CancellationToken mới
        _ttsCts = new CancellationTokenSource();
        _dangPhatTTS = true;

        // Lấy ngôn ngữ đã chọn trong Cài đặt
        var (langCode, langName, langPrefix) = GetCurrentTtsLang();

        // Cập nhật UI
        LblPlayIcon.Text = "⏸";
        LblAudioSub.Text = $"▶ Đang phát · {langName}...";

        try
        {
            // Cấu hình giọng đọc
            var options = new SpeechOptions
            {
                Pitch = 1.0f,
                Volume = 1.0f,
            };

            // Tìm locale phù hợp với ngôn ngữ đã chọn
            var locales = await TextToSpeech.GetLocalesAsync();
            var matchedLocale = locales?.FirstOrDefault(l =>
                l.Language.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase));
            if (matchedLocale != null)
            {
                options.Locale = matchedLocale;
            }

            // Đọc nội dung MoTa
            await TextToSpeech.SpeakAsync(_quanDangChon.MoTa, options, _ttsCts.Token);

            // Đọc xong bình thường (không bị cancel)
            if (_dangPhatTTS)
            {
                _dangPhatTTS = false;
                LblPlayIcon.Text = "▶";
                LblAudioSub.Text = $"Đã phát xong · {langName}";
            }
        }
        catch (OperationCanceledException)
        {
            // Bị hủy bởi StopTts() — UI đã reset trong StopTts
        }
        catch (Exception ex)
        {
            _dangPhatTTS = false;
            LblPlayIcon.Text = "▶";
            LblAudioSub.Text = $"Lỗi TTS: {ex.Message}";
        }
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

    private void NavTours_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new TourPage());
        }
    }

    private void NavProfile_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new ProfilePage());
        }
    }
}
