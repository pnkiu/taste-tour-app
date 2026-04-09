using TasteTourApp.Services;
using TasteTourApp.Services.Geofence;

namespace TasteTourApp.Views;

public partial class TourPage : ContentPage
{
    private readonly DatabaseService _dbService;
    private readonly GeofenceEngine _geofenceEngine;
    public TourPage(DatabaseService dbService, GeofenceEngine geofenceEngine)
    {
        InitializeComponent();
        _dbService = dbService;
        _geofenceEngine = geofenceEngine;
    }

    // ============================================================
    //  CTA: BẮT ĐẦU HÀNH TRÌNH
    // ============================================================
    private void BtnBatDau_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new MainPage(_dbService, _geofenceEngine));
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
            Application.Current.MainPage = new NavigationPage(new MainPage(_dbService, _geofenceEngine));
        }
    }

    private void NavSaved_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new SavedPage(_dbService, _geofenceEngine));
        }
    }

    private void NavProfile_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new ProfilePage(_dbService, _geofenceEngine));
        }
    }
}
