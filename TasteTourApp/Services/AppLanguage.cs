namespace TasteTourApp.Services
{
    /// <summary>
    /// Static helper quản lý ngôn ngữ giao diện ứng dụng.
    /// Sử dụng cùng preference key "tts_language" với TTS để giữ thống nhất.
    /// </summary>
    public static class AppLanguage
    {
        // Preference key dùng chung với TTS language
        private const string PrefKey = "tts_language";

        /// <summary>Mã ngôn ngữ hiện tại ("vi" hoặc "en", ...).</summary>
        public static string Code => Preferences.Get(PrefKey, "vi");

        /// <summary>True nếu ngôn ngữ UI hiện tại là tiếng Anh.</summary>
        public static bool IsEnglish => Code == "en";

        /// <summary>
        /// Đặt ngôn ngữ ứng dụng (và TTS).
        /// </summary>
        public static void SetLanguage(string code)
        {
            Preferences.Set(PrefKey, code);
        }

        /// <summary>
        /// Trả về chuỗi tiếng Anh nếu IsEnglish, ngược lại trả tiếng Việt.
        /// </summary>
        public static string T(string vi, string en)
            => IsEnglish ? en : vi;

        /// <summary>
        /// Chọn nội dung POI theo ngôn ngữ hiện tại.
        /// Nếu chuỗi tiếng Anh trống, fallback về tiếng Việt.
        /// </summary>
        public static string PoiText(string? vi, string? en)
        {
            if (IsEnglish && !string.IsNullOrWhiteSpace(en))
                return en!;
            return vi ?? string.Empty;
        }
    }
}
