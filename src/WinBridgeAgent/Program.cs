// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using WinBridgeAgent.Core;
using WinBridgeAgent.Infrastructure;
using WinBridgeAgent.Managers;
using WinBridgeAgent.Security;

namespace WinBridgeAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
            Console.WriteLine("[WinBridgeAgent] Non-commercial license. Contact vlad [at] gmail [dot] com for commercial terms.");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                // Placeholder for file logging, will be configured from appsettings.json
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Starting WinBridgeAgent service host.");
                // Log required startup event
                Log.Information("{{\"Event\": \"ServiceStart\", \"Origin\": \"WinBridgeAgent\", \"Author\": \"Vladislav Manoilov\", \"License\": \"NonCommercial\"}}");

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "WinBridgeAgent service host terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "WinBridgeAgent";
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Configuration already handled by CreateDefaultBuilder
                })
                .UseSerilog((hostingContext, loggerConfiguration) => {
                    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(); 
                        // File sink will be configured via appsettings.json by Serilog.Settings.Configuration
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Load configuration
                    services.Configure<ServiceSettings>(hostContext.Configuration.GetSection("Service"));
                    services.Configure<SecuritySettings>(hostContext.Configuration.GetSection("Security"));
                    services.Configure<LoggingSettings>(hostContext.Configuration.GetSection("Logging"));

                    // Register services
                    services.AddSingleton<ITokenManager, SecurityTokenManagerVlad>();
                    services.AddSingleton<IFileOperationManager, FileOperationManager>();
                    services.AddSingleton<NamedPipeServer>(); // NamedPipeServer manages its own lifecycle
                    services.AddHostedService<Worker>();
                });
    }
}

