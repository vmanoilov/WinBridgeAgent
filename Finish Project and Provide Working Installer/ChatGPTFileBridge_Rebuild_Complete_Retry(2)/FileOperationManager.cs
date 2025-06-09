// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.Json;
using WinBridgeAgent.Core;

namespace WinBridgeAgent.Managers
{
    // Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
    public interface IFileOperationManager
    {
        PipeResponse CreateFile(string relativePath, byte[] content);
        PipeResponse ReadFile(string relativePath);
        PipeResponse DeleteFile(string relativePath);
        PipeResponse ListFiles(string relativePath);
    }

    public class FileOperationManager : IFileOperationManager
    {
        private readonly ILogger<FileOperationManager> _logger;
        private readonly ServiceSettings _serviceSettings;
        private readonly string _rootFolder;

        public FileOperationManager(ILogger<FileOperationManager> logger, IOptions<ServiceSettings> serviceSettings)
        {
            _logger = logger;
            _serviceSettings = serviceSettings.Value;
            _rootFolder = Path.GetFullPath(_serviceSettings.RootFolder);

            if (!Directory.Exists(_rootFolder))
            {
                try
                {
                    Directory.CreateDirectory(_rootFolder);
                    _logger.LogInformation("Root folder created: {RootFolder}", _rootFolder);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed to create root folder: {RootFolder}. File operations will likely fail.", _rootFolder);
                    // Depending on requirements, this could be a fatal error for the service.
                }
            }
            _logger.LogInformation("FileOperationManager initialized. Root Folder: {RootFolder}, MaxFileSize: {MaxFileSize}, AllowedExtensions: {AllowedExtensions}", 
                _rootFolder, _serviceSettings.MaxFileSize, string.Join(", ", _serviceSettings.AllowedExtensions));
        }

