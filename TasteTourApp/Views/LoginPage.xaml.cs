namespace TasteTourApp.Views;

public partial class LoginPage : ContentPage
{
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
        // TODO: Kiểm tra đăng nhập với email & password
        string email = EntEmail.Text;
        string password = EntPassword.Text;

        // Tạm thời bỏ qua bước kiểm tra và điều hướng vào trang chính:
        // Đặt lại MainPage hoặc dùng Navigation để qua AppShell
        if (Application.Current != null)
        {
            Application.Current.MainPage = new AppShell();
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
        // Khách sẽ vào thẳng trang chính
        if (Application.Current != null)
        {
            Application.Current.MainPage = new AppShell();
        }
    }

    private async void TaoTaiKhoan_Tapped(object sender, EventArgs e)
    {
        await DisplayAlert("Đăng ký", "Trang Tạo tài khoản đang được phát triển.", "OK");
    }
}
