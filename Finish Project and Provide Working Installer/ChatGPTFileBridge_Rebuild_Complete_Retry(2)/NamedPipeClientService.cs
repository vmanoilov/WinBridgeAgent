// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
using System;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WinBridgeAgentControlPanel.Models;

namespace WinBridgeAgentControlPanel.Services
{
    // Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
    public class NamedPipeClientService
    {
        private const string PipeName = "WinBridgeAgentPipe"; // Should match the service's pipe name
        private const int ConnectionTimeoutMs = 5000; // 5 seconds timeout for connection

        public NamedPipeClientService()
        {
        }

        private async Task<string?> SendRequestAsync(PipeRequest request)
        {
            try
            {
                using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await pipeClient.ConnectAsync(ConnectionTimeoutMs);

                if (!pipeClient.IsConnected)
                {
                    // Log or handle connection failure
                    Console.WriteLine("Failed to connect to the WinBridgeAgent service pipe.");
                    return null;
                }

                var requestJson = JsonSerializer.Serialize(request);
                using var writer = new StreamWriter(pipeClient, Encoding.UTF8) { AutoFlush = true };
                await writer.WriteLineAsync(requestJson);

                using var reader = new StreamReader(pipeClient, Encoding.UTF8);
                var responseJson = await reader.ReadLineAsync();
                
                pipeClient.Close();
                return responseJson;
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Connection to pipe timed out: {ex.Message}");
                // Log or handle timeout
                return JsonSerializer.Serialize(new PipeResponse { Success = false, ErrorMessage = "Connection to service timed out." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error communicating with pipe: {ex.Message}");
                // Log or handle other exceptions
                return JsonSerializer.Serialize(new PipeResponse { Success = false, ErrorMessage = $"Error communicating with service: {ex.Message}" });
            }
        }

        public async Task<PipeResponse?> GetTokenAsync()
        {
            var request = new PipeRequest { Command = "GetToken" };
            var responseJson = await SendRequestAsync(request);
            if (responseJson != null)
            {
                try
                {
                    return JsonSerializer.Deserialize<PipeResponse>(responseJson);
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"Error deserializing GetToken response: {jsonEx.Message}");
                    return new PipeResponse { Success = false, ErrorMessage = "Invalid response format from service." };
                }
            }
            return new PipeResponse { Success = false, ErrorMessage = "No response from service." };
        }

        // Add other methods for different commands as needed, e.g.:
        // public async Task<PipeResponse?> CreateFileAsync(string token, string relativePath, byte[] content)
        // {
        //     var request = new PipeRequest 
        //     { 
        //         Token = token, 
        //         Command = "CreateFile", 
        //         RelativePath = relativePath, 
        //         Content = content 
        //     };
        //     var responseJson = await SendRequestAsync(request);
        //     if (responseJson != null) 
        //     {
        //         return JsonSerializer.Deserialize<PipeResponse>(responseJson);
        //     }
        //     return null;
        // }

        // For now, the GUI primarily needs GetToken. Other file operations are done by the service itself,
        // and the GUI manages the service and its configuration rather than directly using its file operations.
        // If the GUI needed to, for example, test file operations, then more methods would be added here.
    }
}

