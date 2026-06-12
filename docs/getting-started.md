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

Phase 1 is read-only. It does not send rumble, lightbar, speaker, headset, or raw HID reports.
