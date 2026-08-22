# Contributing to PadScope

PadScope values measured hardware evidence over assumptions. Contributions may improve code, documentation, tests, controller profiles, or compatibility reports.

## Development setup

1. Install Windows 10/11 and the .NET 8 SDK.
2. Fork and clone the repository.
3. Build and test with:

   ```powershell
   dotnet restore src\PadScope.sln
   dotnet build src\PadScope.sln -c Release
   dotnet test src\PadScope.sln -c Release --no-build
   ```

4. Keep normal scans read-only and add tests for behavioral changes.

## Pull requests

- Keep each pull request focused.
- Explain the user-visible behavior and safety impact.
- Never claim hardware support without observed evidence.
- Include the controller model, connection mode, VID/PID, and sanitized output for compatibility changes.
- Do not commit build output, local reports, device paths, or personal data.

## Controller profiles

Unknown values must stay unknown. A product name alone does not prove protocol compatibility. USB and Bluetooth results should be recorded separately when they differ.
