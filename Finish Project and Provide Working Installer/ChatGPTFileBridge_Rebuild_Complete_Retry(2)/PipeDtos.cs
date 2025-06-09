// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
namespace WinBridgeAgentControlPanel.Models
{
    // Part of WinBridgeAgent (c) 2025 Vladislav Manoilov

    /// <summary>
    /// Represents a request sent over the named pipe to the WinBridgeAgent service.
    /// This should mirror the PipeRequest DTO in the WinBridgeAgent service.
    /// </summary>
    public class PipeRequest
    {
        public string? Token { get; set; }
        public string? Command { get; set; }
        public string? RelativePath { get; set; }
        public byte[]? Content { get; set; } // For CreateFile
        // Add other parameters as needed for different commands if the GUI needs to send them.
        // For now, primarily for GetToken.
    }

    /// <summary>
    /// Represents a response received over the named pipe from the WinBridgeAgent service.
    /// This should mirror the PipeResponse DTO in the WinBridgeAgent service.
    /// </summary>
    public class PipeResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public object? Data { get; set; } // Can be string (token), list of files, file content etc.
    }

    // Specific DTO for ListFiles if the Data object needs strong typing
    public class FileListItem
    {
        public string? Name { get; set; }
        public string? Type { get; set; } // "File" or "Directory"
        public string? RelativePath { get; set; }
        public long Size { get; set; } // Optional, if service provides it
        public DateTime LastModified { get; set; } // Optional, if service provides it
    }
}

