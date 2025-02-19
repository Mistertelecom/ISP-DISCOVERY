using System.IO;
using System.Text.Json;

namespace NetworkDiscovery
{
    public static class AppConfig
    {
        private static readonly string configPath = "config.json";
        
        public static string Language { get; set; } = "en";
        public static bool DarkMode { get; set; } = false;

        public static void Load()
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<ConfigData>(json);
                Language = config.Language;
                DarkMode = config.DarkMode;
            }
        }

        public static void Save()
        {
            var config = new ConfigData {
                Language = Language,
                DarkMode = DarkMode
            };
            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(configPath, json);
        }

        private class ConfigData
        {
            public string Language { get; set; }
            public bool DarkMode { get; set; }
        }
    }
}