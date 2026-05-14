using System.Net.Http.Json;
using System.Text.Json;
using TasteTourApp.Models;

namespace TasteTourApp.Services
{
    // DTO nhẹ — chỉ dùng ở tầng service, không cần SQLite
    public record TourDto(
        int     Id,
        string  Name,
        string? Description,
        string? Duration,
        string? Distance,
        string? ImageUrl);

    public class ApiService
    {
        private readonly HttpClient _httpClient;

        private readonly string _baseUrl = "http://192.168.31.240:5220/api";

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
        //  LẤY DANH SÁCH POI THEO TOUR (có orderIndex)
        // ============================================================
        public async Task<List<QuanAn>> FetchTourPoisAsync(int tourId)
        {
            try
            {
                if (!IsNetworkAvailable())
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService] Offline — không thể lấy tour POIs.");
                    return new List<QuanAn>();
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                jsonOptions.Converters.Add(new IntToStringConverter());

                var response = await _httpClient.GetFromJsonAsync<List<QuanAn>>(
                    $"{_baseUrl}/ToursApi/{tourId}/pois", jsonOptions);

                return response ?? new List<QuanAn>();
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[ApiService] FetchTourPois timeout.");
                return new List<QuanAn>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] FetchTourPois lỗi: {ex.Message}");
                return new List<QuanAn>();
            }
        }

        // ============================================================
        //  LẤY THÔNG TIN TOUR
        // ============================================================
        public async Task<TourDto?> FetchTourAsync(int tourId)
        {
            try
            {
                if (!IsNetworkAvailable()) return null;

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return await _httpClient.GetFromJsonAsync<TourDto>(
                    $"{_baseUrl}/ToursApi/{tourId}", jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] FetchTour lỗi: {ex.Message}");
                return null;
            }
        }
    }

    // ============================================================
    //  CONVERTER TIỆN ÍCH
    // ============================================================
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