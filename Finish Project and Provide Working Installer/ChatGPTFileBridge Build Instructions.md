# ChatGPTFileBridge Build Instructions

This document provides instructions for building the ChatGPTFileBridge project on a Windows machine.

## Prerequisites

1. **Windows 10/11** operating system
2. **.NET 8.0 SDK** - [Download from Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0)
3. **Visual Studio 2022** (recommended) or **Visual Studio Code** with C# extensions

## Build Steps

### Option 1: Using Visual Studio 2022

1. Open the solution file `WinBridgeAgent.sln` or `WinBridgeAgentControlPanel.sln` in Visual Studio 2022
2. Select the build configuration (Debug/Release)
3. Right-click on the solution in Solution Explorer and select "Build Solution"
4. The compiled output will be available in the `bin` directory of each project

### Option 2: Using .NET CLI

1. Open a command prompt or PowerShell window
2. Navigate to the directory containing the solution file
3. Run the following commands:

```powershell
# Build the WinBridgeAgent (service component)
dotnet build WinBridgeAgent.csproj -c Release

# Build the WinBridgeAgentControlPanel (UI component)
dotnet build WinBridgeAgentControlPanel.csproj -c Release
```

## Creating an Installer

To create an installer for the application, you'll need:

1. **NSIS (Nullsoft Scriptable Install System)** - [Download from NSIS website](https://nsis.sourceforge.io/Download)
2. Build both projects in Release configuration
3. Compile the installer script:
   - Right-click on `WinBridgeAgentInstaller.nsi` and select "Compile NSIS Script"
   - Or use the NSIS compiler from command line: `makensis WinBridgeAgentInstaller.nsi`

The installer will be created in the same directory as the NSIS script.

## Project Structure

- **WinBridgeAgent**: Windows service component that handles file operations
- **WinBridgeAgentControlPanel**: WPF UI application for controlling the service

## Troubleshooting

- Ensure all NuGet packages are restored before building
- If you encounter any build errors, check that you have the correct .NET SDK version (8.0)
- For Windows-specific API issues, ensure you're building on a Windows machine
