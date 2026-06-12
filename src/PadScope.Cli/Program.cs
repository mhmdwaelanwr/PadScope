using System.Text.Json;
using PadScope.Core.Diagnostics;
using PadScope.Core.Scanning;

IControllerScanner scanner = new StubControllerScanner();

string command = args.Length > 0 ? args[0].ToLowerInvariant() : "scan";

switch (command)
{
    case "scan":
        RunScan(scanner, args);
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

    var reports = scanner
        .Scan()
        .Select(ReportBuilder.BuildInitialReport)
        .ToList();

    if (json)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        Console.WriteLine(JsonSerializer.Serialize(reports, options));
        return;
    }

    Console.WriteLine("PadScope scan");
    Console.WriteLine("=============");

    foreach (var report in reports)
    {
        Console.WriteLine($"Device: {report.Device.DisplayName}");
        Console.WriteLine($"Manufacturer: {report.Device.Manufacturer ?? "Unknown"}");
        Console.WriteLine($"VID/PID: {report.Device.VendorId ?? "?"}/{report.Device.ProductId ?? "?"}");
        Console.WriteLine($"Connection: {report.Device.ConnectionType}");
        Console.WriteLine($"Source: {report.Device.Source}");
        Console.WriteLine($"Input: {report.Input}");
        Console.WriteLine($"Rumble: {report.Rumble}");
        Console.WriteLine($"Lightbar: {report.Lightbar}");
        Console.WriteLine($"Gyro: {report.Gyro}");
        Console.WriteLine($"Touchpad: {report.Touchpad}");
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

static void PrintHelp()
{
    Console.WriteLine("PadScope");
    Console.WriteLine("Gamepad diagnostics and compatibility toolkit for Windows.");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  scan          Run the scanner");
    Console.WriteLine("  scan --json   Run the scanner and print JSON");
    Console.WriteLine("  help          Show help");
}
