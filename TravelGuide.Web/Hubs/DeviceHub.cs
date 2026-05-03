using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace TravelGuide.Web.Hubs
{
    public class DeviceHub : Hub
    {
        // ─── Key = deviceId (ID vật lý của thiết bị), KHÔNG dùng connectionId ───
        // Lý do: connectionId thay đổi mỗi lần reconnect, nhưng deviceId luôn cố định
        // → Cùng 1 thiết bị vật lý chỉ có DUY NHẤT 1 entry, dù navigate bao nhiêu lần
        private static readonly ConcurrentDictionary<string, DeviceInfo> _onlineDevices = new();

        public class DeviceInfo
        {
            public string ConnectionId { get; set; } = ""; // SignalR connectionId hiện tại
            public string DeviceId     { get; set; } = ""; // ID vật lý (stable, từ Preferences)
            public string Platform     { get; set; } = "";
            public string JoinTime     { get; set; } = "";
            public double Lat          { get; set; }
            public double Lng          { get; set; }
        }

        // 1. Mobile gọi khi vào app (hoặc navigate giữa các trang)
        public async Task DeviceJoined(string deviceId, string platform, double lat, double lng)
        {
            var connectionId = Context.ConnectionId;

            if (_onlineDevices.TryGetValue(deviceId, out var existing))
            {
                // Thiết bị VẬT LÝ này đã có → chỉ cập nhật connectionId + vị trí
                // KHÔNG tạo entry mới → web KHÔNG thêm row mới
                existing.ConnectionId = connectionId;
                existing.Lat = lat;
                existing.Lng = lng;

                await Clients.All.SendAsync("OnDeviceLocationUpdated", connectionId, lat, lng);
            }
            else
            {
                // Thiết bị mới hoàn toàn → tạo entry, broadcast cho web
                var joinTime = DateTime.Now.ToString("HH:mm:ss");
                _onlineDevices[deviceId] = new DeviceInfo
                {
                    ConnectionId = connectionId,
                    DeviceId     = deviceId,
                    Platform     = platform,
                    JoinTime     = joinTime,
                    Lat          = lat,
                    Lng          = lng
                };

                await Clients.All.SendAsync("OnDeviceConnected", connectionId, deviceId, platform, joinTime, lat, lng);
            }
        }

        // 2. Web Admin kết nối → gửi lại toàn bộ danh sách thiết bị đang online
        public override async Task OnConnectedAsync()
        {
            foreach (var kv in _onlineDevices)
            {
                var info = kv.Value;
                await Clients.Caller.SendAsync(
                    "OnDeviceConnected",
                    info.ConnectionId,
                    info.DeviceId,
                    info.Platform,
                    info.JoinTime,
                    info.Lat,
                    info.Lng
                );
            }

            await base.OnConnectedAsync();
        }

        // 3. Tự động chạy khi Mobile mất kết nối
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            // Tìm thiết bị theo connectionId hiện tại (key là deviceId)
            var toRemove = _onlineDevices.FirstOrDefault(
                kv => kv.Value.ConnectionId == connectionId);

            if (toRemove.Key != null)
            {
                _onlineDevices.TryRemove(toRemove.Key, out _);
                await Clients.All.SendAsync("OnDeviceDisconnected", connectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}