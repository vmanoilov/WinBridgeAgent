// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
namespace WinBridgeAgent.Core
{
    public class LoggingSettings
    {
        public string Directory { get; set; } = "C:\\BridgeLogs";
        public int RetentionDays { get; set; } = 30;
        public string MinimumLevel { get; set; } = "Information"; // Serilog.Events.LogEventLevel
    }
}

