using Microsoft.Maui.Controls;
using System;
using TasteTourApp.Services;
using TasteTourApp.Services.Geofence;

namespace TasteTourApp.Views;

public partial class ProfilePage : ContentPage
{
    private readonly DatabaseService _dbService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly SyncService _syncService;
    private readonly DeviceService _deviceService;

    public ProfilePage(DatabaseService dbService, GeofenceEngine geofenceEngine, SyncService syncService, DeviceService deviceService)
    {
        InitializeComponent();
        _dbService = dbService;
        _geofenceEngine = geofenceEngine;
        _syncService = syncService;
        _deviceService = deviceService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Frictionless: hiển thị tên và ID thiết bị thay vì email tài khoản
        LblUserName.Text = _deviceService.Nickname;
        LblUserEmail.Text = _deviceService.ShortId;

        // Cập nhật thông tin thiết bị trong sub-section
        if (LblDeviceId != null)
            LblDeviceId.Text = _deviceService.ShortId;
        if (LblDeviceName != null)
            LblDeviceName.Text = _deviceService.DeviceModel;
        if (LblFirstUsed != null)
            LblFirstUsed.Text = AppLanguage.T(
                $"Sử dụng từ {_deviceService.FirstUsedDate}",
                $"Using since {_deviceService.FirstUsedDate}");

        // Áp dụng ngôn ngữ giao diện
        ApplyLanguage();
    }

    // ============================================================
    //  ÁP DỤNG NGÔN NGỮ GIAO DIỆN
    // ============================================================
    private void ApplyLanguage()
    {
        LblEditProfile.Text   = AppLanguage.T("Chỉnh sửa hồ sơ", "Edit Profile");
        LblSettings.Text      = AppLanguage.T("Cài đặt", "Settings");
        LblHelpCenter.Text    = AppLanguage.T("Trung tâm trợ giúp", "Help Center");
        LblDeviceInfoTitle.Text = AppLanguage.T("Thông tin thiết bị", "Device Information");
        LblStatusTitle.Text   = AppLanguage.T("Trạng thái", "Status");

        // Cập nhật sub-menu ngôn ngữ hiện tại
        LblAppLangTitle.Text  = AppLanguage.T("Ngôn ngữ ứng dụng", "App Language");
        LblAppLangSub.Text    = AppLanguage.IsEnglish ? "🇺🇸  English" : "🇻🇳  Tiếng Việt";
    }

