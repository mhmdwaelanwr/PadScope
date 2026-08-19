using System.Text;
using System.Text.Json;
using PadScope.Core.Diagnostics;
using PadScope.Core.Input;
using PadScope.Core.Models;
using PadScope.Core.Scanning;
using PadScope.Core.Testing;
using PadScope.Hid;
using PadScope.Hid.Mouse;
using PadScope.Hid.Virtual;

IControllerScanner scanner = new WindowsDeviceScanner();

string command = args.Length > 0 ? args[0].ToLowerInvariant() : "scan";

switch (command)
{
    case "scan":
        RunScan(scanner, args);
        break;

    case "input":
        RunInput(scanner, args);
        break;

    case "rumble":
        RunRumble(scanner, args);
        break;

    case "lightbar":
        RunLightbar(scanner, args);
        break;

    case "virtual":
        RunVirtual(scanner, args);
        break;

    case "mouse":
        RunMouse(scanner, args);
        break;

    case "stages":
        PrintStages();
        break;

    case "run-stage":
        RunStage(scanner, args);
        break;

    case "run-safe":
        RunSafeStageSuite(scanner);
        break;

    case "package":
        PrintPackageInstructions();
        break;

    case "help":
    case "--help":
    case "-h":
        PrintHelp();
        break;

    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        Environment.ExitCode = 1;
        break;
}

