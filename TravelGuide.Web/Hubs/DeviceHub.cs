using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;

namespace TravelGuide.Web.Hubs
{
    // Kế thừa từ Hub của SignalR
    public class DeviceHub : Hub
    {
        // 1. Hàm này dành cho Mobile (Phát) gọi lên khi vừa mở App
        // Đã thêm lat và lng để phục vụ vẽ Heatmap
        public async Task DeviceJoined(string deviceId, string platform, double lat, double lng)
        {
            var joinTime = DateTime.Now.ToString("HH:mm:ss");
            var connectionId = Context.ConnectionId; // Mã kết nối ẩn của SignalR

            // Phát loa thông báo kèm tọa độ cho màn hình Web Admin
            await Clients.All.SendAsync("OnDeviceConnected", connectionId, deviceId, platform, joinTime, lat, lng);
        }

        // 2. Hàm này tự động chạy khi Mobile bị tắt mạng hoặc đóng App
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            // Phát loa thông báo: "Thiết bị vừa ngắt kết nối!"
            await Clients.All.SendAsync("OnDeviceDisconnected", connectionId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}