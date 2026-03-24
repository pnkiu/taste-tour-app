using TasteTourApp.Models;
using TasteTourApp.Services;

namespace TasteTourApp.Views;

public partial class MainPage : ContentPage
{
    private DatabaseService _dbService = new DatabaseService();
    private List<QuanAn> _danhSachQuan = new();
    private QuanAn? _quanDangChon = null;
    private bool _sheetDangMo = false;

    // FIX BLACK SCREEN: Đo chiều cao thật của sheet sau khi layout xong
    // Thay vì dùng số cứng 480, dùng chiều cao màn hình
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
    //  HTML BẢN ĐỒ LEAFLET
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
            transition: all 0.2s ease;
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

        /* HIGHLIGHT: ghim được chọn to hơn và màu cam */
        .marker-pin.selected {{
            background: #FF6F00;
            width: 40px; height: 40px;
            box-shadow: 0 4px 16px rgba(255,111,0,0.5);
        }}
        .marker-wrapper {{ width: 40px; height: 50px; }}

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

        function taoIcon(isSelected) {{
            return L.divIcon({{
                className: 'marker-wrapper',
                html: '<div class=""marker-pin' + (isSelected ? ' selected' : '') + '""></div>',
                iconSize: [40, 50],
                iconAnchor: [20, 50],
                popupAnchor: [0, -54]
            }});
        }}

        // Hàm highlight ghim được chọn, reset ghim cũ
        function highlightMarker(id) {{
            if (selectedId && allMarkers[selectedId]) {{
                allMarkers[selectedId].setIcon(taoIcon(false));
            }}
            selectedId = id;
            if (allMarkers[id]) {{
                allMarkers[id].setIcon(taoIcon(true));
            }}
        }}

        function themGhim(id, ten, lat, lng) {{
            var marker = L.marker([lat, lng], {{ icon: taoIcon(false) }}).addTo(map);
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
        // Dùng SizeChanged để lấy chiều cao thật thay vì số cứng
        TheChiTiet.SizeChanged += OnSheetSizeChanged;
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
    //  RENDER POI CARDS
    // ============================================================
    private void RenderPoiCards(List<QuanAn> danhSach)
    {
        PoiCardRow.Children.Clear();

        for (int i = 0; i < danhSach.Count; i++)
        {
            var quan = danhSach[i];
            var (emoji, bgColor, label) = LayThongTinLoai(quan.LoaiQuan ?? "");

            var card = new Border
            {
                WidthRequest = 145,
                BackgroundColor = Color.FromArgb("#F8F6F3"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 0,
            };
            card.Shadow = new Shadow { Brush = Colors.Black, Offset = new Point(0, 2), Radius = 6, Opacity = 0.07f };

            var stack = new VerticalStackLayout();

            var hero = new Border { HeightRequest = 85, StrokeThickness = 0, BackgroundColor = Color.FromArgb(bgColor) };
            hero.Content = new Label { Text = emoji, FontSize = 32, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            stack.Children.Add(hero);

            var info = new VerticalStackLayout { Padding = new Thickness(10, 8, 10, 10), Spacing = 4 };
            info.Children.Add(new Label
            {
                Text = quan.TenQuan,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1A1A1A"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            });
            info.Children.Add(new Label
            {
                Text = "📍 Vĩnh Khánh",
                FontSize = 10,
                TextColor = Color.FromArgb("#2D6A4F"),
                FontAttributes = FontAttributes.Bold
            });
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
    //  MỞ SHEET CHI TIẾT
    //  FIX: Dùng chiều cao thật của sheet để TranslationY đúng
    // ============================================================
    private async Task MoChiTiet(string idQuan)
    {
        var quan = await _dbService.LayQuanAnTheoId(idQuan);
        if (quan == null) return;

        _quanDangChon = quan;
        _sheetDangMo = true;

        // Điền dữ liệu
        LblTenQuan.Text = quan.TenQuan;
        LblMoTa.Text = quan.MoTa;
        LblAudioTen.Text = quan.TenQuan;
        LblKhoangCach.Text = "Vĩnh Khánh, Q.4";
        LblAudioSub.Text = "Tiếng Việt · TTS";
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

        // FIX: Đảm bảo sheet nằm dưới màn hình trước khi animate
        // Dùng chiều cao thật của sheet (sau khi đã được layout)
        double sheetH = TheChiTiet.Height > 0 ? TheChiTiet.Height + 20 : _sheetHeight;
        if (TheChiTiet.TranslationY < sheetH / 2)
            TheChiTiet.TranslationY = sheetH;

        // Ẩn danh sách
        await BottomSheetDanhSach.FadeTo(0, 150);
        BottomSheetDanhSach.IsVisible = false;

        // Trượt sheet lên
        await TheChiTiet.TranslateTo(0, 0, 350, Easing.CubicOut);
    }

    // ============================================================
    //  ĐÓNG SHEET
    // ============================================================
    private async void BtnDong_Tapped(object sender, EventArgs e)
    {
        _sheetDangMo = false;

        double sheetH = TheChiTiet.Height > 0 ? TheChiTiet.Height + 20 : _sheetHeight;
        await TheChiTiet.TranslateTo(0, sheetH, 280, Easing.CubicIn);

        // Reset highlight ghim trên bản đồ
        await BanDoWebView.EvaluateJavaScriptAsync("highlightMarker('')");

        BottomSheetDanhSach.IsVisible = true;
        await BottomSheetDanhSach.FadeTo(1, 200);
    }

    // ============================================================
    //  HELPER
    // ============================================================
    private static (string emoji, string bg, string label) LayThongTinLoai(string loai)
    {
        return _loaiQuanMap.TryGetValue(loai, out var v) ? v : ("🍴", "#2D3A2E", "Quán ăn");
    }

    // ============================================================
    //  CÁC NÚT
    // ============================================================
    private void BtnPlay_Tapped(object sender, EventArgs e)
    {
        if (_quanDangChon == null) return;
        LblAudioSub.Text = "▶ Đang phát... (TTS sẽ tích hợp sau)";
        LblPlayIcon.Text = "⏸";
    }

    private void BtnNgheThuyetMinh_Tapped(object sender, EventArgs e) => BtnPlay_Tapped(sender, e);

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

    private void BtnAudio_Tapped(object sender, EventArgs e)
    {
        if (_quanDangChon != null) BtnPlay_Tapped(sender, e);
    }
}
