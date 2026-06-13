# Stage 11 Packaging

Stage 11 prepares a portable Windows build for testers.

Goals:

- Build PadScope.Desktop in Release mode.
- Publish a win-x64 package.
- Create a ZIP artifact.
- Keep local logs and reports out of the package.
- Confirm the app starts successfully before release.

Recommended command:

```powershell
dotnet publish src\PadScope.Desktop\PadScope.Desktop.csproj -c Release -r win-x64 --self-contained false -o artifacts\PadScope-win-x64
```
