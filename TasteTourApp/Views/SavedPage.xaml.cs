using TasteTourApp.Models;

namespace TasteTourApp.Views;

public partial class SavedPage : ContentPage
{
    public SavedPage()
    {
        InitializeComponent();
    }

    private void NavHome_Tapped(object sender, EventArgs e)
    {
        // Điều hướng sang trang chính (MainPage)
        if (Application.Current != null && Application.Current.MainPage is NavigationPage nav)
        {
            var mainPage = nav.Navigation.NavigationStack.FirstOrDefault(p => p is MainPage);
            if (mainPage != null)
            {
                nav.PopToRootAsync();
            }
            else
            {
                nav.PushAsync(new MainPage());
            }
        }
    }

    private void NavTours_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null && Application.Current.MainPage is NavigationPage nav)
        {
            var p = nav.Navigation.NavigationStack.FirstOrDefault(x => x is TourPage);
            if (p != null) nav.PopToRootAsync(); // Mocking navigation behavior
            else nav.PushAsync(new TourPage());
        }
    }

    private void NavProfile_Tapped(object sender, EventArgs e)
    {
        // Điều hướng sang ProfilePage
        if (Application.Current != null && Application.Current.MainPage is NavigationPage nav)
        {
            var p = nav.Navigation.NavigationStack.FirstOrDefault(x => x is ProfilePage);
            if (p != null) nav.PopToRootAsync();
            else nav.PushAsync(new ProfilePage());
        }
    }
}
