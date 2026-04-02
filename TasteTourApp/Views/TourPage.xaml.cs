namespace TasteTourApp.Views;

public partial class TourPage : ContentPage
{
    public TourPage()
    {
        InitializeComponent();
    }

    // ============================================================
    //  CTA: BẮT ĐẦU HÀNH TRÌNH
    // ============================================================
    private void BtnBatDau_Tapped(object sender, EventArgs e)
    {
        // Chuyển sang MainPage (Explore) để bắt đầu khám phá
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new MainPage());
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
            Application.Current.MainPage = new NavigationPage(new MainPage());
        }
    }

    private void NavSaved_Tapped(object sender, EventArgs e)
    {
        // TODO: Saved page
    }

    private void NavProfile_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new ProfilePage());
        }
    }
}
