using TasteTourApp.Models;
using TasteTourApp.Services;
using TasteTourApp.Services.Geofence;

namespace TasteTourApp.Views;

public partial class SavedPage : ContentPage
{
    private readonly DatabaseService _dbService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly SyncService _syncService;
    private List<QuanAn> _allSaved = new();

    // Emoji và màu theo loại quán (giữ đồng bộ với MainPage)
    private static readonly Dictionary<string, (string emoji, string bg, string label)> _loaiQuanMap = new()
    {
        { "Oc",     ("🦪", "#1B4332", "🦪 Ốc") },
        { "HaiSan", ("🦑", "#1A3A5C", "🦑 Hải sản") },
        { "Sushi",  ("🍱", "#4A1942", "🍱 Sushi") },
    };

    public SavedPage(DatabaseService dbService, GeofenceEngine geofenceEngine, SyncService syncService)
    {
        InitializeComponent();
        _dbService = dbService;
        _geofenceEngine = geofenceEngine;
        _syncService = syncService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSavedItems();
    }

    // ============================================================
    //  LOAD DỮ LIỆU YÊU THÍCH
    // ============================================================
    private async Task LoadSavedItems()
    {
        _allSaved = await _dbService.LayDanhSachYeuThich();
        RenderSavedCards(_allSaved);
        LblSavedCount.Text = $"{_allSaved.Count} địa điểm";
    }

    // ============================================================
    //  RENDER CARDS (code-behind, tương tự RenderPoiCards ở MainPage)
    // ============================================================
    private void RenderSavedCards(List<QuanAn> list)
    {
        SavedCardStack.Children.Clear();

        if (list.Count == 0)
        {
            EmptyView.IsVisible = true;
            return;
        }

        EmptyView.IsVisible = false;

        foreach (var quan in list)
        {
            var (emoji, bgColor, loaiLabel) = _loaiQuanMap.TryGetValue(quan.LoaiQuan ?? "", out var v)
                ? v
                : ("🍴", "#2D3A2E", "🍴 Quán ăn");

            // ── Card wrapper ──────────────────────────────────────────
            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 0,
            };
            card.Shadow = new Shadow
            {
                Brush = Colors.Black, Offset = new Point(0, 4),
                Radius = 12, Opacity = 0.05f
            };

            // ── Layout: ảnh trái | thông tin phải ────────────────────
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(
                new ColumnDefinition { Width = 100 },
                new ColumnDefinition { Width = GridLength.Star }
            )};