        private bool IsPathSafe(string relativePath, out string fullPath)
        {
            fullPath = string.Empty;
            try
            {
                // Combine and normalize the path
                string combinedPath = Path.Combine(_rootFolder, relativePath);
                fullPath = Path.GetFullPath(combinedPath);

                // Check if the normalized path is still within the root folder
                if (!fullPath.StartsWith(_rootFolder, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Path traversal attempt detected. RelativePath: {RelativePath}, FullPath: {FullPath}, RootFolder: {RootFolder}", relativePath, fullPath, _rootFolder);
                    return false;
                }
                return true;
            }
            catch (ArgumentException ex) // Catches invalid characters in path
            {
                _logger.LogWarning(ex, "Invalid path characters. RelativePath: {RelativePath}", relativePath);
                return false;
            }
            catch (SecurityException ex)
            {
                _logger.LogWarning(ex, "Path security exception. RelativePath: {RelativePath}", relativePath);
                return false;
            }
            catch (PathTooLongException ex)
            {
                _logger.LogWarning(ex, "Path too long. RelativePath: {RelativePath}", relativePath);
                return false;
            }
        }

        private bool IsExtensionAllowed(string fileName)
        {
            if (_serviceSettings.AllowedExtensions.Contains("*"))
            {
                return true;
            }
            var extension = Path.GetExtension(fileName).TrimStart('.');
            return _serviceSettings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        public PipeResponse CreateFile(string relativePath, byte[] content)
        {
            _logger.LogInformation("Attempting to create file: {RelativePath}", relativePath);
            if (!IsPathSafe(relativePath, out string fullPath))
            {
                return new PipeResponse { Success = false, ErrorMessage = "Invalid or unsafe path." };
            }

            if (!IsExtensionAllowed(Path.GetFileName(fullPath)))
            {
                _logger.LogWarning("File creation denied due to disallowed extension: {FullPath}", fullPath);
                return new PipeResponse { Success = false, ErrorMessage = "File extension not allowed." };
            }

            if (content.Length > _serviceSettings.MaxFileSize)
            {
                _logger.LogWarning("File creation denied due to size limit. Size: {ContentLength}, MaxSize: {MaxFileSize}", content.Length, _serviceSettings.MaxFileSize);
                return new PipeResponse { Success = false, ErrorMessage = $"File size exceeds maximum allowed ({_serviceSettings.MaxFileSize} bytes)." };
            }

            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(fullPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(fullPath, content);
                _logger.LogInformation("Log.WinBridge.CreateAudit: File created successfully: {FullPath}", fullPath);
                return new PipeResponse { Success = true, Data = $"File '{relativePath}' created successfully." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating file: {FullPath}", fullPath);
                return new PipeResponse { Success = false, ErrorMessage = "Error creating file: " + ex.Message };
            }
        }

        public PipeResponse ReadFile(string relativePath)
        {
            _logger.LogInformation("Attempting to read file: {RelativePath}", relativePath);
            if (!IsPathSafe(relativePath, out string fullPath))
            {
                return new PipeResponse { Success = false, ErrorMessage = "Invalid or unsafe path." };
            }

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found for reading: {FullPath}", fullPath);
                return new PipeResponse { Success = false, ErrorMessage = "File not found." };
            }
            
            if (!IsExtensionAllowed(Path.GetFileName(fullPath)))
            {
                _logger.LogWarning("File read denied due to disallowed extension: {FullPath}", fullPath);
                return new PipeResponse { Success = false, ErrorMessage = "File extension not allowed for reading." };
            }

            try
            {
                var content = File.ReadAllBytes(fullPath);
                _logger.LogInformation("Log.WinBridge.CreateAudit: File read successfully: {FullPath}", fullPath);
                return new PipeResponse { Success = true, Data = content };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {FullPath}", fullPath);
                return new PipeResponse { Success = false, ErrorMessage = "Error reading file: " + ex.Message };
            }
        }

        public PipeResponse DeleteFile(string relativePath)
        {
            _logger.LogInformation("Attempting to delete file: {RelativePath}", relativePath);
            if (!IsPathSafe(relativePath, out string fullPath))
            {
                return new PipeResponse { Success = false, ErrorMessage = "Invalid or unsafe path." };
            }

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found for deletion: {FullPath}", fullPath);
                return new PipeResponse { Success = false, ErrorMessage = "File not found." };
            }

            if (!IsExtensionAllowed(Path.GetFileName(fullPath))) // Check extension even for delete for consistency, though less critical
            {
                _logger.LogWarning("File deletion denied due to disallowed extension: {FullPath}", fullPath);
                return new PipeResponse { Success = false, ErrorMessage = "File extension not allowed for deletion." };
            }

            try
            {
                File.Delete(fullPath);
                _logger.LogInformation("Log.WinBridge.CreateAudit: File deleted successfully: {FullPath}", fullPath);
                return new PipeResponse { Success = true, Data = $"File '{relativePath}' deleted successfully." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FullPath}", fullPath);
                return new PipeResponse { Success = false, ErrorMessage = "Error deleting file: " + ex.Message };
            }
        }

        public PipeResponse ListFiles(string relativePath)
        {
            _logger.LogInformation("Attempting to list files in: {RelativePath}", relativePath);
            if (!IsPathSafe(relativePath, out string fullPath))
            {
                return new PipeResponse { Success = false, ErrorMessage = "Invalid or unsafe path." };
            }

            if (!Directory.Exists(fullPath))
            {
                _logger.LogWarning("Directory not found for listing: {FullPath}", fullPath);
                return new PipeResponse { Success = false, ErrorMessage = "Directory not found." };
            }

            try
            {
                var entries = new List<object>();
                foreach (var dir in Directory.GetDirectories(fullPath))
                {
                    entries.Add(new { Name = Path.GetFileName(dir), Type = "Directory", RelativePath = Path.GetRelativePath(_rootFolder, dir).Replace('\\', '/') });
                }
                foreach (var file in Directory.GetFiles(fullPath))
                {
                    if (IsExtensionAllowed(Path.GetFileName(file)))
                    {
                        entries.Add(new { Name = Path.GetFileName(file), Type = "File", RelativePath = Path.GetRelativePath(_rootFolder, file).Replace('\\', '/') });
                    }
                }
                _logger.LogInformation("Log.WinBridge.CreateAudit: Files listed successfully for: {FullPath}", fullPath);
                return new PipeResponse { Success = true, Data = entries };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files in: {FullPath}", fullPath);
                return new PipeResponse { Success = false, ErrorMessage = "Error listing files: " + ex.Message };
            }
        }
    }
}