    private void NavExplore_Tapped(object sender, EventArgs e)
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
            Application.Current.MainPage = new NavigationPage(new TourPage(_dbService, _geofenceEngine, _syncService));
        }
    }

    private void NavSaved_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new SavedPage(_dbService, _geofenceEngine, _syncService));
        }
    }

    private bool _isCaiDatExpanded = false;

    // ============================================================
    //  CHỌN NGÔN NGỮ ỨNG DỤNG
    // ============================================================
    private static readonly Dictionary<string, (string display, string shortName)> _appLanguages = new()
    {
        { "vi", ("🇻🇳  Tiếng Việt", "Tiếng Việt") },
        { "en", ("🇺🇸  English",       "English")   },
    };

    private async void AppLanguage_Tapped(object sender, EventArgs e)
    {
        string currentCode = AppLanguage.Code;
        string currentName = _appLanguages.ContainsKey(currentCode)
            ? _appLanguages[currentCode].shortName
            : "Tiếng Việt";

        string cancelText = AppLanguage.T("Hủy", "Cancel");
        string titleText  = AppLanguage.T(
            $"Ngôn ngữ ứng dụng (hiện: {currentName})",
            $"App Language (current: {currentName})");

        var options = _appLanguages.Select(kv =>
            kv.Key == currentCode
                ? $"✓  {kv.Value.display}"
                : $"     {kv.Value.display}"
        ).ToArray();

        string result = await DisplayActionSheet(titleText, cancelText, null, options);
        if (string.IsNullOrEmpty(result) || result == cancelText) return;

        foreach (var kv in _appLanguages)
        {
            if (result.Contains(kv.Value.display))
            {
                if (kv.Key == currentCode) return; // không đổi

                AppLanguage.SetLanguage(kv.Key);

                string msg = AppLanguage.T(
                    $"Đã đổi ngôn ngữ sang {kv.Value.shortName}.\n\nGiao diện sẽ cập nhật ngay.",
                    $"Language changed to {kv.Value.shortName}.\n\nThe interface will update now.");
                await DisplayAlert(AppLanguage.T("Ngôn ngữ", "Language"), msg, "OK");

                // Reload ProfilePage với ngôn ngữ mới
                if (Application.Current != null)
                {
                    Application.Current.MainPage = new NavigationPage(
                        new ProfilePage(_dbService, _geofenceEngine, _syncService, _deviceService));
                }
                return;
            }
        }
    }


    private async void CaiDat_Tapped(object sender, EventArgs e)
    {
        _isCaiDatExpanded = !_isCaiDatExpanded;
        CaiDatSubMenu.IsVisible = _isCaiDatExpanded;

        // Animate arrow rotation
        await CaiDatArrow.RotateTo(_isCaiDatExpanded ? 90 : 0, 200, Easing.CubicInOut);
    }

    // Mapping ngôn ngữ: code → (flag + tên, tên ngắn)
    private static readonly Dictionary<string, (string display, string shortName)> _ttsLanguages = new()
    {
        { "vi", ("🇻🇳  Tiếng Việt", "Tiếng Việt") },
        { "en", ("🇺🇸  English", "English") },
        { "ko", ("🇰🇷  한국어 (Tiếng Hàn)", "한국어") },
        { "zh", ("🇨🇳  中文 (Tiếng Trung)", "中文") },
    };

    private async void TTSVoice_Tapped(object sender, EventArgs e)
    {
        // Lấy ngôn ngữ hiện tại
        string currentLang = Preferences.Get("tts_language", "vi");
        string currentName = _ttsLanguages.ContainsKey(currentLang)
            ? _ttsLanguages[currentLang].shortName : "Tiếng Việt";

        // Tạo danh sách với dấu ✓ cho ngôn ngữ đang chọn
        var options = _ttsLanguages.Select(kv =>
            kv.Key == currentLang
                ? $"✓  {kv.Value.display}"
                : $"     {kv.Value.display}"
        ).ToArray();

        string result = await DisplayActionSheet(
            $"Ngôn ngữ thuyết minh (đang dùng: {currentName})",
            "Hủy", null, options);

        if (!string.IsNullOrEmpty(result) && result != "Hủy")
        {
            // Tìm language code từ kết quả chọn
            foreach (var kv in _ttsLanguages)
            {
                if (result.Contains(kv.Value.display))
                {
                    Preferences.Set("tts_language", kv.Key);
                    await DisplayAlert("Ngôn ngữ TTS",
                        $"Đã chọn: {kv.Value.display}\n\nThuyết minh sẽ được đọc bằng {kv.Value.shortName}.",
                        "OK");
                    break;
                }
            }
        }
    }

    private async void GPSSensitivity_Tapped(object sender, EventArgs e)
    {
        string[] radii = { "50m - Rất gần", "100m - Gần", "200m - Trung bình", "500m - Xa", "1000m - Rất xa" };
        string result = await DisplayActionSheet("Bán kính kích hoạt GPS", "Hủy", null, radii);
        if (!string.IsNullOrEmpty(result) && result != "Hủy")
        {
            await DisplayAlert("Độ nhạy GPS", $"Đã đặt bán kính: {result}", "OK");
        }
    }

    private async void OfflineData_Tapped(object sender, EventArgs e)
    {
        string[] options = { "Tải gói Hà Nội", "Tải gói TP.HCM", "Tải gói Đà Nẵng", "Tải gói Huế", "Xóa tất cả dữ liệu offline" };
        string result = await DisplayActionSheet("Quản lý dữ liệu Offline", "Hủy", null, options);
        if (!string.IsNullOrEmpty(result) && result != "Hủy")
        {
            if (result == "Xóa tất cả dữ liệu offline")
            {
                bool confirm = await DisplayAlert("Xác nhận", "Bạn có chắc chắn muốn xóa tất cả dữ liệu offline?", "Đồng ý", "Hủy");
                if (confirm)
                {
                    await DisplayAlert("Hoàn tất", "Đã xóa tất cả dữ liệu offline", "OK");
                }
            }
            else
            {
                await DisplayAlert("Đang tải", $"Bắt đầu tải: {result}", "OK");
            }
        }
    }
}
