using TasteTourApp.Services;

namespace TasteTourApp.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService; // 1. Khai báo biến

    // 2. Yêu cầu MAUI tự động "tiêm" ApiService vào đây
    public LoginPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void QuenMatKhau_Tapped(object sender, EventArgs e)
    {
        await DisplayAlert("Thông báo", "Chức năng Quên mật khẩu đang được phát triển.", "OK");
    }

    private async void DangNhap_Tapped(object sender, EventArgs e)
    {
        string email = EntEmail.Text?.Trim();
        string password = EntPassword.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập Email và Mật khẩu.", "OK");
            return;
        }

        // Hiện loading nhẹ (có thể sửa lại bằng ActivityIndicator sau)
        await Task.Delay(100);

        var result = await _apiService.LoginAsync(email, password);

        if (result.Success)
        {
            // Lưu trạng thái đăng nhập thành công
            Preferences.Set("is_logged_in", true);
            Preferences.Set("user_email", result.Email);
            Preferences.Set("user_role", result.Role);

            if (Application.Current != null)
            {
                Application.Current.MainPage = new AppShell();
            }
        }
        else
        {
            await DisplayAlert("Đăng nhập thất bại", result.Message, "Thử lại");
        }
    }

    private async void Google_Tapped(object sender, EventArgs e)
    {
        await DisplayAlert("Đăng nhập", "Đăng nhập bằng Google đang được triển khai.", "OK");
    }

    private async void Apple_Tapped(object sender, EventArgs e)
    {
        await DisplayAlert("Đăng nhập", "Đăng nhập bằng Apple đang được triển khai.", "OK");
    }

    private void Khach_Tapped(object sender, EventArgs e)
    {
        // Khách sẽ vào thẳng trang chính nhưng không được đánh dấu là logged in
        Preferences.Set("is_logged_in", false);
        Preferences.Remove("user_email");
        Preferences.Remove("user_role");

        if (Application.Current != null)
        {
            Application.Current.MainPage = new AppShell();
        }
    }

    private async void TaoTaiKhoan_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            // Chuyển sang trang Đăng ký (RegisterPage) dùng Dependency Injection
            Application.Current.MainPage = Handler.MauiContext.Services.GetService<RegisterPage>();
        }
    }
}
