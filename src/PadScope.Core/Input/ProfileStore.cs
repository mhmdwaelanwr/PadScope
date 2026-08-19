using System.Text.Json;
using System.Text.Json.Serialization;

namespace PadScope.Core.Input;

public static class ProfileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ProfilesDirectory
    {
        get
        {
            string baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directory = Path.Combine(baseDirectory, "PadScope", "profiles");
            Directory.CreateDirectory(directory);
            return directory;
        }
    }

    public static ControllerProfile Load(string path)
    {
        string json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<ControllerProfile>(json, Options)
            ?? throw new InvalidDataException($"Profile '{path}' is empty or invalid.");
    }

    public static void Save(ControllerProfile profile, string path)
    {
        string json = JsonSerializer.Serialize(profile, Options);
        File.WriteAllText(path, json);
    }

    public static ControllerProfile CreateDefault()
    {
        return new ControllerProfile
        {
            Name = "Default",
            Version = "1.0",
            LeftStick = new StickSettings { Deadzone = 8 },
            RightStick = new StickSettings { Deadzone = 8 }
        };
    }

    public static string SaveDefaultProfile()
    {
        string path = Path.Combine(ProfilesDirectory, "default.json");
        Save(CreateDefault(), path);
        return path;
    }
}