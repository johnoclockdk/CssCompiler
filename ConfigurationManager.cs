using System;
using System.IO;
using System.Text.Json;

namespace Compiler
{
    public class ConfigurationManager
    {
        private const string ConfigFileName = "config.json";

        public Config LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFileName))
                {
                    var defaultConfig = new Config { };
                    string defaultJson = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(ConfigFileName, defaultJson);
                }

                string json = File.ReadAllText(ConfigFileName);
                return JsonSerializer.Deserialize<Config>(json)!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                return new Config();
            }
        }

        public static void SaveConfig(Config config)
        {
            // Serialize the config object to JSON
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            // Write to the file
            File.WriteAllText(ConfigFileName, json);
        }

    }
}
