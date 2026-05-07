namespace TasteTourApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            if (!IsAppLaunchLink(uri))
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[DEEP LINK] Open app from QR: {uri}");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Task.Delay(300);

                    if (Shell.Current != null)
                    {
                        await Shell.Current.GoToAsync("//MainPage");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEEP LINK ERROR] {ex}");
                }
            });
        }

        static bool IsAppLaunchLink(Uri uri)
        {
            if (uri.Scheme.Equals("tastetour", StringComparison.OrdinalIgnoreCase))
            {
                return uri.Host.Equals("open", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("start", StringComparison.OrdinalIgnoreCase);
            }

            return uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                && uri.Host.Equals("tastetour.app", StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath.StartsWith("/start", StringComparison.OrdinalIgnoreCase);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
