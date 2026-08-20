# PadScope - Windows Development Prompt

## Quick Setup (Copy this)

```
I'm continuing development on PadScope, a Windows gamepad diagnostics and DS4Windows-parity toolkit.

Repository: https://github.com/mhmdwaelanwr/PadScope.git
Branch: main
Last commit: fcb92a4

Setup steps:
1. git clone https://github.com/mhmdwaelanwr/PadScope.git
2. Install .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
3. Install ViGEmBus driver from https://github.com/nefarius/ViGEmBus/releases
4. Install HidHide (optional) from https://github.com/ViGEm/HidHide/releases
5. dotnet build src/PadScope.sln
6. Connect DS4/DualSense controller via USB
7. dotnet run --project src/PadScope.Cli -- scan
8. dotnet run --project src/PadScope.Cli -- audio --probe
9. dotnet run --project src/PadScope.Cli -- audio --list
10. dotnet run --project src/PadScope.Desktop
```

## Hardware

- DS4/DualSense controller (USB + Bluetooth)
- Ready to test: Audio Lab, virtual controllers, rumble, lightbar, macros, mouse emulation

## Current State

All 18 stages implemented:

| Stage | Feature | Status |
|-------|---------|--------|
| 0 | Build verification | Done |
| 1 | Empty scan | Done |
| 2 | USB scan | Done |
| 3 | Bluetooth scan | Done |
| 4 | Profile validation | Done |
| 5 | HID inspection | Needs hardware |
| 6 | Rumble | Needs hardware |
| 7 | Lightbar | Needs hardware |
| 8 | Touchpad/Gyro | Needs hardware |
| 9 | Audio endpoint check | Done |
| 10 | Audio Lab WASAPI | Done, needs test |
| 11 | Packaging | Done |
| 12 | Virtual controller | Done |
| 13 | Remapping | Done |
| 14 | HidHide | Done |
| 15 | Touchpad mouse | Done |
| 16 | Gyro mouse | Done |
| 17 | Macros | Done |

## What to test on Windows

1. Connect DS4/DualSense via USB
2. Run CLI scan to detect the controller
3. Test audio probe and WASAPI device listing
4. Open Desktop app, test each tab
5. Test rumble and lightbar with confirmation
6. Test virtual controller passthrough (DS4 + Xbox360)
7. Test touchpad/gyro mouse emulation
8. Test macros with example profile

## Key Files

| File | Purpose |
|------|---------|
| src/PadScope.Cli/Program.cs | CLI entry point |
| src/PadScope.Desktop/MainWindow.xaml | Desktop UI layout |
| src/PadScope.Desktop/MainWindow.xaml.cs | Desktop code-behind |
| src/PadScope.Hid/Ds4ControllerSession.cs | HID session |
| src/PadScope.Hid/Audio/AudioStreamBridge.cs | WASAPI audio bridge |
| src/PadScope.Hid/Virtual/Ds4PassThrough.cs | Virtual controller |
| src/PadScope.Core/Input/MacroProcessor.cs | Macro engine |
| src/PadScope.Core/Input/ProfileStore.cs | Profile JSON save/load |
| src/PadScope.Core/Diagnostics/AudioProbe.cs | Audio WMI detection |

## NuGet Packages

- HidSharp 2.1.0 - HID communication
- NAudio 2.2.1 - WASAPI audio
- Nefarius.ViGEm.Client 1.21.256 - Virtual controllers
- System.Management - WMI queries

## Commit Style

Conventional commits, lowercase: fix:, feat:, chore:
```
