// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
namespace WinBridgeAgent.Core
{
    public class SecuritySettings
    {
        public int TokenValidityMinutes { get; set; } = 60;
        public bool RequireAuthentication { get; set; } = true;
    }
}

