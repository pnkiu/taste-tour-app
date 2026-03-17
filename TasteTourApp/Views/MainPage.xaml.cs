using System.Collections.ObjectModel;
using TasteTourApp.Models; // Gọi thư mục Models để lấy khuôn đúc QuanAn
using TasteTourApp.Services;
namespace TasteTourApp.Views;

public partial class MainPage : ContentPage
{
    // ObservableCollection giúp danh sách tự động cập nhật lên màn hình
    public ObservableCollection<QuanAn> DanhSachPOIs { get; set; }

    private DatabaseService _dbService = new DatabaseService();

    public MainPage()
    {
        InitializeComponent();

        // Khởi tạo một danh sách rỗng trước
        DanhSachPOIs = new ObservableCollection<QuanAn>();

        // Trói danh sách rỗng này vào giao diện XAML
        DanhSachQuanAn.ItemsSource = DanhSachPOIs;
    }

    // Hàm này tự động chạy ngay khoảnh khắc màn hình vừa bật lên
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Gọi hàm đi lấy dữ liệu
        await LoadDuLieuTuKho();
    }

    private async Task LoadDuLieuTuKho()
    {
        // 2. Ra lệnh cho anh Thủ kho chui vào SQLite lấy dữ liệu ra
        // Chú ý chữ 'await': Vì việc lục lọi file tốn thời gian, app phải "chờ" một chút
        var danhSachTuSQLite = await _dbService.LayDanhSachQuanAn();

        // Xóa sạch danh sách cũ trên màn hình (nếu có) để tránh bị nhân đôi
        DanhSachPOIs.Clear();

        // 3. Lấy được bao nhiêu món từ kho thì lần lượt ném hết lên màn hình
        foreach (var quan in danhSachTuSQLite)
        {
            DanhSachPOIs.Add(quan);
        }
    }
}