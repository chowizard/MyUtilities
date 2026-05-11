using System.Text.Encodings.Web;
using System.Text.Json;

namespace Summarizer.Core
{
    public static class AppSettingsLoader
    {
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            IndentSize = 4,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static AppSettings Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var defaultSettings = new AppSettings();
                Save(filePath, defaultSettings);
                return defaultSettings;
            }

            var json = File.ReadAllText(filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, jsonOptions) ?? new AppSettings();

            // 하위 호환: 구 "replaceMessages" 키 → ReplaceStaffMessages 마이그레이션
            if (settings.ReplaceStaffMessages.Length == 0)
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("replaceMessages", out var legacyElement))
                {
                    var legacy = JsonSerializer.Deserialize<ReplaceMessage[]>(
                        legacyElement.GetRawText(), jsonOptions);
                    if (legacy?.Length > 0)
                        settings.ReplaceStaffMessages = legacy;
                }
            }

            return settings;
        }

        public static void Save(string filePath, AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, jsonOptions);
            File.WriteAllText(filePath, json);
        }
    }
}