            // ── Cột trái: hero image / emoji ──────────────────────────
            var heroBorder = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb(bgColor),
                HeightRequest = 120,
            };
            // Custom shape chỉ bo góc trái
            heroBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(16, 0, 16, 0)
            };

            var heroGrid = new Grid();

            // Ảnh nếu có
            if (!string.IsNullOrEmpty(quan.HinhAnh))
            {
                // HinhAnh là path tương đối "/uploads/pois/xxx.jpg" từ web → cần URL đầy đủ
                string imageUrl = quan.HinhAnh.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? quan.HinhAnh
                    : $"http://10.0.2.2:5220{quan.HinhAnh}";

                heroGrid.Children.Add(new Image
                {
                    Source = ImageSource.FromUri(new Uri(imageUrl)),
                    Aspect = Aspect.AspectFill,
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalOptions = LayoutOptions.Fill
                });
            }
            else
            {
                // Emoji fallback
                heroGrid.Children.Add(new Label
                {
                    Text = emoji,
                    FontSize = 36,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                });
            }
            heroBorder.Content = heroGrid;
            grid.Children.Add(heroBorder);
            Grid.SetColumn(heroBorder, 0);

            // ── Cột phải: thông tin ───────────────────────────────────
            var infoGrid = new Grid
            {
                Padding = new Thickness(14, 12, 14, 12),
                RowDefinitions = new RowDefinitionCollection(
                    new RowDefinition { Height = GridLength.Auto },   // tên
                    new RowDefinition { Height = GridLength.Auto },   // loại
                    new RowDefinition { Height = GridLength.Star },   // mô tả
                    new RowDefinition { Height = GridLength.Auto }    // nút unsave
                )
            };

            var tenLabel = new Label
            {
                Text = quan.TenQuan,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2C2F30"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            };
            infoGrid.Children.Add(tenLabel);
            Grid.SetRow(tenLabel, 0);

            // Badge loại quán
            var badgeBorder = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(8, 3),
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 5, 0, 0),
                BackgroundColor = Color.FromArgb("#E8F5E9"),
            };
            badgeBorder.Content = new Label
            {
                Text = loaiLabel,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#376454")
            };
            infoGrid.Children.Add(badgeBorder);
            Grid.SetRow(badgeBorder, 1);

            var moTaLabel = new Label
            {
                Text = quan.MoTa,
                FontSize = 12,
                TextColor = Color.FromArgb("#595C5D"),
                Opacity = 0.85,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 2,
                Margin = new Thickness(0, 6, 0, 0)
            };
            infoGrid.Children.Add(moTaLabel);
            Grid.SetRow(moTaLabel, 2);

            // ── Nút Unsave (bỏ lưu) ───────────────────────────────────
            var unsaveBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#FFF0F0"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 0,
                Padding = new Thickness(12, 6),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 8, 0, 0)
            };
            var unsaveRow = new HorizontalStackLayout { Spacing = 4 };
            unsaveRow.Children.Add(new Label { Text = "❤️", FontSize = 12, VerticalOptions = LayoutOptions.Center });
            unsaveRow.Children.Add(new Label
            {
                Text = "Đã lưu",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#B91C1C"),
                VerticalOptions = LayoutOptions.Center
            });
            unsaveBorder.Content = unsaveRow;

            var quanId = quan.Id;  // capture for lambda
            var unsaveTap = new TapGestureRecognizer();
            unsaveTap.Tapped += async (s, e) =>
            {
                await _dbService.LuuYeuThich(quanId, false);
                await unsaveBorder.ScaleTo(0.0, 180, Easing.CubicIn);
                await LoadSavedItems();
            };
            unsaveBorder.GestureRecognizers.Add(unsaveTap);

            infoGrid.Children.Add(unsaveBorder);
            Grid.SetRow(unsaveBorder, 3);

            grid.Children.Add(infoGrid);
            Grid.SetColumn(infoGrid, 1);

            card.Content = grid;

            // Tap vào card → (placeholder, có thể mở detail sau)
            var cardTap = new TapGestureRecognizer();
            cardTap.Tapped += (s, e) =>
            {
                // TODO: mở chi tiết từ SavedPage nếu cần
            };
            card.GestureRecognizers.Add(cardTap);

            SavedCardStack.Children.Add(card);
        }
    }

    // ============================================================
    //  SỰ KIỆN TÌM KIẾM
    // ============================================================
    private void SearchEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim().ToLowerInvariant() ?? "";
        var filtered = string.IsNullOrEmpty(query)
            ? _allSaved
            : _allSaved.Where(q =>
                q.TenQuan.ToLowerInvariant().Contains(query) ||
                (q.MoTa?.ToLowerInvariant().Contains(query) ?? false)
              ).ToList();

        RenderSavedCards(filtered);
        LblSavedCount.Text = $"{filtered.Count} địa điểm";
    }

    // ============================================================
    //  ĐIỀU HƯỚNG
    // ============================================================
    private void NavHome_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new MainPage(_dbService, _geofenceEngine, _syncService));
        }
    }

    private void NavTours_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            // Sang TourPage, mang theo 2 công cụ
            Application.Current.MainPage = new NavigationPage(new TourPage(_dbService, _geofenceEngine, _syncService));
        }
    }

    private void NavProfile_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            // Sang ProfilePage, mang theo 2 công cụ
            Application.Current.MainPage = new NavigationPage(new ProfilePage(_dbService, _geofenceEngine, _syncService));
        }
    }
}
