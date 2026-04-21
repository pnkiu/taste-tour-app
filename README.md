3 bước dành cho Phát (phía Mobile App) 

Cài thư viện: Cài gói NuGet Microsoft.AspNetCore.SignalR.Client vào project MAUI.

Kết nối & Báo danh: Khi mở App (hoặc quét xong QR), gọi lệnh StartAsync() để nối máy với Server, sau đó bắn tên thiết bị lên bằng lệnh InvokeAsync("DeviceJoined", ...).

Ngắt kết nối: Bắt sự kiện tắt App hoặc rời trang (OnDisappearing), gọi lệnh StopAsync() để Server biết đường gỡ tên máy khỏi danh sách đang Online.