static void RunScan(IControllerScanner scanner, string[] args)
{
    bool json = args.Any(arg => arg.Equals("--json", StringComparison.OrdinalIgnoreCase));

    var reports = scanner.Scan().Select(ReportBuilder.BuildInitialReport).ToList();

    if (json)
    {
        Console.WriteLine(JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
        return;
    }

    PrintReports(reports);
}

static void RunInput(IControllerScanner scanner, string[] args)
{
    if (!TrySelectDevice(scanner, args, out ControllerDevice? device, out string? error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    using Ds4ControllerSession session = new(new HidSharpHidInputReader(), device);

    if (!session.TryStart(out error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"Live input from: {device.DisplayName} (VID {device.VendorId ?? "?"}/PID {device.ProductId ?? "?"})");
    Console.WriteLine("Move sticks, press buttons, or use the touchpad. Press Ctrl+C to stop.");
    Console.WriteLine();

    string lastLine = string.Empty;
    session.StateUpdated += state =>
    {
        string line = FormatInputState(state);
        if (line == lastLine)
        {
            return;
        }

        lastLine = line;
        Console.WriteLine(line);
    };
    session.Error += message => Console.Error.WriteLine(message);

    WaitForCtrlC();
}

static void RunRumble(IControllerScanner scanner, string[] args)
{
    if (!TrySelectDevice(scanner, args, out ControllerDevice? device, out string? error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    if (!Confirm("This sends a rumble output report to the selected controller. Continue?"))
    {
        Console.WriteLine("Aborted.");
        return;
    }

    using Ds4ControllerSession session = new(new HidSharpHidInputReader(), device);

    if (!session.TryStart(out error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    byte small = ParseByteArg(args, "--small", 255);
    byte large = ParseByteArg(args, "--large", 0);
    double seconds = ParseDoubleArg(args, "--seconds", 1.0);

    if (!session.TrySendRumble(small, large, out error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"Sent rumble small={small} large={large} for {seconds:F1}s.");
    Console.WriteLine("Did the controller vibrate? If not, the clone may not implement the DS4 output report.");
    Thread.Sleep(TimeSpan.FromSeconds(seconds));

    session.TryResetOutput(out _);
    Console.WriteLine("Output reset to neutral.");
}

static void RunLightbar(IControllerScanner scanner, string[] args)
{
    if (!TrySelectDevice(scanner, args, out ControllerDevice? device, out string? error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    if (!Confirm("This sends a lightbar output report to the selected controller. Continue?"))
    {
        Console.WriteLine("Aborted.");
        return;
    }

    using Ds4ControllerSession session = new(new HidSharpHidInputReader(), device);

    if (!session.TryStart(out error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    string colorHex = GetArgValue(args, "--color") ?? "00FF00";
    if (!TryParseRgb(colorHex, out byte red, out byte green, out byte blue))
    {
        Console.Error.WriteLine($"Invalid color '{colorHex}'. Use RRGGBB, for example FF0000.");
        Environment.ExitCode = 1;
        return;
    }

    double? seconds = args.Any(arg => arg.Equals("--seconds", StringComparison.OrdinalIgnoreCase))
        ? ParseDoubleArg(args, "--seconds", 1.0)
        : null;

    if (!session.TrySendLightbar(red, green, blue, out error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"Sent lightbar #{colorHex}.");
    Console.WriteLine("Did the lightbar change color? If not, the clone may not implement the DS4 output report.");

    if (seconds is double hold)
    {
        Thread.Sleep(TimeSpan.FromSeconds(hold));
        session.TryResetOutput(out _);
        Console.WriteLine("Lightbar reset to neutral.");
    }
    else
    {
        Console.WriteLine("Press Ctrl+C to reset the lightbar.");
        WaitForCtrlC();
        session.TryResetOutput(out _);
        Console.WriteLine("Lightbar reset to neutral.");
    }
}

static void RunVirtual(IControllerScanner scanner, string[] args)
{
    if (!TrySelectDevice(scanner, args, out ControllerDevice? device, out string? error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    string targetName = GetArgValue(args, "--target") ?? "ds4";
    IVirtualControllerTarget target = targetName.Equals("xbox360", StringComparison.OrdinalIgnoreCase)
        ? new ViGEmXbox360Target()
        : new ViGEmDualShock4Target();

    Console.WriteLine($"Virtual target: {target.GetType().Name}");
    Console.WriteLine(ViGEmBusDetector.DescribeStatus());
    Console.WriteLine(HidHideDetector.DescribeStatus());

    ControllerProfile? profile = null;
    string? profilePath = GetArgValue(args, "--profile");
    if (profilePath is not null)
    {
        try
        {
            profile = ProfileStore.Load(profilePath);
            Console.WriteLine($"Applied profile: {profile.Name} v{profile.Version} ({profilePath})");
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or ArgumentException)
        {
            Console.Error.WriteLine($"Could not load profile '{profilePath}': {ex.Message}");
            Environment.ExitCode = 1;
            return;
        }
    }

    using Ds4ControllerSession physical = new(new HidSharpHidInputReader(), device);
    using Ds4PassThrough bridge = new(physical, target, profile);

    if (!bridge.TryStart(out error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"Passthrough live: {device.DisplayName} -> {target.GetType().Name}");
    Console.WriteLine("Games can now use the virtual pad. Press Ctrl+C to stop.");
    Console.WriteLine();

    target.FeedbackReceived += feedback =>
    {
        string line = $"[GAME] rumble small={feedback.SmallMotor} large={feedback.LargeMotor}";

        if (feedback.LedNumber > 0)
        {
            line += $" led={feedback.LedNumber}";
        }

        if (feedback.Red != 0 || feedback.Green != 0 || feedback.Blue != 0)
        {
            line += $" lightbar=#{feedback.Red:X2}{feedback.Green:X2}{feedback.Blue:X2}";
        }

        Console.WriteLine(line);
    };

    WaitForCtrlC();
}

static void RunMouse(IControllerScanner scanner, string[] args)
{
    if (!TrySelectDevice(scanner, args, out ControllerDevice? device, out string? error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    bool touch = args.Any(arg => arg.Equals("--touch", StringComparison.OrdinalIgnoreCase));
    bool gyro = args.Any(arg => arg.Equals("--gyro", StringComparison.OrdinalIgnoreCase));

    if (!touch && !gyro)
    {
        touch = true;
        gyro = true;
    }

    double sensitivity = ParseDoubleArg(args, "--sensitivity", 1.0);

    using Ds4ControllerSession session = new(new HidSharpHidInputReader(), device);
    using MouseEmulationBridge bridge = new(
        session,
        new WindowsMouseSink(),
        touch ? new TouchpadMouseSettings { Sensitivity = sensitivity } : null,
        gyro ? new GyroMouseSettings { Sensitivity = sensitivity } : null);

    if (!bridge.TryStart(out error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"Mouse emulation live: touchpad={touch} gyro={gyro} sensitivity={sensitivity:F2}");
    Console.WriteLine("One finger moves the mouse (tap clicks, drag holds). Two fingers hold the right button.");
    Console.WriteLine("Pressing the touchpad clicks and holds the left button. Tilt the controller for gyro mouse.");
    Console.WriteLine("Press Ctrl+C to stop.");
    WaitForCtrlC();
}

static bool TrySelectDevice(
    IControllerScanner scanner,
    string[] args,
    out ControllerDevice? device,
    out string? error)
{
    device = null;

    var reports = scanner.Scan().Select(ReportBuilder.BuildInitialReport).ToList();

    if (reports.Count == 0)
    {
        error = "No controller-like device was detected. Connect the controller first, then try again.";
        return false;
    }

    string? vid = GetArgValue(args, "--vid");
    string? pid = GetArgValue(args, "--pid");

    IEnumerable<CompatibilityReport> candidates = reports;

    if (vid is not null)
    {
        candidates = candidates.Where(report => string.Equals(report.Device.VendorId, vid, StringComparison.OrdinalIgnoreCase));
    }

    if (pid is not null)
    {
        candidates = candidates.Where(report => string.Equals(report.Device.ProductId, pid, StringComparison.OrdinalIgnoreCase));
    }

    List<CompatibilityReport> filtered = candidates.ToList();
    if (filtered.Count == 0)
    {
        filtered = reports.ToList();
    }

    if (filtered.Count == 0)
    {
        error = "No matching device was found.";
        return false;
    }

    device = filtered[0].Device;
    error = null;
    return true;
}

static string FormatInputState(PadScope.Core.Input.Ds4InputState state)
{
    StringBuilder builder = new();

    builder.Append($"LX {state.LeftStickX,3} LY {state.LeftStickY,3} ");
    builder.Append($"RX {state.RightStickX,3} RY {state.RightStickY,3} ");
    builder.Append($"L2 {state.LeftTrigger,3} R2 {state.RightTrigger,3} ");
    builder.Append($"GYR {state.GyroX,5} {state.GyroY,5} {state.GyroZ,5} ");
    builder.Append($"BAT {(state.BatteryLevel.HasValue ? state.BatteryLevel.Value.ToString() : "?"),2} ");

    if (state.Touch1?.Touching == true)
    {
        builder.Append($"T1 {state.Touch1.Value.X,4}x{state.Touch1.Value.Y,4} ");
    }

    builder.Append(state.Buttons.ToString());

    return builder.ToString();
}

static void RunSafeStageSuite(IControllerScanner scanner)
{
    Console.WriteLine("PadScope safe stage suite");
    Console.WriteLine("=========================");
    Console.WriteLine("This suite runs only implemented read-only or packaging stages.");
    Console.WriteLine();

    foreach (int stageNumber in new[] { 0, 1, 2, 3, 4, 9, 11 })
    {
        RunStage(scanner, new[] { "run-stage", stageNumber.ToString() });
        Console.WriteLine("-------------------------");
    }

    Console.WriteLine("Locked stages intentionally skipped: 5, 6, 7, 8, 10.");
}

static void RunStage(IControllerScanner scanner, string[] args)
{
    if (args.Length < 2 || !int.TryParse(args[1], out int stageNumber) || !Enum.IsDefined(typeof(TestStage), stageNumber))
    {
        Console.Error.WriteLine("Usage: PadScope.Cli run-stage <0-16>");
        Environment.ExitCode = 1;
        return;
    }

    TestStage stage = (TestStage)stageNumber;
    TestStageDefinition definition = TestStageRegistry.All.First(item => item.Stage == stage);

    Console.WriteLine($"Stage {stageNumber}: {definition.Name}");
    Console.WriteLine($"Status: {definition.Status}");
    Console.WriteLine($"Goal: {definition.Goal}");
    Console.WriteLine();

    if (stage is TestStage.BuildVerification)
    {
        Console.WriteLine("Implemented: build this solution with dotnet build src\\PadScope.sln, then launch PadScope.Desktop.");
        return;
    }

    if (stage is TestStage.EmptyScan or TestStage.UsbScan or TestStage.BluetoothScan or TestStage.ProfileValidation or TestStage.AudioEndpoint)
    {
        Console.WriteLine("Implemented: running read-only scan and report builder.");
        Console.WriteLine();
        PrintReports(scanner.Scan().Select(ReportBuilder.BuildInitialReport).ToList());
        return;
    }

    if (stage is TestStage.Packaging)
    {
        PrintPackageInstructions();
        return;
    }

    if (stage is TestStage.VirtualController)
    {
        Console.WriteLine("Implemented: requires the ViGEmBus driver and a connected controller.");
        Console.WriteLine("Run: PadScope.Cli virtual [--vid XXXX] [--pid XXXX] [--target ds4|xbox360] [--profile path.json]");
        return;
    }

    if (stage is TestStage.Remapping)
    {
        Console.WriteLine("Implemented: requires the ViGEmBus driver, a connected controller, and a JSON profile.");
        Console.WriteLine("Run: PadScope.Cli virtual [--vid XXXX] [--pid XXXX] [--target ds4|xbox360] --profile path.json");
        return;
    }

    if (stage is TestStage.HidHideIntegration)
    {
        Console.WriteLine("Implemented: HidHide driver status is reported.");
        Console.WriteLine(HidHideDetector.DescribeStatus());
        return;
    }

    if (stage is TestStage.TouchpadMouse or TestStage.GyroMouse)
    {
        Console.WriteLine("Implemented: requires a connected DS4-like controller.");
        Console.WriteLine("Run: PadScope.Cli mouse [--vid XXXX] [--pid XXXX] [--touch] [--gyro] [--sensitivity 1]");
        return;
    }

    Console.WriteLine("Locked: this stage needs verified device evidence before it can run.");
    Console.WriteLine(definition.WhatToDo);
    Environment.ExitCode = 2;
}

static void PrintReports(IReadOnlyList<CompatibilityReport> reports)
{
    Console.WriteLine("PadScope scan");
    Console.WriteLine("=============");

    if (reports.Count == 0)
    {
        Console.WriteLine("No controller-like devices were detected by the read-only Windows scanner.");
        Console.WriteLine("Try connecting the controller by USB first, then run the scan again.");
        return;
    }

    foreach (var report in reports)
    {
        Console.WriteLine($"Device: {report.Device.DisplayName}");
        Console.WriteLine($"Manufacturer: {report.Device.Manufacturer ?? "Unknown"}");
        Console.WriteLine($"VID/PID: {report.Device.VendorId ?? "?"}/{report.Device.ProductId ?? "?"}");
        Console.WriteLine($"Connection: {report.Device.ConnectionType}");
        Console.WriteLine($"Source: {report.Device.Source}");
        Console.WriteLine($"Profile: {report.ProfileName}");
        Console.WriteLine($"Confidence: {report.ProfileConfidence}");
        Console.WriteLine($"Risk: {report.RecommendedRiskLevel}");
        Console.WriteLine($"Input: {report.Input}");
        Console.WriteLine($"Windows audio endpoint: {report.WindowsAudioEndpoint}");
        Console.WriteLine($"DS4 audio protocol: {report.Ds4AudioProtocol}");
        Console.WriteLine("Notes:");

        foreach (string note in report.Notes)
        {
            Console.WriteLine($"- {note}");
        }

        Console.WriteLine();
    }
}

static void PrintStages()
{
    Console.WriteLine("PadScope stages");
    Console.WriteLine("===============");

    foreach (var stage in TestStageRegistry.All)
    {
        Console.WriteLine($"{(int)stage.Stage}: {stage.Name}");
        Console.WriteLine($"Status: {stage.Status}");
        Console.WriteLine($"Goal: {stage.Goal}");
        Console.WriteLine($"Next: {stage.WhatToDo}");
        Console.WriteLine($"Pass: {stage.PassCriteria}");
        Console.WriteLine();
    }
}

static void PrintPackageInstructions()
{
    Console.WriteLine("PadScope Windows package");
    Console.WriteLine("========================");
    Console.WriteLine("Run from the repository root:");
    Console.WriteLine("dotnet publish src\\PadScope.Desktop\\PadScope.Desktop.csproj -c Release -r win-x64 --self-contained false -o artifacts\\PadScope-win-x64");
    Console.WriteLine();
    Console.WriteLine("Or use GitHub Actions: Package Windows");
}

static void PrintHelp()
{
    Console.WriteLine("PadScope");
    Console.WriteLine("Gamepad diagnostics and compatibility toolkit for Windows.");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  scan                       Run the read-only Windows scanner");
    Console.WriteLine("  scan --json                Run the scanner and print JSON");
    Console.WriteLine("  input [--vid XXXX] [--pid XXXX]   Live-read the controller state");
    Console.WriteLine("  rumble [--vid XXXX] [--pid XXXX] [--small 255] [--large 0] [--seconds 1]");
    Console.WriteLine("  lightbar [--vid XXXX] [--pid XXXX] [--color RRGGBB] [--seconds 1]");
    Console.WriteLine("  virtual [--vid XXXX] [--pid XXXX] [--target ds4|xbox360] [--profile path.json]   Mirror the pad as a virtual controller");
    Console.WriteLine("  mouse [--vid XXXX] [--pid XXXX] [--touch] [--gyro] [--sensitivity 1]   Drive the Windows mouse from touchpad and gyro");
    Console.WriteLine("  stages                     Print implemented and locked stage status");
    Console.WriteLine("  run-stage <0-16>           Run a safe implemented stage, or explain a locked stage");
    Console.WriteLine("  run-safe                   Run all safe implemented stage checks");
    Console.WriteLine("  package                    Print Windows package instructions");
    Console.WriteLine("  help                       Show help");
}

static void WaitForCtrlC()
{
    using ManualResetEventSlim gate = new();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        gate.Set();
    };
    gate.Wait();
}

static bool Confirm(string message)
{
    Console.WriteLine(message);
    Console.Write("Type 'yes' to continue: ");
    return string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase);
}

static string? GetArgValue(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static byte ParseByteArg(string[] args, string name, byte fallback)
{
    string? value = GetArgValue(args, name);
    return byte.TryParse(value, out byte parsed) ? parsed : fallback;
}

static double ParseDoubleArg(string[] args, string name, double fallback)
{
    string? value = GetArgValue(args, name);
    return double.TryParse(value, out double parsed) ? parsed : fallback;
}

static bool TryParseRgb(string hex, out byte red, out byte green, out byte blue)
{
    red = 0;
    green = 0;
    blue = 0;

    if (hex.Length != 6 ||
        !byte.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out red) ||
        !byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out green) ||
        !byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out blue))
    {
        return false;
    }

    return true;
}