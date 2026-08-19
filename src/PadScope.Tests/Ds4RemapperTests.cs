using PadScope.Core.Input;
using Xunit;

namespace PadScope.Tests;

public class Ds4RemapperTests
{
    private static Ds4InputState BuildState(
        Ds4Buttons buttons = Ds4Buttons.None,
        byte lx = 128,
        byte ly = 128,
        byte rx = 128,
        byte ry = 128,
        byte lt = 0,
        byte rt = 0)
    {
        return new Ds4InputState
        {
            Raw = new byte[64],
            ReportId = Ds4ReportParser.UsbReportId,
            Buttons = buttons,
            LeftStickX = lx,
            LeftStickY = ly,
            RightStickX = rx,
            RightStickY = ry,
            LeftTrigger = lt,
            RightTrigger = rt
        };
    }

    [Fact]
    public void NoSettings_ReturnsUnchangedState()
    {
        var profile = new ControllerProfile { Name = "Plain", Version = "1.0" };
        Ds4InputState input = BuildState(Ds4Buttons.Cross, lx: 10, lt: 200);

        Ds4InputState output = Ds4Remapper.Apply(profile, input);

        Assert.Equal(input, output);
    }

    [Fact]
    public void ButtonRemap_RedirectsPressedButtons()
    {
        var profile = new ControllerProfile
        {
            Name = "SwapCrossCircle",
            Version = "1.0",
            ButtonRemap = new Dictionary<Ds4Buttons, Ds4Buttons>
            {
                [Ds4Buttons.Cross] = Ds4Buttons.Circle,
                [Ds4Buttons.Circle] = Ds4Buttons.Cross
            }
        };

        Ds4InputState output = Ds4Remapper.Apply(
            profile,
            BuildState(Ds4Buttons.Cross | Ds4Buttons.Triangle));

        Assert.True(output.Buttons.HasFlag(Ds4Buttons.Circle));
        Assert.False(output.Buttons.HasFlag(Ds4Buttons.Cross));
        Assert.True(output.Buttons.HasFlag(Ds4Buttons.Triangle));
    }

    [Fact]
    public void StickSwap_ExchangesSticks()
    {
        var profile = new ControllerProfile
        {
            Name = "SwapSticks",
            Version = "1.0",
            LeftStick = new StickSettings { SwapSticks = true }
        };

        Ds4InputState output = Ds4Remapper.Apply(profile, BuildState(lx: 10, rx: 200));

        Assert.Equal((byte)200, output.LeftStickX);
        Assert.Equal((byte)10, output.RightStickX);
    }

    [Fact]
    public void StickInvert_FlipsAxes()
    {
        var profile = new ControllerProfile
        {
            Name = "Invert",
            Version = "1.0",
            RightStick = new StickSettings { InvertX = true, InvertY = true }
        };

        Ds4InputState output = Ds4Remapper.Apply(profile, BuildState(rx: 200, ry: 30));

        Assert.Equal((byte)55, output.RightStickX);
        Assert.Equal((byte)225, output.RightStickY);
    }

    [Fact]
    public void StickDeadzone_CentersNeutralArea()
    {
        var profile = new ControllerProfile
        {
            Name = "Deadzone",
            Version = "1.0",
            LeftStick = new StickSettings { Deadzone = 20 }
        };

        Ds4InputState inside = Ds4Remapper.Apply(profile, BuildState(lx: 130, ly: 126));
        Assert.Equal((byte)128, inside.LeftStickX);
        Assert.Equal((byte)128, inside.LeftStickY);

        Ds4InputState outside = Ds4Remapper.Apply(profile, BuildState(lx: 160));
        Assert.Equal((byte)160, outside.LeftStickX);
    }

    [Fact]
    public void TriggerInvert_FlipsValue()
    {
        var profile = new ControllerProfile
        {
            Name = "InvertTriggers",
            Version = "1.0",
            RightTrigger = new TriggerSettings { Invert = true }
        };

        Ds4InputState output = Ds4Remapper.Apply(profile, BuildState(rt: 200));

        Assert.Equal((byte)55, output.RightTrigger);
    }

    [Fact]
    public void TriggerDeadzone_ZeroesLowInput()
    {
        var profile = new ControllerProfile
        {
            Name = "TriggerDeadzone",
            Version = "1.0",
            LeftTrigger = new TriggerSettings { Deadzone = 10 }
        };

        Ds4InputState output = Ds4Remapper.Apply(profile, BuildState(lt: 8));

        Assert.Equal((byte)0, output.LeftTrigger);
    }
}