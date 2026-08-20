using PadScope.Core.Input;
using Xunit;

namespace PadScope.Tests;

public class ProfileStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsProfile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"padscope-profile-{Guid.NewGuid():N}.json");

        try
        {
            var profile = new ControllerProfile
            {
                Name = "RoundTrip",
                Version = "2.0",
                ButtonRemap = new Dictionary<Ds4Buttons, Ds4Buttons>
                {
                    [Ds4Buttons.Cross] = Ds4Buttons.Circle
                },
                LeftStick = new StickSettings { Deadzone = 16, InvertY = true },
                ApplyLightbar = false
            };

            ProfileStore.Save(profile, path);
            ControllerProfile loaded = ProfileStore.Load(path);

            Assert.Equal("RoundTrip", loaded.Name);
            Assert.Equal(Ds4Buttons.Circle, loaded.ButtonRemap[Ds4Buttons.Cross]);
            Assert.NotNull(loaded.LeftStick);
            Assert.True(loaded.LeftStick.InvertY);
            Assert.Equal((byte)16, loaded.LeftStick.Deadzone);
            Assert.False(loaded.ApplyLightbar);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CreateDefault_HasDeadzones()
    {
        ControllerProfile profile = ProfileStore.CreateDefault();

        Assert.Equal("Default", profile.Name);
        Assert.NotNull(profile.LeftStick);
        Assert.True(profile.LeftStick.Deadzone > 0);
        Assert.NotNull(profile.RightStick);
        Assert.True(profile.ApplyRumble);
        Assert.True(profile.ApplyLightbar);
    }

    [Fact]
    public void Load_MissingFile_Throws()
    {
        string path = Path.Combine(Path.GetTempPath(), "padscope-missing-profile.json");

        Assert.ThrowsAny<IOException>(() => ProfileStore.Load(path));
    }

    [Fact]
    public void SaveAndLoad_RoundTripsMacros()
    {
        string path = Path.Combine(Path.GetTempPath(), $"padscope-macros-{Guid.NewGuid():N}.json");

        try
        {
            var profile = new ControllerProfile
            {
                Name = "Macros",
                Version = "1.0",
                Macros = new[]
                {
                    new MacroDefinition
                    {
                        Name = "Rapid",
                        Trigger = Ds4Buttons.L1 | Ds4Buttons.R1,
                        Output = Ds4Buttons.Cross,
                        ShotsPerSecond = 8
                    }
                },
                Sequences = new[]
                {
                    new MacroSequence
                    {
                        Name = "Demo",
                        Trigger = Ds4Buttons.L2,
                        Steps = new[]
                        {
                            new MacroSequenceStep { Buttons = Ds4Buttons.Square, DurationSeconds = 0.2 }
                        }
                    }
                }
            };

            ProfileStore.Save(profile, path);
            ControllerProfile loaded = ProfileStore.Load(path);

            Assert.Single(loaded.Macros);
            Assert.Equal(Ds4Buttons.L1 | Ds4Buttons.R1, loaded.Macros[0].Trigger);
            Assert.Equal(8, loaded.Macros[0].ShotsPerSecond);
            Assert.Single(loaded.Sequences);
            Assert.Single(loaded.Sequences[0].Steps);
            Assert.Equal(Ds4Buttons.Square, loaded.Sequences[0].Steps[0].Buttons);
        }
        finally
        {
            File.Delete(path);
        }
    }
}