using TasteTourApp.Models;
using TasteTourApp.Services;

namespace TasteTourApp.Views;

public partial class MainPage : ContentPage
{
    private DatabaseService _dbService = new DatabaseService();
    private List<QuanAn> _danhSachQuan = new();
    private QuanAn? _quanDangChon = null;
    private bool _sheetDangMo = false;
    private const double SHEET_HEIGHT = 480;

    // ============================================================
    //  HTML BẢN ĐỒ LEAFLET — nhúng thẳng vào C#, không cần file .html
    // ============================================================
    private static string TaoHtmlBanDo(List<QuanAn> danhSach)
    {
        // Tạo JS để cắm ghim cho từng quán
        var jsGhim = new System.Text.StringBuilder();
        foreach (var q in danhSach)
        {
            // Escape tên quán để tránh lỗi JS
            var tenEscaped = q.TenQuan.Replace("'", "\\'").Replace("\n", " ");
            jsGhim.AppendLine($@"
                themGhim('{q.Id}', '{tenEscaped}', {q.ViDo.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {q.KinhDo.ToString(System.Globalization.CultureInfo.InvariantCulture)});
            ");
        }

        // Tọa độ trung tâm = quán đầu tiên
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

        /* Custom marker */
        .marker-pin {{
            width: 36px;
            height: 36px;
            border-radius: 50% 50% 50% 0;
            background: #2D6A4F;
            transform: rotate(-45deg);
            border: 3px solid white;
            box-shadow: 0 2px 8px rgba(0,0,0,0.3);
        }}
        .marker-pin::after {{
            content: '';
            width: 14px;
            height: 14px;
            background: white;
            border-radius: 50%;
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
        }}
        .marker-wrapper {{
            width: 36px;
            height: 44px;
        }}

        /* Popup style */
        .leaflet-popup-content-wrapper {{
            border-radius: 14px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.15);
            border: none;
        }}
        .leaflet-popup-content {{
            margin: 12px 16px;
            font-family: -apple-system, sans-serif;
        }}
        .popup-ten {{
            font-weight: 700;
            font-size: 14px;
            color: #1A1A1A;
            margin-bottom: 2px;
        }}
        .popup-sub {{
            font-size: 11px;
            color: #2D6A4F;
            font-weight: 600;
        }}
        .leaflet-popup-tip {{
            background: white;
        }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <script>
        // Khởi tạo bản đồ
        var map = L.map('map', {{
            zoomControl: false,
            attributionControl: false
        }}).setView([{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}], 17);

        // Tile Carto Light — đẹp, tối giản, phù hợp app du lịch
        L.tileLayer('https://{{s}}.basemaps.cartocdn.com/light_all/{{z}}/{{x}}/{{y}}{{r}}.png', {{
            subdomains: 'abcd',
            maxZoom: 20
        }}).addTo(map);

        // Thêm nút zoom ở góc phải
        L.control.zoom({{ position: 'topright' }}).addTo(map);

        // Custom icon marker
        function taoIcon() {{
            return L.divIcon({{
                className: 'marker-wrapper',
                html: '<div class=""marker-pin""></div>',
                iconSize: [36, 44],
                iconAnchor: [18, 44],
                popupAnchor: [0, -48]
            }});
        }}

        // Hàm thêm ghim — được gọi từ C#
        function themGhim(id, ten, lat, lng) {{
            var marker = L.marker([lat, lng], {{ icon: taoIcon() }}).addTo(map);
            marker.bindPopup(
                '<div class=""popup-ten"">' + ten + '</div>' +
                '<div class=""popup-sub"">📍 Vĩnh Khánh, Q.4</div>'
            );
            marker.on('click', function() {{
                // Gửi ID về C# qua URL scheme
                window.location.href = 'tappin://' + id;
            }});
        }}

        // Cắm ghim từ dữ liệu C#
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
        TheChiTiet.TranslationY = SHEET_HEIGHT;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDuLieuTuKho();
    }

    // ============================================================
    //  LOAD DỮ LIỆU & KHỞI TẠO BẢN ĐỒ
    // ============================================================
    private async Task LoadDuLieuTuKho()
    {
        _danhSachQuan = await _dbService.LayDanhSachQuanAn();

        // Tạo HTML với dữ liệu thật và gán vào WebView
        var html = TaoHtmlBanDo(_danhSachQuan);
        BanDoWebView.Source = new HtmlWebViewSource { Html = html };

        // Render POI cards ở bottom sheet
        RenderPoiCards(_danhSachQuan);
    }

    // ============================================================
    //  NHẬN SỰ KIỆN TỪ LEAFLET (bấm marker)
    //  Leaflet gửi về C# qua URL scheme: tappin://VK_01
    // ============================================================
    private async void BanDoWebView_Navigating(object sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("tappin://"))
        {
            // Chặn navigation thật
            e.Cancel = true;

            // Lấy ID từ URL
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

        string[] emojis = { "🦪", "🦑", "🍱" };
        string[] bgColors = { "#1B4332", "#1A3A5C", "#4A1942" };

        for (int i = 0; i < danhSach.Count; i++)
        {
            var quan = danhSach[i];
            string emoji = i < emojis.Length ? emojis[i] : "📍";
            string bgColor = bgColors[i % bgColors.Length];

            var card = new Border
            {
                WidthRequest = 145,
                BackgroundColor = Color.FromArgb("#F8F6F3"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 0,
            };
            card.Shadow = new Shadow
            {
                Brush = Colors.Black,
                Offset = new Point(0, 2),
                Radius = 6,
                Opacity = 0.07f
            };

            var stack = new VerticalStackLayout();
            var hero = new Border
            {
                HeightRequest = 85,
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb(bgColor),
            };
            hero.Content = new Label
            {
                Text = emoji,
                FontSize = 32,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
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
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await MoChiTiet(tapId);
            card.GestureRecognizers.Add(tapGesture);

            PoiCardRow.Children.Add(card);
        }
    }

    // ============================================================
    //  MỞ SHEET CHI TIẾT
    // ============================================================
    private async Task MoChiTiet(string idQuan)
    {
        var quan = await _dbService.LayQuanAnTheoId(idQuan);
        if (quan == null) return;

        _quanDangChon = quan;
        _sheetDangMo = true;

        LblTenQuan.Text = quan.TenQuan;
        LblMoTa.Text = quan.MoTa;
        LblAudioTen.Text = quan.TenQuan;
        LblKhoangCach.Text = "Vĩnh Khánh, Q.4";
        LblAudioSub.Text = "Tiếng Việt · TTS";
        LblPlayIcon.Text = "▶";

        if (TheChiTiet.TranslationY < SHEET_HEIGHT / 2)
            TheChiTiet.TranslationY = SHEET_HEIGHT;

        await BottomSheetDanhSach.FadeTo(0, 150);
        BottomSheetDanhSach.IsVisible = false;

        await TheChiTiet.TranslateTo(0, 0, 350, Easing.CubicOut);
    }

    // ============================================================
    //  ĐÓNG SHEET
    // ============================================================
    private async void BtnDong_Tapped(object sender, EventArgs e)
    {
        _sheetDangMo = false;
        await TheChiTiet.TranslateTo(0, SHEET_HEIGHT, 280, Easing.CubicIn);
        BottomSheetDanhSach.IsVisible = true;
        await BottomSheetDanhSach.FadeTo(1, 200);
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
        var url = $"https://www.google.com/maps/dir/?api=1&destination={_quanDangChon.ViDo.ToString(System.Globalization.CultureInfo.InvariantCulture)},{_quanDangChon.KinhDo.ToString(System.Globalization.CultureInfo.InvariantCulture)}&travelmode=walking";
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
