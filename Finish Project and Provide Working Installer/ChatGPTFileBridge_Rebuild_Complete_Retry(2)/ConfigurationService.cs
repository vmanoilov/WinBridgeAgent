// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WinBridgeAgentControlPanel.Models; // Assuming AppSettings models will be here or in a shared location

namespace WinBridgeAgentControlPanel.Services
{
    // Part of WinBridgeAgent (c) 2025 Vladislav Manoilov

    /// <summary>
    /// Defines the structure of the appsettings.json file for the WinBridgeAgent service.
    /// This should mirror the structure used by the service.
    /// </summary>
    public class ServiceAppSettings
    {
        public ServiceSettingsSection? Service { get; set; }
        public SecuritySettingsSection? Security { get; set; }
        public LoggingSettingsSection? Logging { get; set; }
    }

    public class ServiceSettingsSection
    {
        public string? RootFolder { get; set; }
        public long MaxFileSize { get; set; }
        public string[]? AllowedExtensions { get; set; }
    }

    public class SecuritySettingsSection
    {
        public int TokenValidityMinutes { get; set; }
        public bool RequireAuthentication { get; set; }
    }

    public class LoggingSettingsSection
    {
        public string? Directory { get; set; }
        public int RetentionDays { get; set; }
        public string? MinimumLevel { get; set; } // e.g., Information, Debug, Error
    }


    public class ConfigurationService
    {
        private string _appSettingsPath = string.Empty; // Path to the service's appsettings.json

        public ConfigurationService()
        {
            // In a real app, this path might be discovered or configured.
            // For now, let's assume it might be set externally or discovered.
            // Example: Discover based on service installation path (requires service discovery first)
        }

        public void SetAppSettingsPath(string path)
        {
            _appSettingsPath = path;
            Console.WriteLine($"AppSettingsPath set to: {_appSettingsPath}");
        }

        public string GetAppSettingsPath()
        {
            return _appSettingsPath;
        }

        public async Task<ServiceAppSettings?> LoadConfigurationAsync()
        {
            if (string.IsNullOrEmpty(_appSettingsPath) || !File.Exists(_appSettingsPath))
            {
                Console.WriteLine($"appsettings.json path is not set or file does not exist: {_appSettingsPath}");
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_appSettingsPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                return JsonSerializer.Deserialize<ServiceAppSettings>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration from {_appSettingsPath}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SaveConfigurationAsync(ServiceAppSettings settings)
        {
            if (string.IsNullOrEmpty(_appSettingsPath))
            {
                Console.WriteLine("appsettings.json path is not set. Cannot save configuration.");
                return false;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Or match the existing file's casing
                };
                var json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(_appSettingsPath, json);
                Console.WriteLine($"Configuration saved to {_appSettingsPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration to {_appSettingsPath}: {ex.Message}");
                return false;
            }
        }
    }
}

