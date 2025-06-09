// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WinBridgeAgentControlPanel.Services;
using WinBridgeAgentControlPanel.Models;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32; // For SaveFileDialog

namespace WinBridgeAgentControlPanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
        private const string WinBridgeAgentServiceName = "WinBridgeAgent"; // Or make configurable
        private readonly WindowsServiceManager _serviceManager;
        private readonly NamedPipeClientService _pipeClient;
        private readonly ConfigurationService _configService;
        private string? _serviceInstallPath;
        private string? _appSettingsPath;
        private string? _logFolderPath;

        public ObservableCollection<LogEntry> AllLogEntries { get; set; }
        public ObservableCollection<LogEntry> FilteredLogEntries { get; set; }


        public MainWindow()
        {
            InitializeComponent();
            this.Title = "WinBridgeAgent Control Panel (v0.1.0) (c) 2025 Vladislav Manoilov";

            _serviceManager = new WindowsServiceManager(WinBridgeAgentServiceName);
            _pipeClient = new NamedPipeClientService();
            _configService = new ConfigurationService();

            AllLogEntries = new ObservableCollection<LogEntry>();
            FilteredLogEntries = new ObservableCollection<LogEntry>();
            LogsListView.ItemsSource = FilteredLogEntries;

            // Wire up event handlers
            RefreshDashboardButton.Click += RefreshDashboardButton_Click;
            StartServiceButton.Click += StartServiceButton_Click;
            StopServiceButton.Click += StopServiceButton_Click;
            RestartServiceButton.Click += RestartServiceButton_Click;
            OpenLogsFolderButton.Click += OpenLogsFolderButton_Click;
            CopyTokenButtonDashboard.Click += CopyTokenButton_Click; // Shared handler
            CopyTokenButtonTokensTab.Click += CopyTokenButton_Click; // Shared handler
            GenerateTokenButton.Click += GenerateTokenButton_Click;
            LoadConfigButton.Click += LoadConfigButton_Click;
            SaveConfigButton.Click += SaveConfigButton_Click;
            BrowseRootFolderButton.Click += BrowseRootFolderButton_Click;
            BrowseLogDirectoryButton.Click += BrowseLogDirectoryButton_Click;
            // Service Tools
            BrowseServiceExecutableButton.Click += BrowseServiceExecutableButton_Click;
            InstallServiceButton.Click += InstallServiceButton_Click;
            UninstallServiceButton.Click += UninstallServiceButton_Click;
            SetStartupTypeButton.Click += SetStartupTypeButton_Click;
            ShowServiceDetailsButton.Click += ShowServiceDetailsButton_Click;
            // Logs Viewer
            ApplyLogFilterButton.Click += ApplyLogFilterButton_Click;
            ExportLogsButton.Click += ExportLogsButton_Click;
            // About
            CopyEmailButton.Click += CopyEmailButton_Click;
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadLicenseTextAsync();
            await RefreshDashboardAsync();
            await LoadConfigurationAsync(); // Also load config for the config tab
            await LoadLogsAsync(); // Load logs on startup
        }

        private async Task LoadLicenseTextAsync()
        {
            try
            {
                if (File.Exists("LICENSE.txt"))
                {
                    LicenseText.Text = await File.ReadAllTextAsync("LICENSE.txt");
                }
                else
                {
                    LicenseText.Text = "LICENSE.txt not found.";
                }
            }
            catch (Exception ex)
            {
                LicenseText.Text = $"Error loading LICENSE.txt: {ex.Message}";
            }
        }

        private async void RefreshDashboardButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDashboardAsync();
        }

        private async Task RefreshDashboardAsync()
        {
            ServiceStatusText.Text = "Status: Refreshing...";
            CurrentTokenText.Text = string.Empty;
            TokenExpiryText.Text = "Expires: N/A";

            var status = _serviceManager.GetServiceStatus();
            ServiceStatusText.Text = $"Status: {status}";

            StartServiceButton.IsEnabled = (status == ServiceState.Stopped);
            StopServiceButton.IsEnabled = (status == ServiceState.Running || status == ServiceState.Paused);
            RestartServiceButton.IsEnabled = (status == ServiceState.Running || status == ServiceState.Paused || status == ServiceState.Stopped && status != ServiceState.NotFound);
            
            _serviceInstallPath = _serviceManager.GetServiceInstallPath();
            if (!string.IsNullOrEmpty(_serviceInstallPath))
            {
                _appSettingsPath = System.IO.Path.Combine(_serviceInstallPath, "appsettings.json");
                _configService.SetAppSettingsPath(_appSettingsPath);
                var config = await _configService.LoadConfigurationAsync();
                if (config?.Logging?.Directory != null)
                {
                    _logFolderPath = System.IO.Path.IsPathRooted(config.Logging.Directory) ? config.Logging.Directory : System.IO.Path.Combine(_serviceInstallPath, config.Logging.Directory);
                    OpenLogsFolderButton.IsEnabled = Directory.Exists(_logFolderPath);
                }
                else
                {
                    OpenLogsFolderButton.IsEnabled = false;
                }
            }
            else
            {
                _appSettingsPath = null;
                _configService.SetAppSettingsPath(string.Empty);
                 OpenLogsFolderButton.IsEnabled = false;
            }

            if (status == ServiceState.Running)
            {
                var tokenResponse = await _pipeClient.GetTokenAsync();
                if (tokenResponse?.Success == true && tokenResponse.Data is JsonElement tokenElement && tokenElement.ValueKind == JsonValueKind.String)
                {
                    CurrentTokenText.Text = tokenElement.GetString();
                    var config = await _configService.LoadConfigurationAsync(); 
                    if (config?.Security?.TokenValidityMinutes > 0)
                    {
                        TokenExpiryText.Text = $"Expires: (approx. {config.Security.TokenValidityMinutes} mins from generation)";
                    }
                }
                else
                {
                    CurrentTokenText.Text = tokenResponse?.ErrorMessage ?? "Failed to retrieve token.";
                }
            }
            else
            {
                 CurrentTokenText.Text = "Service not running.";
            }
        }

        private async void StartServiceButton_Click(object sender, RoutedEventArgs e)
        {
            ServiceStatusText.Text = "Status: Starting...";
            bool success = await _serviceManager.StartServiceAsync();
            MessageBox.Show(success ? "Service started successfully." : "Failed to start service.", "Service Control");
            await RefreshDashboardAsync();
        }

        private async void StopServiceButton_Click(object sender, RoutedEventArgs e)
        {
            ServiceStatusText.Text = "Status: Stopping...";
            bool success = await _serviceManager.StopServiceAsync();
            MessageBox.Show(success ? "Service stopped successfully." : "Failed to stop service.", "Service Control");
            await RefreshDashboardAsync();
        }

        private async void RestartServiceButton_Click(object sender, RoutedEventArgs e)
        {
            ServiceStatusText.Text = "Status: Restarting...";
            bool success = await _serviceManager.RestartServiceAsync();
            MessageBox.Show(success ? "Service restarted successfully." : "Failed to restart service.", "Service Control");
            await RefreshDashboardAsync();
        }

        private void OpenLogsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_logFolderPath) && Directory.Exists(_logFolderPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _logFolderPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open logs folder: {_logFolderPath}\nError: {ex.Message}", "Error");
                }
            }
            else
            {
                MessageBox.Show("Log folder path is not configured or does not exist. Please check service configuration.", "Log Folder");
            }
        }

        private void CopyTokenButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox? tokenBoxToCopyFrom = null;
            if (sender == CopyTokenButtonDashboard) tokenBoxToCopyFrom = CurrentTokenText;
            else if (sender == CopyTokenButtonTokensTab) tokenBoxToCopyFrom = GeneratedTokenText;

            if (tokenBoxToCopyFrom != null && !string.IsNullOrEmpty(tokenBoxToCopyFrom.Text) && tokenBoxToCopyFrom.Text != "Service not running." && !tokenBoxToCopyFrom.Text.StartsWith("Failed"))
            {
                try
                {
                    Clipboard.SetText(tokenBoxToCopyFrom.Text);
                    MessageBox.Show("Token copied to clipboard.", "Copy Token");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not copy token to clipboard: {ex.Message}", "Error");
                }
            }
            else
            {
                MessageBox.Show("No valid token to copy.", "Copy Token");
            }
        }

        private async Task LoadConfigurationAsync()
        {
            if (string.IsNullOrEmpty(_appSettingsPath))
            {
                _serviceInstallPath = _serviceManager.GetServiceInstallPath();
                 if (!string.IsNullOrEmpty(_serviceInstallPath))
                {
                    _appSettingsPath = System.IO.Path.Combine(_serviceInstallPath, "appsettings.json");
                    _configService.SetAppSettingsPath(_appSettingsPath);
                }
            }

            var settings = await _configService.LoadConfigurationAsync();
            if (settings != null)
            {
                RootFolderText.Text = settings.Service?.RootFolder ?? string.Empty;
                MaxFileSizeText.Text = (settings.Service?.MaxFileSize / (1024 * 1024))?.ToString() ?? "100"; 
                AllowedExtensionsText.Text = string.Join(",", settings.Service?.AllowedExtensions ?? new string[]{ "*" });
                TokenValidityText.Text = settings.Security?.TokenValidityMinutes.ToString() ?? "60";
                RequireAuthenticationCheck.IsChecked = settings.Security?.RequireAuthentication ?? true;
                LogDirectoryText.Text = settings.Logging?.Directory ?? string.Empty;
                _logFolderPath = settings.Logging?.Directory; // Update log folder path for viewer
                 if (!string.IsNullOrEmpty(_serviceInstallPath) && !System.IO.Path.IsPathRooted(_logFolderPath)){
                    _logFolderPath = System.IO.Path.Combine(_serviceInstallPath, _logFolderPath);
                }

                LogRetentionText.Text = settings.Logging?.RetentionDays.ToString() ?? "30";
                string logLevel = settings.Logging?.MinimumLevel ?? "Information";
                foreach (ComboBoxItem item in LogLevelCombo.Items)
                {
                    if (item.Content.ToString().Equals(logLevel, StringComparison.OrdinalIgnoreCase))
                    {
                        LogLevelCombo.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                MessageBox.Show("Could not load configuration. Service might not be installed or appsettings.json is missing/invalid.", "Configuration Error");
            }
        }

        private async void LoadConfigButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadConfigurationAsync();
        }

        private async void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (!long.TryParse(MaxFileSizeText.Text, out long maxFileSizeMB) || maxFileSizeMB <=0) { MessageBox.Show("Invalid Max File Size.","Error"); return; }
            if (!int.TryParse(TokenValidityText.Text, out int tokenValidityMins) || tokenValidityMins <=0) { MessageBox.Show("Invalid Token Validity.","Error"); return; }
            if (!int.TryParse(LogRetentionText.Text, out int logRetentionDays) || logRetentionDays <=0) { MessageBox.Show("Invalid Log Retention Days.","Error"); return; }
            if (string.IsNullOrWhiteSpace(RootFolderText.Text)) { MessageBox.Show("Root Folder cannot be empty.","Error"); return; }
            if (string.IsNullOrWhiteSpace(LogDirectoryText.Text)) { MessageBox.Show("Log Directory cannot be empty.","Error"); return; }
            if (string.IsNullOrWhiteSpace(AllowedExtensionsText.Text)) { MessageBox.Show("Allowed Extensions cannot be empty. Use 	'*' for all.","Error"); return; }

            var settings = new ServiceAppSettings
            {
                Service = new ServiceSettingsSection
                {
                    RootFolder = RootFolderText.Text,
                    MaxFileSize = maxFileSizeMB * 1024 * 1024,
                    AllowedExtensions = AllowedExtensionsText.Text.Split(	',	').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray()
                },
                Security = new SecuritySettingsSection
                {
                    TokenValidityMinutes = tokenValidityMins,
                    RequireAuthentication = RequireAuthenticationCheck.IsChecked ?? true
                },
                Logging = new LoggingSettingsSection
                {
                    Directory = LogDirectoryText.Text,
                    RetentionDays = logRetentionDays,
                    MinimumLevel = (LogLevelCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Information"
                }
            };

            bool success = await _configService.SaveConfigurationAsync(settings);
            MessageBox.Show(success ? "Configuration saved successfully. Restart service for changes to take full effect." : "Failed to save configuration.", "Configuration Save");
            if(success) { 
                await RefreshDashboardAsync(); 
                await LoadLogsAsync(); // Reload logs if path changed
            }
        }

        private void BrowseRootFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RootFolderText.Text = dialog.SelectedPath;
            }
        }
        private void BrowseLogDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.W
(Content truncated due to size limit. Use line ranges to read in chunks)