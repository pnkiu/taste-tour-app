
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

            // Frictionless: QR code dẫn thẳng vào MainPage (tastetour://start)
            if (uri.Scheme.ToLower() == "tastetour" && uri.Host.ToLower() == "start")
            {
                System.Diagnostics.Debug.WriteLine("[DEEP LINK] Vừa quét mã, chuyển đến MainPage...");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Task.Delay(500);
                        // Đảm bảo Shell (Giao diện chính) đã được tạo ra trước khi chuyển
                        if (Shell.Current != null)
                        {
                            // 🛑 QUAN TRỌNG: Hãy đảm bảo "//MainPage" là đúng tên Route của bạn
                            await Shell.Current.GoToAsync("//MainPage");
                            System.Diagnostics.Debug.WriteLine("[DEEP LINK] Chuyển trang thành công!");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[DEEP LINK LỖI] Shell chưa kịp khởi tạo!");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Nếu văng, nó sẽ in lý do văng ra cửa sổ Output để mình bắt bệnh
                        System.Diagnostics.Debug.WriteLine($"[DEEP LINK LỖI CRASH]: {ex.Message}");
                    }
                } );
                }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Frictionless Onboarding: vào thẳng AppShell → MainPage, không qua Login
            return new Window(new AppShell());
        }
    }
}