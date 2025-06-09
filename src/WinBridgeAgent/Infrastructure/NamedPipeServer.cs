// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WinBridgeAgent.Core;
using WinBridgeAgent.Managers;
using WinBridgeAgent.Security; // Added for ITokenManager

namespace WinBridgeAgent.Infrastructure
{
    // Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
    public class NamedPipeServer : IDisposable
    {
        private const string PipeName = "WinBridgeAgentPipe";
        private readonly ILogger<NamedPipeServer> _logger;
        private readonly IFileOperationManager _fileOperationManager;
        private readonly ITokenManager _tokenManager;
        private readonly SecuritySettings _securitySettings;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _serverTask;

        public NamedPipeServer(
            ILogger<NamedPipeServer> logger,
            IFileOperationManager fileOperationManager,
            ITokenManager tokenManager,
            IOptions<SecuritySettings> securitySettings)
        {
            _logger = logger;
            _fileOperationManager = fileOperationManager;
            _tokenManager = tokenManager;
            _securitySettings = securitySettings.Value;
            _logger.LogInformation("NamedPipeServer initialized. Pipe Name: {PipeName}, RequireAuthentication: {RequireAuth}", PipeName, _securitySettings.RequireAuthentication);
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            _serverTask = Task.Run(() => ListenForConnectionsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            _logger.LogInformation("NamedPipeServer started.");
            return Task.CompletedTask;
        }

        private async Task ListenForConnectionsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var pipeServer = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    _logger.LogInformation("Waiting for client connection...");
                    await pipeServer.WaitForConnectionAsync(token);
                    _logger.LogInformation("Client connected.");

                    _ = HandleConnectionAsync(pipeServer, token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("NamedPipeServer operation canceled. Shutting down listener.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in NamedPipeServer listening loop.");
                    await Task.Delay(1000, token); // Avoid tight loop on persistent errors
                }
            }
            _logger.LogInformation("NamedPipeServer listener stopped.");
        }

        private async Task HandleConnectionAsync(NamedPipeServerStream pipeServer, CancellationToken token)
        {
            try
            {
                using var reader = new StreamReader(pipeServer, Encoding.UTF8);
                using var writer = new StreamWriter(pipeServer, Encoding.UTF8) { AutoFlush = true };

                while (pipeServer.IsConnected && !token.IsCancellationRequested)
                {
                    var requestJson = await reader.ReadLineAsync();
                    if (requestJson == null)
                    {
                        _logger.LogInformation("Client disconnected (requestJson is null).");
                        break;
                    }

                    _logger.LogDebug("Received request: {Request}", requestJson);
                    string responseJson = ProcessRequestInternal(requestJson);
                    _logger.LogDebug("Sending response: {Response}", responseJson);

                    await writer.WriteLineAsync(responseJson);
                }
            }
            catch (IOException ex) when (ex.Message.Contains("pipe is broken") || ex.Message.Contains("Pipe ended"))
            {
                _logger.LogWarning("Client disconnected: {ErrorMessage}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client connection.");
            }
            finally
            {
                if (pipeServer.IsConnected)
                {
                    pipeServer.Disconnect();
                }
                _logger.LogInformation("Finished handling client connection.");
            }
        }

        private string ProcessRequestInternal(string requestJson)
        {
            PipeResponse response;
            try
            {
                var request = JsonSerializer.Deserialize<PipeRequest>(requestJson);
                if (request == null || string.IsNullOrWhiteSpace(request.Command))
                {
                    _logger.LogWarning("Invalid request format or missing command.");
                    return JsonSerializer.Serialize(new PipeResponse { Success = false, ErrorMessage = "Invalid request format or missing command." });
                }

                // Handle GetToken command separately, as it doesn't require prior authentication
                if (string.Equals(request.Command, "GetToken", StringComparison.OrdinalIgnoreCase))
                {
                    var newToken = _tokenManager.GenerateToken();
                    _logger.LogInformation("Log.WinBridge.CreateAudit: GetToken command processed, new token issued.");
                    response = new PipeResponse { Success = true, Data = newToken };
                    return JsonSerializer.Serialize(response);
                }

                // All other commands require authentication if enabled
                if (_securitySettings.RequireAuthentication)
                {
                    if (string.IsNullOrWhiteSpace(request.Token) || !_tokenManager.ValidateToken(request.Token))
                    {
                        _logger.LogWarning("Authentication failed for command {Command}. Token: {Token}", request.Command, request.Token ?? "<null_or_empty>");
                        // Log.WinBridge.CreateAudit for failed auth attempt
                        _logger.LogInformation("Log.WinBridge.CreateAudit: Authentication failure for command {Command}.", request.Command);
                        return JsonSerializer.Serialize(new PipeResponse { Success = false, ErrorMessage = "Authentication failed. Invalid or missing token." });
                    }
                    _logger.LogInformation("Authentication successful for command {Command}. Token: {Token}", request.Command, request.Token);
                    // Log.WinBridge.CreateAudit for successful auth
                     _logger.LogInformation("Log.WinBridge.CreateAudit: Authentication success for command {Command}.", request.Command);
                }
                else
                {
                    _logger.LogInformation("Authentication not required, proceeding with command {Command}.", request.Command);
                }

                // Dispatch to FileOperationManager based on command
                switch (request.Command.ToLowerInvariant())
                {
                    case "createfile":
                        if (request.Content == null || string.IsNullOrWhiteSpace(request.RelativePath))
                            response = new PipeResponse { Success = false, ErrorMessage = "Missing RelativePath or Content for CreateFile." };
                        else
                            response = _fileOperationManager.CreateFile(request.RelativePath, request.Content);
                        break;
                    case "readfile":
                        if (string.IsNullOrWhiteSpace(request.RelativePath))
                            response = new PipeResponse { Success = false, ErrorMessage = "Missing RelativePath for ReadFile." };
                        else
                            response = _fileOperationManager.ReadFile(request.RelativePath);
                        break;
                    case "deletefile":
                        if (string.IsNullOrWhiteSpace(request.RelativePath))
                            response = new PipeResponse { Success = false, ErrorMessage = "Missing RelativePath for DeleteFile." };
                        else
                            response = _fileOperationManager.DeleteFile(request.RelativePath);
                        break;
                    case "listfiles":
                        // RelativePath can be empty for root listing
                        response = _fileOperationManager.ListFiles(request.RelativePath ?? string.Empty);
                        break;
                    default:
                        _logger.LogWarning("Unknown command received: {Command}", request.Command);
                        response = new PipeResponse { Success = false, ErrorMessage = $"Unknown command: {request.Command}" };
                        break;
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization error during request processing.");
                response = new PipeResponse { Success = false, ErrorMessage = "Invalid JSON request: " + jsonEx.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing request.");
                response = new PipeResponse { Success = false, ErrorMessage = "Internal server error: " + ex.Message };
            }
            return JsonSerializer.Serialize(response);
        }

        public void Stop()
        {
            _logger.LogInformation("Stopping NamedPipeServer...");
            _cancellationTokenSource?.Cancel();
            // Allow time for the server task to complete. Consider making this configurable or await _serverTask.
            _serverTask?.Wait(TimeSpan.FromSeconds(5)); 
            _logger.LogInformation("NamedPipeServer stopped.");
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}

