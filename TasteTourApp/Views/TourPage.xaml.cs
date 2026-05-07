using Microsoft.Extensions.DependencyInjection;
using TasteTourApp.Models;
using TasteTourApp.Services;
using TasteTourApp.Services.Geofence;

namespace TasteTourApp.Views;

public partial class TourPage : ContentPage
{
    private readonly DatabaseService _dbService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly SyncService _syncService;
    private readonly ApiService _apiService;

    // Static field để truyền tour POIs sang MainPage (pattern hiện tại)
    public static List<QuanAn>? PendingTourPois { get; private set; }
    public static string? PendingTourName { get; private set; }

    public static void ClearPendingTour()
    {
        PendingTourPois = null;
        PendingTourName = null;
    }

    public TourPage(DatabaseService dbService, GeofenceEngine geofenceEngine, SyncService syncService)
    {
        InitializeComponent();
        _dbService = dbService;
        _geofenceEngine = geofenceEngine;
        _syncService = syncService;
        _apiService = new ApiService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        LblHeroBadge.Text     = AppLanguage.T("✨  KHÁM PHÁ NGAY", "✨  EXPLORE NOW");
        LblHeroTitle.Text     = AppLanguage.T("Khám phá hương vị\ndi sản qua từng\ncâu chuyện", "Discover flavours\nof heritage through\nevery story");
        LblCtaButton.Text     = AppLanguage.T("Bắt đầu hành trình", "Start Your Journey");
        LblFeaturedTitle.Text = AppLanguage.T("Tour Nổi Bật", "Featured Tours");
        LblFeaturedSub.Text   = AppLanguage.T("Được đề xuất dành riêng cho bạn", "Curated just for you");
        LblViewAll.Text       = AppLanguage.T("Xem tất cả", "View all");
        LblRouteTitle.Text    = AppLanguage.T("Khám Phá Theo Tuyến", "Explore By Route");
    }

    // ============================================================
    //  HERO CTA – mở sheet tour 1
    // ============================================================
    private async void BtnBatDau_Tapped(object sender, EventArgs e)
        => await MoItinerarySheet(tourId: 1);

    // ============================================================
    //  TOUR CARD 1 – bấm vào mở sheet
    // ============================================================
    private async void TourCard1_Tapped(object sender, EventArgs e)
        => await MoItinerarySheet(tourId: 1);

    // ============================================================
    //  MỞ ITINERARY SHEET
    // ============================================================
    private async Task MoItinerarySheet(int tourId)
    {
        // Show loading
        BtnStartTour.IsEnabled = false;
        LblSheetTourName.Text  = AppLanguage.T("Đang tải lộ trình...", "Loading itinerary...");
        ItineraryOverlay.IsVisible = true;
        ItinerarySheet.IsVisible   = true;
        ItinerarySheet.TranslationY = ItinerarySheet.Height > 0 ? ItinerarySheet.Height + 40 : 600;

        // Animate sheet lên
        await ItinerarySheet.TranslateTo(0, 0, 320, Easing.CubicOut);

        // Fetch POI từ API
        var pois = await _apiService.FetchTourPoisAsync(tourId);
        var tour = await _apiService.FetchTourAsync(tourId);

        string tourName = tour?.Name ?? AppLanguage.T("Tour Ẩm thực đêm Quận 4", "Night Food Tour District 4");
        LblSheetTourName.Text = tourName;
        LblSheetMeta.Text = $"🕐 {tour?.Duration ?? "--"}  ·  📍 {pois.Count} {AppLanguage.T("điểm", "stops")}";

        RenderPoiSteps(pois);

        BtnStartTour.IsEnabled = pois.Count > 0;

        // Lưu vào static field để MainPage dùng
        PendingTourPois = pois;
        PendingTourName = tourName;
    }

    // ============================================================
    //  RENDER CÁC BƯỚC HÀNH TRÌNH
    // ============================================================
    private static readonly Dictionary<string, (string emoji, string bg)> _loaiMap = new()
    {
        { "Oc",     ("🦪", "#1B4332") },
        { "HaiSan", ("🦑", "#1A3A5C") },
        { "Sushi",  ("🍱", "#4A1942") },
    };

