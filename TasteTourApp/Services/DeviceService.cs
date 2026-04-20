namespace TasteTourApp.Services
{
    /// <summary>
    /// Nhận diện thiết bị theo mô hình Frictionless Onboarding.
    /// Không cần tài khoản — mỗi lần install tạo ra một Device ID duy nhất.
    /// </summary>
    public class DeviceService
    {
        private const string DeviceIdKey = "device_id";
        private const string DeviceNicknameKey = "device_nickname";

        private string _cachedId;

        // ── Device ID (GUID lưu trong Preferences) ─────────────────────
        public string DeviceId
        {
            get
            {
                if (_cachedId != null) return _cachedId;

                var stored = Preferences.Get(DeviceIdKey, string.Empty);
                if (string.IsNullOrEmpty(stored))
                {
                    stored = Guid.NewGuid().ToString("N").ToUpper(); // 32 ký tự HEX
                    Preferences.Set(DeviceIdKey, stored);
                    System.Diagnostics.Debug.WriteLine($"[DeviceService] Tạo Device ID mới: {stored}");
                }

                _cachedId = stored;
                return _cachedId;
            }
        }

        // ── Tên rút gọn thân thiện: "TTD-A1B2C3" ───────────────────────
        public string ShortId => $"TTD-{DeviceId[^6..]}";

        // ── Tên thiết bị từ hệ điều hành ───────────────────────────────
        public string DeviceName => DeviceInfo.Current.Name ?? "Unknown Device";

        // ── Model thiết bị ──────────────────────────────────────────────
        public string DeviceModel => DeviceInfo.Current.Model ?? "Unknown";

        // ── Nickname tuỳ chỉnh (người dùng có thể đặt tên) ─────────────
        public string Nickname
        {
            get => Preferences.Get(DeviceNicknameKey, DeviceName);
            set => Preferences.Set(DeviceNicknameKey, value);
        }

        // ── Ngày đầu tiên sử dụng app ───────────────────────────────────
        public string FirstUsedDate
        {
            get
            {
                const string key = "device_first_used";
                var stored = Preferences.Get(key, string.Empty);
                if (string.IsNullOrEmpty(stored))
                {
                    stored = DateTime.Now.ToString("dd/MM/yyyy");
                    Preferences.Set(key, stored);
                }
                return stored;
            }
        }
    }
}
