using System.Net.Http.Json;
using System.Text.Json;
using TasteTourApp.Models;

namespace TasteTourApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        // ── Đổi URL này cho đúng với server của bạn ──────────────────
        // Android Emulator → 10.0.2.2 trỏ về localhost máy host
        // Thiết bị thật trên cùng WiFi → dùng IP LAN, ví dụ: 192.168.1.x
        private readonly string _baseUrl = "http://10.0.2.2:5220/api";

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)   // Tránh chờ mãi khi server chậm
            };
        }

        // ============================================================
        //  KIỂM TRA MẠNG
        // ============================================================
        /// <summary>Trả về true nếu thiết bị đang có kết nối Internet.</summary>
        public bool IsNetworkAvailable()
        {
            var access = Connectivity.Current.NetworkAccess;
            return access == NetworkAccess.Internet
                || access == NetworkAccess.ConstrainedInternet;
        }

        // ============================================================
        //  LẤY DANH SÁCH POI TỪ API
        // ============================================================
        /// <summary>
        /// Fetch danh sách POI từ web quản lý.
        /// Chỉ lấy dữ liệu về, KHÔNG lưu vào DB — đó là việc của SyncService.
        /// Trả về list rỗng nếu lỗi mạng hoặc parse thất bại.
        /// </summary>
        public async Task<List<QuanAn>> FetchPOIsAsync()
        {
            try
            {
                // Kiểm tra connectivity trước khi gọi
                if (!IsNetworkAvailable())
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService] Offline — bỏ qua request.");
                    return new List<QuanAn>();
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // THÊM DÒNG NÀY ĐỂ KÍCH HOẠT MÁY DỊCH SỐ THÀNH CHỮ
                jsonOptions.Converters.Add(new IntToStringConverter());

                var response = await _httpClient.GetFromJsonAsync<List<QuanAn>>(
                    $"{_baseUrl}/POIsApi", jsonOptions);

                return response ?? new List<QuanAn>();
            }
            catch (TaskCanceledException)
            {
                // Timeout
                System.Diagnostics.Debug.WriteLine("[ApiService] Request timeout.");
                return new List<QuanAn>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] Lỗi: {ex.Message}");
                return new List<QuanAn>();
            }
        }
    }
    public class IntToStringConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Nếu Web gửi về là một con số (Ví dụ: 1) -> Ép thành chữ ("1")
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32().ToString();
            }
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}