    private void RenderPoiSteps(List<QuanAn> pois)
    {
        PoiStepsContainer.Children.Clear();

        for (int i = 0; i < pois.Count; i++)
        {
            var poi = pois[i];
            var (emoji, bg) = _loaiMap.TryGetValue(poi.LoaiQuan ?? "", out var info) ? info : ("📍", "#2D6A4F");
            bool isLast = i == pois.Count - 1;

            // Wrapper row
            var row = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 14,
                Margin = new Thickness(0, 0, 0, 0),
            };

            // Left column: circle + line
            var leftCol = new Grid { WidthRequest = 36 };
            // Circle số thứ tự
            var circle = new Border
            {
                BackgroundColor = Color.FromArgb(bg),
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
                WidthRequest = 36, HeightRequest = 36,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Start,
            };
            circle.Content = new Label
            {
                Text = $"{i + 1}",
                FontSize = 14, FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center,
            };

            leftCol.Children.Add(circle);

            if (!isLast)
            {
                var line = new BoxView
                {
                    BackgroundColor = Color.FromArgb("#E0E0E0"),
                    WidthRequest = 2, HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions   = LayoutOptions.Fill,
                    Margin = new Thickness(0, 36, 0, 0),
                };
                leftCol.Children.Add(line);
            }

            row.Children.Add(leftCol);
            Grid.SetColumn(leftCol, 0);

            // Right column: info card
            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                StrokeThickness = 0,
                Padding = new Thickness(14, 12),
                Margin = new Thickness(0, 0, 0, isLast ? 8 : 16),
            };
            card.Shadow = new Shadow { Brush = Colors.Black, Offset = new Point(0, 2), Radius = 8, Opacity = 0.07f };

            var cardStack = new VerticalStackLayout { Spacing = 4 };
            cardStack.Children.Add(new Label
            {
                Text = poi.TenQuan, FontSize = 14, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1A1A1A"),
            });

            string desc = AppLanguage.IsEnglish
                ? (poi.MoTaEn ?? poi.MoTa ?? "")
                : (poi.MoTa ?? "");
            if (!string.IsNullOrEmpty(desc))
            {
                cardStack.Children.Add(new Label
                {
                    Text = desc, FontSize = 11, TextColor = Color.FromArgb("#888888"),
                    MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation,
                });
            }

            // Tag emoji
            var tagRow = new HorizontalStackLayout { Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };
            tagRow.Children.Add(new Border
            {
                BackgroundColor = Color.FromArgb($"20{bg.TrimStart('#')}"),
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(8, 3),
                Content = new Label { Text = emoji, FontSize = 12 }
            });
            if (!string.IsNullOrEmpty(poi.AudioContent))
            {
                tagRow.Children.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#1A2D6A4F"),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(8, 3),
                    Content = new Label
                    {
                        Text = "🎧 Audio", FontSize = 10, FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#2D6A4F"),
                    }
                });
            }
            cardStack.Children.Add(tagRow);
            card.Content = cardStack;

            row.Children.Add(card);
            Grid.SetColumn(card, 1);

            PoiStepsContainer.Children.Add(row);
        }
    }

    // ============================================================
    //  ĐÓNG SHEET
    // ============================================================
    private async void BtnCloseSheet_Tapped(object sender, EventArgs e)
    {
        await ItinerarySheet.TranslateTo(0, ItinerarySheet.Height + 40, 260, Easing.CubicIn);
        ItinerarySheet.IsVisible   = false;
        ItineraryOverlay.IsVisible = false;
    }

    private async void Overlay_Tapped(object sender, EventArgs e)
        => await BtnCloseSheet_TappedInternal();

    private async Task BtnCloseSheet_TappedInternal()
    {
        await ItinerarySheet.TranslateTo(0, ItinerarySheet.Height + 40, 260, Easing.CubicIn);
        ItinerarySheet.IsVisible   = false;
        ItineraryOverlay.IsVisible = false;
    }

    // ============================================================
    //  BẮT ĐẦU TOUR → về MainPage
    // ============================================================
    private void BtnStartTour_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(
                new MainPage(_dbService, _geofenceEngine, _syncService));
        }
    }

    // ============================================================
    //  MINI PLAYER
    // ============================================================
    private void BtnCloseMini_Tapped(object sender, EventArgs e)
        => MiniPlayer.IsVisible = false;

    // ============================================================
    //  NAVIGATION BAR
    // ============================================================
    private void NavExplore_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
            Application.Current.MainPage = new NavigationPage(new MainPage(_dbService, _geofenceEngine, _syncService));
    }

    private void NavSaved_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
            Application.Current.MainPage = new NavigationPage(new SavedPage(_dbService, _geofenceEngine, _syncService));
    }

    private void NavProfile_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            var deviceService = IPlatformApplication.Current.Services.GetService<DeviceService>();
            Application.Current.MainPage = new NavigationPage(new ProfilePage(_dbService, _geofenceEngine, _syncService, deviceService));
        }
    }
}
