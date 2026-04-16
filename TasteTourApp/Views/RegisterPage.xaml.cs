using TasteTourApp.Services;

namespace TasteTourApp.Views;

public partial class RegisterPage : ContentPage
{
    private readonly ApiService _apiService; // Khai báo biến

    // Yêu cầu MAUI tiêm ApiService vào
    public RegisterPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void DangKy_Tapped(object sender, EventArgs e)
    {
        string fullName = EntFullName.Text?.Trim();
        string email = EntEmail.Text?.Trim();
        string phone = EntPhone.Text?.Trim();
        string password = EntPassword.Text;
        string confirmPassword = EntConfirmPassword.Text;

        // 1. Validate rỗng
        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(confirmPassword))
        {
            await DisplayAlert("Lỗi", "Vui lòng điền đầy đủ thông tin.", "OK");
            return;
        }

        // 2. Validate email
        if (!email.Contains("@") || !email.Contains("."))
        {
            await DisplayAlert("Lỗi", "Email không hợp lệ.", "OK");
            return;
        }

        // 3. Validate mật khẩu
        if (password.Length < 6)
        {
            await DisplayAlert("Lỗi", "Mật khẩu phải có ít nhất 6 ký tự.", "OK");
            return;
        }

        // 4. Validate xác nhận mật khẩu
        if (password != confirmPassword)
        {
            await DisplayAlert("Lỗi", "Mật khẩu xác nhận không khớp.", "OK");
            return;
        }

        // 5. Validate điều khoản
        if (!ChkTerms.IsChecked)
        {
            await DisplayAlert("Lỗi", "Vui lòng đồng ý với Điều khoản sử dụng và Chính sách bảo mật.", "OK");
            return;
        }

        await Task.Delay(100); // Hiện loading nhẹ

        // Dùng _apiService đã được tiêm để gọi đăng ký
        var result = await _apiService.RegisterAsync(fullName, email, phone, password);

        if (result.Success)
        {
            await DisplayAlert("Thành công", "Tài khoản đã được tạo. Vui lòng đăng nhập.", "OK");

            // Cách chuyển trang chuẩn khi dùng Dependency Injection
            if (Application.Current != null)
            {
                Application.Current.MainPage = Handler.MauiContext.Services.GetService<LoginPage>();
            }
        }
        else
        {
            await DisplayAlert("Đăng ký thất bại", result.Message, "Thử lại");
        }
    }

    private async void DangNhap_Tapped(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            // Chuyển về trang Login bằng GetService
            Application.Current.MainPage = Handler.MauiContext.Services.GetService<LoginPage>();
        }
        await Task.CompletedTask;
    }

    private async void Google_Tapped(object sender, EventArgs e)
    {
        await DisplayAlert("Đăng ký", "Đăng ký bằng Google đang được triển khai.", "OK");
    }

    private async void Apple_Tapped(object sender, EventArgs e)
    {
        await DisplayAlert("Đăng ký", "Đăng ký bằng Apple đang được triển khai.", "OK");
    }

    private async void DieuKhoan_Tapped(object sender, EventArgs e)
    {
        await DisplayAlert("Điều khoản sử dụng", "Nội dung điều khoản sử dụng đang được cập nhật.", "Đóng");
    }

    private async void ChinhSach_Tapped(object sender, EventArgs e)
    {
        await DisplayAlert("Chính sách bảo mật", "Nội dung chính sách bảo mật đang được cập nhật.", "Đóng");
    }
}