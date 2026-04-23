using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;

namespace TravelGuide.Web.Hubs
{
    public class DeviceHub : Hub
    {
        // ─── Lưu trạng thái thiết bị đang Online (dùng ConcurrentDictionary cho thread-safe) ───
        // Key = connectionId của MOBILE, Value = thông tin thiết bị
        private static readonly ConcurrentDictionary<string, DeviceInfo> _onlineDevices = new();

        public class DeviceInfo
        {
            public string DeviceId   { get; set; } = "";
            public string Platform   { get; set; } = "";
            public string JoinTime   { get; set; } = "";
            public double Lat        { get; set; }
            public double Lng        { get; set; }
        }

        // 1. Hàm dành cho Mobile gọi lên khi vừa mở App
        public async Task DeviceJoined(string deviceId, string platform, double lat, double lng)
        {
            var joinTime    = DateTime.Now.ToString("HH:mm:ss");
            var connectionId = Context.ConnectionId;

            // Lưu vào dictionary để khi admin web reload không mất
            _onlineDevices[connectionId] = new DeviceInfo
            {
                DeviceId  = deviceId,
                Platform  = platform,
                JoinTime  = joinTime,
                Lat       = lat,
                Lng       = lng
            };

            // Phát broadcast cho TẤT CẢ client (bao gồm admin web)
            await Clients.All.SendAsync("OnDeviceConnected", connectionId, deviceId, platform, joinTime, lat, lng);
        }

        // 2. Khi Web Admin kết nối vào Hub, gửi lại toàn bộ danh sách thiết bị đang online
        public override async Task OnConnectedAsync()
        {
            // Gửi danh sách hiện tại CHỈ cho client vừa kết nối (Clients.Caller)
            foreach (var kv in _onlineDevices)
            {
                var info = kv.Value;
                await Clients.Caller.SendAsync(
                    "OnDeviceConnected",
                    kv.Key,           // connectionId
                    info.DeviceId,
                    info.Platform,
                    info.JoinTime,
                    info.Lat,
                    info.Lng
                );
            }

            await base.OnConnectedAsync();
        }

        // 3. Tự động chạy khi Mobile bị tắt mạng hoặc đóng App
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            // Xoá khỏi dictionary
            _onlineDevices.TryRemove(connectionId, out _);

            // Phát broadcast thông báo ngắt kết nối
            await Clients.All.SendAsync("OnDeviceDisconnected", connectionId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}