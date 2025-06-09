// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
namespace WinBridgeAgent.Core
{
    public class ServiceSettings
    {
        public string RootFolder { get; set; } = "C:\\BridgeRoot";
        public long MaxFileSize { get; set; } = 104857600; // 100 MB
        public string[] AllowedExtensions { get; set; } = ["*"];
    }
}

