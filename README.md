FORWARD TO: NGUYỄN TẤN PHÁT (MOBILE DEV)
PHẦN 1
SIGNALR & HEATMAP (Giám sát Real-time)
Tao đã làm sẵn cái bản đồ xịn sò trên Web, việc của mày là lấy tọa độ GPS bắn lên cho tao vẽ.

Cấu hình quyền Location: Mở file AndroidManifest.xml (Android) và Info.plist (iOS) thêm quyền truy cập Vị trí (Location). Nhớ viết code hiển thị popup xin phép người dùng khi mở App.

Cài thư viện SignalR Client: Cài NuGet package Microsoft.AspNetCore.SignalR.Client vào project MAUI.

Lấy GPS & Bắn dữ liệu: * Khi App vừa mở (hoặc khi vị trí thay đổi), dùng Geolocation.Default.GetLocationAsync() để lấy Latitude và Longitude.

Gọi Hub SignalR hàm DeviceJoined truyền đúng 4 biến này lên: deviceId, platform, lat, lng.

Code mẫu gọi lên Hub: await hubConnection.SendAsync("DeviceJoined", "May_Cua_Phat", "Android", lat, lng);

PHẦN 2
XỬ LÝ GEOFENCING VÀ AUDIO (Cái này quan trọng để lấy điểm)
Khi khách đi bộ vào khu Vĩnh Khánh, sẽ có trường hợp khách đứng giữa 2 quán ăn (bán kính đè lên nhau). Mày phải xử lý chống nhiễu Audio theo logic "Kẻ mạnh làm vua" (dựa vào cột Priority tao cấu hình trên Web).

Tính khoảng cách: Viết hàm dùng Location.CalculateDistance của MAUI để đo khoảng cách từ tọa độ GPS hiện tại của khách đến tọa độ của TẤT CẢ các điểm POI.

Lọc và Phát Audio: * Lọc ra những POI nào mà Khoảng cách <= Bán kính (Radius).

Nếu có nhiều hơn 2 quán thỏa mãn, mày dùng LINQ OrderByDescending(x => x.Priority).First() để lấy ra cái quán có Priority cao nhất.

Chỉ bật file mp3 của cái quán Priority cao nhất đó thôi, mấy quán kia tắt hết.


Gửi cho Phát đoạn code này để ổng lấy GPS và bắn lên Web cho bạn:

// Trong App Mobile .NET MAUI
var location = await Geolocation.Default.GetLocationAsync();
if (location != null) {
    // Gửi 4 tham số: ID máy, Hệ điều hành, Vĩ độ, Kinh độ
    await hubConnection.SendAsync("DeviceJoined", "iPhone_Cua_Phat", "iOS", location.Latitude, location.Longitude);
}
