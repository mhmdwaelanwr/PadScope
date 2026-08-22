# Getting Started

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- Visual Studio 2022, Rider, or the .NET CLI

## Build

From the repository root:

```powershell
cd src
dotnet restore
dotnet build PadScope.sln
```

## Run the CLI scanner

```powershell
dotnet run --project PadScope.Cli -- scan
```

JSON output:

```powershell
dotnet run --project PadScope.Cli -- scan --json
```

## Run the desktop app

```powershell
dotnet run --project PadScope.Desktop
```

The desktop app is the recommended starting point. Scan first, select the exact device, and verify live input before enabling controlled actions.

## Optional drivers

- ViGEmBus is required only for virtual DualShock 4 or Xbox 360 output.
- HidHide is optional and helps prevent double input when a game sees both the physical and virtual controller.

PadScope reports whether these drivers are detected. Install drivers only from their official project pages and restart Windows if requested.

## Portable release

Tagged releases include a self-contained `PadScope-win-x64.zip`. Extract it to a normal folder and run `PadScope.Desktop.exe`; a separate .NET installation is not required.

## First real-world test

1. Connect the controller by USB.
2. Run the CLI scanner.
3. Export or copy the result.
4. Disconnect USB and pair over Bluetooth.
5. Run the scanner again.
6. Compare VID/PID, device name, path, and connection type.

For Marvo GT-84 research, the most important first data is:

- USB VID/PID
- Bluetooth VID/PID or device instance path
- Whether Windows exposes any controller-related audio endpoint
- Whether the same device appears under multiple names

## Safety note

Normal scanning and report export are read-only. Rumble, lightbar, virtual-controller, mouse, and audio actions are separate user-triggered features and require confirmation where appropriate. Verify the displayed target before continuing.
