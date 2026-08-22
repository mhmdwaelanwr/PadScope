# PadScope

[![.NET](https://github.com/mhmdwaelanwr/PadScope/actions/workflows/dotnet.yml/badge.svg)](https://github.com/mhmdwaelanwr/PadScope/actions/workflows/dotnet.yml)
[![Package Windows](https://github.com/mhmdwaelanwr/PadScope/actions/workflows/package-windows.yml/badge.svg)](https://github.com/mhmdwaelanwr/PadScope/actions/workflows/package-windows.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)

**A Windows gamepad diagnostics, compatibility, remapping, and experimentation toolkit.**

PadScope helps you discover what a controller actually supports on PC—especially DualShock 4-compatible controllers and low-cost PS4-style clones. It combines a WPF desktop app and a command-line interface for scanning devices, inspecting live HID input, testing selected features, creating virtual controllers, applying profiles, and exporting evidence-based compatibility reports.

> PadScope is under active development. Hardware support varies, particularly with clone controllers. Unknown capabilities remain reported as unknown instead of being presented as working.

## Highlights

- Read-only Windows controller discovery with USB/Bluetooth hints
- Device identity, VID/PID, manufacturer, path, and known-profile matching
- JSON and Markdown compatibility report export
- Live buttons, sticks, triggers, battery, gyro, and touchpad monitoring
- Controlled rumble and lightbar tests with explicit confirmation
- Virtual DualShock 4 or Xbox 360 passthrough through ViGEmBus
- JSON-based remapping profiles, combos, rapid fire, and macros
- Touchpad and gyro mouse emulation with adjustable sensitivity
- Controller audio endpoint discovery through WMI and WASAPI
- Desktop and CLI interfaces
- Dark and light desktop themes
- Automated Windows build, test, and packaging workflows

## Current status

| Area | Status | Notes |
| --- | --- | --- |
| Device scanning | Implemented | Read-only Windows scan and profile matching |
| Report export | Implemented | JSON and Markdown |
| Live HID input | Implemented | Intended for DS4-style reports |
| Rumble/lightbar | Implemented, controlled | Sends output reports only after confirmation |
| Virtual controller | Implemented | Requires ViGEmBus |
| Remapping/macros | Implemented | Applied through JSON profiles |
| Touchpad/gyro mouse | Implemented | Requires compatible input reports |
| Audio endpoint lab | Experimental | Enumerates and routes Windows audio endpoints; it cannot add hardware support |
| Clone compatibility | Device-dependent | Must be verified per controller and connection mode |

## Requirements

- Windows 10 or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) for building from source
- A USB or Bluetooth gamepad
- [ViGEmBus](https://github.com/nefarius/ViGEmBus) only for virtual DS4/Xbox 360 output
- HidHide is optional and useful when preventing games from seeing both the physical and virtual controller

The packaged desktop build is framework-dependent, so the .NET 8 Desktop Runtime must be installed on the target PC.

## Build and run

From PowerShell:

```powershell
git clone https://github.com/mhmdwaelanwr/PadScope.git
cd PadScope
dotnet restore src\PadScope.sln
dotnet build src\PadScope.sln
```

Start the desktop app:

```powershell
dotnet run --project src\PadScope.Desktop
```

Show CLI help or run a safe scan:

```powershell
dotnet run --project src\PadScope.Cli -- help
dotnet run --project src\PadScope.Cli -- scan
dotnet run --project src\PadScope.Cli -- scan --json
```

Run the test suite:

```powershell
dotnet test src\PadScope.sln
```

## Desktop workflow

1. Connect the controller—USB is recommended for the first scan.
2. Start PadScope Desktop and select **Scan**.
3. Review the detected device, identity, profile confidence, risk level, and recommended next action.
4. Use Live Input to confirm report parsing.
5. Export the result as JSON or Markdown.
6. Only then try controlled output, virtual-controller, mouse, or audio features as appropriate.
7. Repeat the scan over Bluetooth and compare the results.

The desktop app includes dedicated areas for scanning and reports, staged tests, compatibility profiles, live input/output, virtual controllers and profiles, mouse emulation, and the Audio Lab.

## CLI commands

All commands may be run with:

```powershell
dotnet run --project src\PadScope.Cli -- <command> [options]
```

| Command | Purpose |
| --- | --- |
| `scan [--json]` | Scan controller-like Windows devices |
| `input [--vid XXXX] [--pid XXXX]` | Stream live controller state |
| `rumble ...` | Send a confirmed, timed rumble test |
| `lightbar ...` | Send a confirmed lightbar color test |
| `virtual ... --target ds4\|xbox360` | Mirror the physical pad to a ViGEm virtual controller |
| `mouse [--touch] [--gyro] [--sensitivity N]` | Control the Windows mouse |
| `audio --probe\|--list` | Inspect controller audio endpoints |
| `profile-example [--path file.json]` | Create an example remapping/macro profile |
| `stages` | Display all development/test stages |
| `run-stage <0-17>` | Run or explain a specific stage |
| `run-safe` | Run implemented read-only and packaging checks |
| `package` | Print the Windows publish command |

Examples:

```powershell
# Inspect a specific controller
dotnet run --project src\PadScope.Cli -- input --vid 054C --pid 09CC

# Controlled output tests (PadScope asks for confirmation)
dotnet run --project src\PadScope.Cli -- rumble --small 255 --large 128 --seconds 1
dotnet run --project src\PadScope.Cli -- lightbar --color 00AEEF --seconds 2

# Virtual Xbox 360 controller with a profile
dotnet run --project src\PadScope.Cli -- virtual --target xbox360 --profile .\profile.json

# Touchpad and gyro mouse
dotnet run --project src\PadScope.Cli -- mouse --touch --gyro --sensitivity 1.2

# Audio discovery
dotnet run --project src\PadScope.Cli -- audio --probe
dotnet run --project src\PadScope.Cli -- audio --list
```

When multiple devices are present, use `--vid` and `--pid` to select one. If no match is found, the current CLI falls back to the first detected controller, so verify the selected device before controlled tests.

## Safety model

PadScope is diagnostics-first:

- **Safe:** enumeration, profile matching, audio endpoint discovery, and report export
- **Controlled:** rumble and lightbar output to a selected controller, with a warning and confirmation
- **Experimental:** protocol research and continuous audio/output experiments

A normal scan never sends HID output reports. Controller names and profiles are hints, not proof of full compatibility. See [the safety policy](docs/safety-policy.md) before experimenting with unknown hardware.

## Compatibility profiles

Starter profiles are stored in [`data/controllers`](data/controllers) for:

- Sony DualShock 4
- Sony DualSense
- Marvo GT-84
- SkyTech DS4-style clone
- Zero DS4-style clone
- Generic Wireless Controller
- AULA G1000

Profiles are evidence records. USB and Bluetooth modes may expose different identities or capabilities, and a clone may implement only part of the DS4 protocol.

## Project structure

```text
src/
├── PadScope.Core       Scanning models, diagnostics, profiles, mapping, tests, and reports
├── PadScope.Hid        HID I/O, virtual controllers, mouse bridge, and audio integration
├── PadScope.Desktop    Windows WPF application
├── PadScope.Cli        Command-line interface
└── PadScope.Tests      Unit tests
data/controllers/       Compatibility profiles
docs/                   Architecture, research, safety, and test documentation
```

## Packaging

Create a framework-dependent Windows x64 build:

```powershell
dotnet publish src\PadScope.Desktop\PadScope.Desktop.csproj -c Release -r win-x64 --self-contained false -o artifacts\PadScope-win-x64
```

The **Package Windows** GitHub Actions workflow also produces a `PadScope-win-x64.zip` artifact when run manually or when a `v*` tag is pushed.

## Known limitations

- PadScope currently targets Windows and `net8.0-windows`.
- DS4-style report parsing does not guarantee correct behavior for every clone.
- Virtual-controller output requires the separately installed ViGEmBus driver.
- A virtual controller can cause double input unless the physical controller is hidden or the game is configured correctly.
- Audio endpoint detection or routing does not mean that a controller implements Sony's DS4 HID audio protocol.
- PadScope cannot restore a feature missing from the controller's firmware or hardware.

## Documentation

- [Getting started](docs/getting-started.md)
- [Architecture](docs/architecture.md)
- [A-to-Z feature map](docs/a-to-z-feature-map.md)
- [Staged test plan](docs/staged-test-plan.md)
- [Safety policy](docs/safety-policy.md)
- [Compatibility profiles](docs/compatibility-profiles.md)
- [DS4 audio protocol notes](docs/ds4-audio-protocol.md)
- [Research notes](docs/research-notes.md)

## Contributing

Compatibility evidence is especially valuable. When reporting a controller, include:

- Exact model and connection mode
- VID/PID and the displayed Windows device name
- Whether input, rumble, lightbar, gyro, touchpad, and audio endpoints were actually observed
- A PadScope JSON or Markdown report when possible

Use the repository's compatibility-report or feature-test issue templates so observed results remain separate from assumptions.

## License

PadScope is available under the [MIT License](LICENSE).
