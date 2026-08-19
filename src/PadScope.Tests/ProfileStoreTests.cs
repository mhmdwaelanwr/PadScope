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

        Assert.Throws<IOException>(() => ProfileStore.Load(path));
    }
}