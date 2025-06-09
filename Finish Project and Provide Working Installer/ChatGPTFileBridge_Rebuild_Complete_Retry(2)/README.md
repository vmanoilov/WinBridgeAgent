# WinBridgeAgent

**WinBridgeAgent** is a secure Windows service for controlled local folder access via named pipes—designed for GPT-powered tools and automation agents running locally.

> 🔒 Licensed for non-commercial use only.  
> 📜 See [`LICENSE.txt`](./LICENSE.txt) and [`NOTICE.md`](./NOTICE.md) for details.

---

## 🚀 Features

- File operations sandboxed to a root directory
- Named pipe communication (local, bi-directional, secure)
- Token-based authentication for clients
- Structured JSON logging via Serilog
- Built-in service fingerprinting and licensing metadata

---

## 🛠 Usage

This project is a starter template. You'll need to implement:

- `FileOperationManager` (I/O logic with path validation)
- `TokenManager` (auth logic)
- `NamedPipeServer` (IPC layer)
- `ServiceHost` (`Program.cs`, `Startup.cs`)

See `src/Core/Interfaces/IFileOperationManager.cs` for the starting contract.

---

## 📫 Licensing & Commercial Use

This code is **free for personal and non-commercial use**.  
For commercial licensing (e.g., SaaS, enterprise apps), contact:

`vlad [at] gmail [dot] com` — subject: *"WinBridgeAgent Commercial License"*

---

(c) 2025 Vladislav Manoilov