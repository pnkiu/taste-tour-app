using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using TasteTourApp.Services;
using TasteTourApp.Services.Geofence;
using TasteTourApp.Views;

namespace TasteTourApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // ── Đăng ký services ──────────────────────────────────────
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<GeofenceEngine>();
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<SyncService>();
            // Frictionless Onboarding: nhận diện thiết bị thay vì tài khoản
            builder.Services.AddSingleton<DeviceService>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<SavedPage>();
            builder.Services.AddSingleton<ProfilePage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
