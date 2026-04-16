using TasteTourApp.Models;

namespace TasteTourApp.Services
{
    /// <summary>
    /// Trạng thái của quá trình đồng bộ dữ liệu
    /// </summary>
    public enum SyncStatus
    {
        Idle,
        Syncing,
        Success,
        Offline,
        Error
    }

    /// <summary>
    /// Kết quả sau khi đồng bộ xong
    /// </summary>
    public class SyncResult
    {
        public SyncStatus Status { get; init; }
        public int Added { get; init; }
        public int Updated { get; init; }
        public string? ErrorMessage { get; init; }
        public DateTime SyncedAt { get; init; } = DateTime.Now;

        public string StatusText => Status switch
        {
            SyncStatus.Syncing  => "🔄 Đang đồng bộ...",
            SyncStatus.Success  => Added + Updated > 0
                                    ? $"✅ Đã cập nhật {Added + Updated} quán"
                                    : "✅ Dữ liệu đã mới nhất",
            SyncStatus.Offline  => "📶 Offline · Dùng dữ liệu cũ",
            SyncStatus.Error    => $"⚠️ Lỗi đồng bộ",
            _                   => ""
        };

        public bool IsSuccess => Status == SyncStatus.Success;
    }

    /// <summary>
    /// Service đóng gói toàn bộ luồng đồng bộ dữ liệu POI từ API về SQLite local.
    /// Chiến lược: Merge theo Id — giữ nguyên IsSaved của quán cũ.
    /// Không bao giờ xóa quán khỏi local để offline vẫn hoạt động.
    /// </summary>
    public class SyncService
    {
        private readonly ApiService _apiService;
        private readonly DatabaseService _dbService;

        // Khoá để tránh sync đồng thời
        private readonly SemaphoreSlim _syncLock = new(1, 1);
        private bool _isSyncing = false;

        // ── Events ─────────────────────────────────────────────────────
        public event EventHandler<SyncResult>? SyncStatusChanged;

        // ── Thời điểm sync gần nhất (lưu vào Preferences) ─────────────
        private const string PREF_LAST_SYNC = "last_sync_at";
        public DateTime? LastSyncedAt
        {
            get
            {
                var ticks = Preferences.Get(PREF_LAST_SYNC, 0L);
                return ticks == 0L ? null : new DateTime(ticks, DateTimeKind.Local);
            }
            private set
            {
                Preferences.Set(PREF_LAST_SYNC, value?.Ticks ?? 0L);
            }
        }

        public SyncService(ApiService apiService, DatabaseService dbService)
        {
            _apiService = apiService;
            _dbService = dbService;
        }

        // ============================================================
        //  SYNC CHÍNH
        // ============================================================
        /// <summary>
        /// Kiểm tra mạng → gọi API → merge DB → notify UI.
        /// Trả về SyncResult để caller có thể xử lý ngay mà không cần chờ event.
        /// </summary>
        public async Task<SyncResult> SyncAsync()
        {
            // Nếu đang sync thì bỏ qua, không xếp hàng chờ
            if (_isSyncing) return new SyncResult { Status = SyncStatus.Syncing };

            await _syncLock.WaitAsync();
            try
            {
                _isSyncing = true;

                // 1. Thông báo đang sync
                var syncingResult = new SyncResult { Status = SyncStatus.Syncing };
                RaiseEvent(syncingResult);

                // 2. Kiểm tra kết nối mạng
                if (!_apiService.IsNetworkAvailable())
                {
                    var offlineResult = new SyncResult { Status = SyncStatus.Offline };
                    RaiseEvent(offlineResult);
                    System.Diagnostics.Debug.WriteLine("[Sync] Offline — bỏ qua sync.");
                    return offlineResult;
                }

                // 3. Lấy dữ liệu từ API
                var apiData = await _apiService.FetchPOIsAsync();
                if (apiData == null || apiData.Count == 0)
                {
                    var errorResult = new SyncResult
                    {
                        Status = SyncStatus.Error,
                        ErrorMessage = "API trả về dữ liệu rỗng"
                    };
                    RaiseEvent(errorResult);
                    return errorResult;
                }

                // 4. Merge vào DB local
                var (added, updated) = await _dbService.SyncTuApi(apiData);

                // 5. Cập nhật thời gian sync
                LastSyncedAt = DateTime.Now;

                var successResult = new SyncResult
                {
                    Status = SyncStatus.Success,
                    Added = added,
                    Updated = updated,
                    SyncedAt = LastSyncedAt.Value
                };

                System.Diagnostics.Debug.WriteLine(
                    $"[Sync] Thành công: +{added} mới, ~{updated} cập nhật lúc {LastSyncedAt:HH:mm:ss}");

                RaiseEvent(successResult);
                return successResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Lỗi: {ex.Message}");
                var errorResult = new SyncResult
                {
                    Status = SyncStatus.Error,
                    ErrorMessage = ex.Message
                };
                RaiseEvent(errorResult);
                return errorResult;
            }
            finally
            {
                _isSyncing = false;
                _syncLock.Release();
            }
        }

        // ── Helper ─────────────────────────────────────────────────────
        private void RaiseEvent(SyncResult result)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                SyncStatusChanged?.Invoke(this, result));
        }
    }
}
