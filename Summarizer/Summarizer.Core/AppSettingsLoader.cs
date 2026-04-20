using System.Text.Json;

namespace Summarizer.Core
{
    public static class AppSettingsLoader
    {
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public static AppSettings Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<AppSettings>(json, jsonOptions) ?? new AppSettings();
            }

            var defaultSettings = new AppSettings();
            var defaultJson = JsonSerializer.Serialize(defaultSettings, jsonOptions);
            File.WriteAllText(filePath, defaultJson);
            return defaultSettings;
        }
    }
}
