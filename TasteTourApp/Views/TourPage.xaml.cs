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
