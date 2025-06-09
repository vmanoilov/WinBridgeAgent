# WinBridgeAgent

Secure Windows service for controlled local folder access via named pipes.

- Designed for AI-assisted workflows and local GPT agent usage
- Token-based authentication
- Structured logging with Serilog
- Built-in licensing and attribution

> **Licensed for non-commercial use.** For commercial licensing, see `LICENSE.txt` or `NOTICE.md`.

(c) 2025 Vladislav Manoilov

## Source Layout

The service implementation lives under `src/WinBridgeAgent`. It is a .NET 8
worker service that exposes a named pipe interface for file operations. A token
manager enforces authentication and all activity is logged via Serilog.

## Building from Source

1. Install the **.NET 8 SDK**.
2. Run the publish command from the `src` directory:

   ```bash
   cd src
   dotnet publish -c Release -r win-x64 --self-contained true -o ./publish_output
   ```

   The resulting `publish_output` folder contains `WinBridgeAgent.exe` and
   `appsettings.json`. See `src/INSTALL.txt` for detailed installer and service
   instructions.
