using System.Text.Json;
using PadScope.Core.Diagnostics;
using PadScope.Core.Scanning;
using PadScope.Core.Testing;

IControllerScanner scanner = new WindowsDeviceScanner();

string command = args.Length > 0 ? args[0].ToLowerInvariant() : "scan";

switch (command)
{
    case "scan":
        RunScan(scanner, args);
        break;

    case "stages":
        PrintStages();
        break;

    case "run-stage":
        RunStage(scanner, args);
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

static void RunStage(IControllerScanner scanner, string[] args)
{
    if (args.Length < 2 || !int.TryParse(args[1], out int stageNumber) || !Enum.IsDefined(typeof(TestStage), stageNumber))
    {
        Console.Error.WriteLine("Usage: PadScope.Cli run-stage <0-11>");
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

    Console.WriteLine("Locked: this stage needs verified device evidence before it can run.");
    Console.WriteLine(definition.WhatToDo);
    Environment.ExitCode = 2;
}

static void PrintReports(IReadOnlyList<PadScope.Core.Models.CompatibilityReport> reports)
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
    Console.WriteLine("  scan              Run the read-only Windows scanner");
    Console.WriteLine("  scan --json       Run the scanner and print JSON");
    Console.WriteLine("  stages            Print implemented and locked stage status");
    Console.WriteLine("  run-stage <0-11>  Run a safe implemented stage, or explain a locked stage");
    Console.WriteLine("  package           Print Windows package instructions");
    Console.WriteLine("  help              Show help");
}
