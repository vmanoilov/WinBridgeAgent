# ChatGPTFileBridge Project

This package contains all the necessary source files for building the ChatGPTFileBridge application on a Windows machine.

## Package Contents

1. **Source Code Files**
   - WinBridgeAgent (Windows Service component)
   - WinBridgeAgentControlPanel (WPF UI application)
   - ChatGPTFileBridge API and UI components

2. **Project Files**
   - .csproj files for all components
   - Solution files (.sln)
   - Configuration files

3. **Build and Installation**
   - WinBridgeAgentInstaller.nsi (NSIS installer script)
   - build_instructions.md (Detailed build guide)
   - INSTALL.txt and INSTALLER_README.md

4. **Documentation**
   - README.md (This file)
   - LICENSE.txt
   - NOTICE.md

## Build Requirements

- Windows 10/11 operating system
- .NET 8.0 SDK
- Visual Studio 2022 (recommended) or Visual Studio Code with C# extensions
- NSIS (Nullsoft Scriptable Install System) for creating the installer

## Quick Start

1. Extract all files to a directory on your Windows machine
2. Open the solution file in Visual Studio 2022
3. Build the solution
4. Use NSIS to compile the installer script

For detailed instructions, please refer to the `build_instructions.md` file.

## Project Structure

This project consists of two main components:

1. **WinBridgeAgent** - A Windows service that handles file operations
2. **WinBridgeAgentControlPanel** - A WPF UI application for controlling the service

The project also includes the ChatGPTFileBridge API and UI components which appear to be part of the same system.

## Notes

- This project contains Windows-specific components (WPF, Windows Services) and must be built on a Windows machine
- The .NET 8.0 SDK is required for compilation
- All necessary dependencies are specified in the project files
