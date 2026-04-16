using System.Net.Http.Json;
using System.Text.Json;
using TasteTourApp.Models;

namespace TasteTourApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        // ── Đổi URL này cho đúng với server của bạn ──────────────────
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
        public bool IsNetworkAvailable()
        {
            var access = Connectivity.Current.NetworkAccess;
            return access == NetworkAccess.Internet || access == NetworkAccess.ConstrainedInternet;
        }

        // ============================================================
        //  LẤY DANH SÁCH POI TỪ API
        // ============================================================
        public async Task<List<QuanAn>> FetchPOIsAsync()
        {
            try
            {
                if (!IsNetworkAvailable())
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService] Offline — bỏ qua request.");
                    return new List<QuanAn>();
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                jsonOptions.Converters.Add(new IntToStringConverter());

                var response = await _httpClient.GetFromJsonAsync<List<QuanAn>>($"{_baseUrl}/POIsApi", jsonOptions);

                return response ?? new List<QuanAn>();
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[ApiService] Request timeout.");
                return new List<QuanAn>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] Lỗi: {ex.Message}");
                return new List<QuanAn>();
            }
        }

        // ============================================================
        //  ĐĂNG NHẬP (API)
        // ============================================================
        public async Task<LoginResponse> LoginAsync(string email, string password)
        {
            if (!IsNetworkAvailable())
                return new LoginResponse { Success = false, Message = "Không có kết nối mạng." };

            try
            {
                var payload = new { Email = email, Password = password };
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/AuthApi/login", payload);

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(jsonOptions);

                if (result == null)
                    return new LoginResponse { Success = false, Message = "Lỗi phản hồi từ máy chủ." };

                if (!response.IsSuccessStatusCode && !result.Success && string.IsNullOrEmpty(result.Message))
                    result.Message = "Tài khoản hoặc mật khẩu không hợp lệ.";

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] Lỗi Login: {ex.Message}");
                return new LoginResponse { Success = false, Message = "Lỗi kết nối đến máy chủ." };
            }
        }

        // ============================================================
        //  ĐĂNG KÝ (API)
        // ============================================================
        public async Task<RegisterResponse> RegisterAsync(string fullName, string email, string phone, string password)
        {
            if (!IsNetworkAvailable())
                return new RegisterResponse { Success = false, Message = "Không có kết nối mạng." };

            try
            {
                var payload = new
                {
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    Password = password
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/AuthApi/register", payload);

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = await response.Content.ReadFromJsonAsync<RegisterResponse>(jsonOptions);

                if (result == null)
                    return new RegisterResponse { Success = false, Message = "Lỗi phản hồi từ máy chủ." };

                // 409 Conflict = email đã tồn tại — server đã trả về message rõ ràng
                if (!response.IsSuccessStatusCode && !result.Success && string.IsNullOrEmpty(result.Message))
                    result.Message = "Đăng ký không thành công. Vui lòng thử lại.";

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] Lỗi Register: {ex.Message}");
                return new RegisterResponse { Success = false, Message = "Lỗi kết nối đến máy chủ." };
            }
        }
    } // <--- CLASS ApiService ĐÓNG LẠI Ở ĐÂY CHỨ KHÔNG PHẢI Ở TRÊN

    // ============================================================
    //  CÁC CLASS MÔ HÌNH DỮ LIỆU (NẰM NGOÀI APISERVICE)
    // ============================================================
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public class IntToStringConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
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