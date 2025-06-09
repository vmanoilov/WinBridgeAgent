// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WinBridgeAgentControlPanel.Services
{
    // Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
    public enum ServiceState
    {
        NotFound,
        Stopped,
        StartPending,
        StopPending,
        Running,
        ContinuePending,
        PausePending,
        Paused,
        Unknown
    }

    public class WindowsServiceManager
    {
        private readonly string _serviceName;

        public WindowsServiceManager(string serviceName)
        {
            _serviceName = serviceName;
        }

        public ServiceState GetServiceStatus()
        {
            try
            {
                using var serviceController = new ServiceController(_serviceName);
                return (ServiceState)(int)serviceController.Status;
            }
            catch (InvalidOperationException ex) // Typically means service not found
            {
                Console.WriteLine($"Service \'{_serviceName}\' not found or error accessing: {ex.Message}");
                return ServiceState.NotFound;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting service status for \'{_serviceName}\': {ex.Message}");
                return ServiceState.Unknown;
            }
        }

        public async Task<bool> StartServiceAsync(int timeoutMilliseconds = 15000)
        {
            try
            {
                using var serviceController = new ServiceController(_serviceName);
                if (serviceController.Status == ServiceControllerStatus.Stopped)
                {
                    serviceController.Start();
                    await Task.Run(() => serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(timeoutMilliseconds)));
                    return serviceController.Status == ServiceControllerStatus.Running;
                }
                return serviceController.Status == ServiceControllerStatus.Running; // Already running
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting service \'{_serviceName}\': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StopServiceAsync(int timeoutMilliseconds = 15000)
        {
            try
            {
                using var serviceController = new ServiceController(_serviceName);
                if (serviceController.Status == ServiceControllerStatus.Running || serviceController.Status == ServiceControllerStatus.Paused)
                {
                    serviceController.Stop();
                    await Task.Run(() => serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(timeoutMilliseconds)));
                    return serviceController.Status == ServiceControllerStatus.Stopped;
                }
                return serviceController.Status == ServiceControllerStatus.Stopped; // Already stopped
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping service \'{_serviceName}\': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RestartServiceAsync(int stopTimeoutMilliseconds = 15000, int startTimeoutMilliseconds = 15000)
        {
            try
            {
                if (await StopServiceAsync(stopTimeoutMilliseconds))
                {
                    return await StartServiceAsync(startTimeoutMilliseconds);
                }
                // If it wasn't running and couldn't be stopped (e.g. not found), try starting it anyway if it exists
                if (GetServiceStatus() == ServiceState.NotFound) return false;
                return await StartServiceAsync(startTimeoutMilliseconds); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restarting service \'{_serviceName}\': {ex.Message}");
                return false;
            }
        }

        public string GetServiceInstallPath()
        {
            // This is more complex and usually requires querying the registry
            // HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\<ServiceName> -> ImagePath
            // This requires Microsoft.Win32.Registry, which is Windows-specific.
            try
            {
                using var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($"SYSTEM\CurrentControlSet\Services\{_serviceName}");
                if (regKey != null)
                {
                    var imagePath = regKey.GetValue("ImagePath")?.ToString();
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        // ImagePath might be quoted and contain arguments, e.g., "C:\path\to\service.exe" /arg
                        imagePath = imagePath.Trim(\'\"	 
');
                        if (imagePath.StartsWith("\"", StringComparison.Ordinal) && imagePath.Contains("\" "))
                        {
                            imagePath = imagePath.Substring(0, imagePath.IndexOf("\" ", StringComparison.Ordinal) + 1);
                        }
                        else if (!imagePath.StartsWith("\"", StringComparison.Ordinal) && imagePath.Contains(" "))
                        {
                            // Unquoted path with arguments, take the first part if it ends with .exe
                            var parts = imagePath.Split(\' 	\');
                            if (parts.Length > 0 && parts[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                imagePath = parts[0];
                            }
                        }
                        return System.IO.Path.GetDirectoryName(imagePath.Trim(\'\"	 
'));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting service install path for \'{_serviceName}\': {ex.Message}");
            }
            return string.Empty;
        }

        // Methods for Install/Uninstall/Change Startup Type would typically use sc.exe or PowerShell
        // as ServiceController doesn't directly support these. These will be implemented later, likely
        // by shelling out to sc.exe or PowerShell cmdlets, requiring admin privileges.

    }
}

