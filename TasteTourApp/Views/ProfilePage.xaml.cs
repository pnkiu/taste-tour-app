using System;
using Microsoft.Maui.Controls;

namespace TasteTourApp.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    private async void NavExplore_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new MainPage());
        }
    }

    private void NavTours_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new TourPage());
        }
    }

    private void NavSaved_Tapped(object sender, EventArgs e)
    {
        // Điều hướng sang Saved page (nếu có)
    }

    private async void DangXuat_Tapped(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Xác nhận", "Bạn có chắc chắn muốn đăng xuất?", "Đồng ý", "Hủy");
        if (answer && Application.Current != null)
        {
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
    }

    private bool _isCaiDatExpanded = false;

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
