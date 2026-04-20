using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;

namespace TravelGuide.Web.Hubs
{
    // Kế thừa từ Hub của SignalR
    public class DeviceHub : Hub
    {
        // 1. Hàm này dành cho Mobile (Phát) gọi lên khi vừa mở App
        public async Task DeviceJoined(string deviceId, string platform)
        {
            var joinTime = DateTime.Now.ToString("HH:mm:ss");
            var connectionId = Context.ConnectionId; // Mã kết nối ẩn của SignalR

            // Phát loa thông báo cho tất cả màn hình Web Admin: "Có máy mới vào!"
            await Clients.All.SendAsync("OnDeviceConnected", connectionId, deviceId, platform, joinTime);
        }

        // 2. Hàm này tự động chạy khi Mobile bị tắt mạng hoặc đóng App
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            // Phát loa thông báo: "Cái máy có mã connectionId này vừa out rồi!"
            await Clients.All.SendAsync("OnDeviceDisconnected", connectionId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}