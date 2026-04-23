using Microsoft.Extensions.DependencyInjection;
using TasteTourApp.Services;
using TasteTourApp.Services.Geofence;

namespace TasteTourApp.Views;

public partial class TourPage : ContentPage
{
    private readonly DatabaseService _dbService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly SyncService _syncService;

    public TourPage(DatabaseService dbService, GeofenceEngine geofenceEngine, SyncService syncService)
    {
        InitializeComponent();
        _dbService = dbService;
        _geofenceEngine = geofenceEngine;
        _syncService = syncService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyLanguage();
    }

    // ============================================================
    //  ÁP DỤNG NGÔN NGỮ GIAO DIỆN
    // ============================================================
    private void ApplyLanguage()
    {
        LblHeroBadge.Text    = AppLanguage.T("✨  KHÁM PHÁ NGAY", "✨  EXPLORE NOW");
        LblHeroTitle.Text    = AppLanguage.T(
            "Khám phá hương vị\ndi sản qua từng\ncâu chuyện",
            "Discover flavours\nof heritage through\nevery story");
        LblCtaButton.Text    = AppLanguage.T("Bắt đầu hành trình", "Start Your Journey");
        LblFeaturedTitle.Text = AppLanguage.T("Tour Nổi Bật", "Featured Tours");
        LblFeaturedSub.Text  = AppLanguage.T("Được đề xuất dành riêng cho bạn", "Curated just for you");
        LblViewAll.Text      = AppLanguage.T("Xem tất cả", "View all");
        LblRouteTitle.Text   = AppLanguage.T("Khám Phá Theo Tuyến", "Explore By Route");
    }

    // ============================================================
    //  CTA: BẮT ĐẦU HÀNH TRÌNH
    // ============================================================
    private void BtnBatDau_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new MainPage(_dbService, _geofenceEngine, _syncService));
        }
    }

    // ============================================================
    //  MINI PLAYER
    // ============================================================
    private void BtnCloseMini_Tapped(object sender, EventArgs e)
    {
        MiniPlayer.IsVisible = false;
    }

    // ============================================================
    //  NAVIGATION BAR
    // ============================================================
    private void NavExplore_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new MainPage(_dbService, _geofenceEngine, _syncService));
        }
    }

    private void NavSaved_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new SavedPage(_dbService, _geofenceEngine, _syncService));
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
}
