// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using WinBridgeAgent.Infrastructure;

namespace WinBridgeAgent.Core
{
    public class Worker : BackgroundService
    {
        // Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
        private readonly ILogger<Worker> _logger;
        private readonly NamedPipeServer _namedPipeServer;
        private readonly ServiceSettings _serviceSettings;

        public Worker(ILogger<Worker> logger, NamedPipeServer namedPipeServer, IOptions<ServiceSettings> serviceSettings)
        {
            _logger = logger;
            _namedPipeServer = namedPipeServer;
            _serviceSettings = serviceSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WinBridgeAgent Worker running at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Root folder configured: {RootFolder}", _serviceSettings.RootFolder);

            try
            {
                // Start the named pipe server
                await _namedPipeServer.StartAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running the NamedPipeServer.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                // Keep the service alive while the named pipe server is running in its own task.
                // The actual work is done by the NamedPipeServer handling client connections.
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WinBridgeAgent Worker stopping at: {time}", DateTimeOffset.Now);
            _namedPipeServer.Stop();
            await base.StopAsync(stoppingToken);
        }
    }
}

