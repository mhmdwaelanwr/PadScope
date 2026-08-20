using PadScope.Core.Input;
using Xunit;

namespace PadScope.Tests;

public class MacroProcessorTests
{
    private static MacroProcessor Empty()
    {
        return new MacroProcessor(Array.Empty<MacroDefinition>(), Array.Empty<MacroSequence>());
    }

    [Fact]
    public void NoMacros_Passthrough()
    {
        MacroProcessor processor = Empty();

        Ds4Buttons output = processor.Process(Ds4Buttons.Cross | Ds4Buttons.Triangle, TimeSpan.FromMilliseconds(10));

        Assert.Equal(Ds4Buttons.Cross | Ds4Buttons.Triangle, output);
    }

    [Fact]
    public void Combo_ReplacesTriggerButtons()
    {
        var processor = new MacroProcessor(
            new[]
            {
                new MacroDefinition
                {
                    Name = "Menu",
                    Trigger = Ds4Buttons.L1 | Ds4Buttons.R1,
                    Output = Ds4Buttons.Options
                }
            },
            Array.Empty<MacroSequence>());

        Ds4Buttons chord = processor.Process(Ds4Buttons.L1 | Ds4Buttons.R1, TimeSpan.Zero);
        Assert.Equal(Ds4Buttons.Options, chord);

        Ds4Buttons withExtra = processor.Process(
            Ds4Buttons.L1 | Ds4Buttons.R1 | Ds4Buttons.Triangle,
            TimeSpan.FromMilliseconds(10));
        Assert.True(withExtra.HasFlag(Ds4Buttons.Options));
        Assert.True(withExtra.HasFlag(Ds4Buttons.Triangle));
        Assert.False(withExtra.HasFlag(Ds4Buttons.L1));
        Assert.False(withExtra.HasFlag(Ds4Buttons.R1));
    }

    [Fact]
    public void RapidFire_AlternatesPulses()
    {
        var processor = new MacroProcessor(
            new[]
            {
                new MacroDefinition
                {
                    Name = "Rapid",
                    Trigger = Ds4Buttons.Cross,
                    Output = Ds4Buttons.Cross,
                    ShotsPerSecond = 2
                }
            },
            Array.Empty<MacroSequence>());

        Assert.Equal(Ds4Buttons.Cross, processor.Process(Ds4Buttons.Cross, TimeSpan.Zero));
        Assert.Equal(Ds4Buttons.None, processor.Process(Ds4Buttons.Cross, TimeSpan.FromMilliseconds(250)));
        Assert.Equal(Ds4Buttons.Cross, processor.Process(Ds4Buttons.Cross, TimeSpan.FromMilliseconds(250)));
    }

    [Fact]
    public void RapidFire_StopsWhenReleased()
    {
        var processor = new MacroProcessor(
            new[]
            {
                new MacroDefinition
                {
                    Name = "Rapid",
                    Trigger = Ds4Buttons.Cross,
                    Output = Ds4Buttons.Cross,
                    ShotsPerSecond = 2
                }
            },
            Array.Empty<MacroSequence>());

        Assert.Equal(Ds4Buttons.Cross, processor.Process(Ds4Buttons.Cross, TimeSpan.Zero));
        Assert.Equal(Ds4Buttons.None, processor.Process(Ds4Buttons.Cross, TimeSpan.FromMilliseconds(300)));
        Assert.Equal(Ds4Buttons.None, processor.Process(Ds4Buttons.None, TimeSpan.FromMilliseconds(100)));
    }

    [Fact]
    public void Toggle_LatchesAfterRelease()
    {
        var processor = new MacroProcessor(
            new[]
            {
                new MacroDefinition
                {
                    Name = "Toggle",
                    Trigger = Ds4Buttons.Triangle,
                    Output = Ds4Buttons.Cross,
                    IsToggle = true
                }
            },
            Array.Empty<MacroSequence>());

        Assert.Equal(Ds4Buttons.Cross, processor.Process(Ds4Buttons.Triangle, TimeSpan.Zero));
        Assert.Equal(Ds4Buttons.Cross, processor.Process(Ds4Buttons.Triangle, TimeSpan.FromMilliseconds(50)));
        Assert.Equal(Ds4Buttons.Cross, processor.Process(Ds4Buttons.None, TimeSpan.FromMilliseconds(50)));
        Assert.Equal(Ds4Buttons.Triangle, processor.Process(Ds4Buttons.Triangle, TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public void Sequence_RunsStepsInOrder()
    {
        var processor = new MacroProcessor(
            Array.Empty<MacroDefinition>(),
            new[]
            {
                new MacroSequence
                {
                    Name = "Demo",
                    Trigger = Ds4Buttons.L2,
                    Steps = new[]
                    {
                        new MacroSequenceStep { Buttons = Ds4Buttons.Square, DurationSeconds = 0.2 },
                        new MacroSequenceStep { Buttons = Ds4Buttons.Triangle, DurationSeconds = 0.2 }
                    }
                }
            });

        Assert.Equal(Ds4Buttons.Square, processor.Process(Ds4Buttons.L2, TimeSpan.Zero));
        Assert.Equal(Ds4Buttons.Triangle, processor.Process(Ds4Buttons.L2, TimeSpan.FromMilliseconds(200)));
        Assert.Equal(Ds4Buttons.L2, processor.Process(Ds4Buttons.L2, TimeSpan.FromMilliseconds(200)));
    }

    [Fact]
    public void MostSpecificCombo_Wins()
    {
        MacroDefinition[] definitions =
        {
            new()
            {
                Name = "Both",
                Trigger = Ds4Buttons.L1 | Ds4Buttons.R1,
                Output = Ds4Buttons.Options
            },
            new()
            {
                Name = "Left",
                Trigger = Ds4Buttons.L1,
                Output = Ds4Buttons.Cross
            }
        };

        var chordProcessor = new MacroProcessor(definitions, Array.Empty<MacroSequence>());
        Assert.Equal(Ds4Buttons.Options, chordProcessor.Process(Ds4Buttons.L1 | Ds4Buttons.R1, TimeSpan.Zero));

        var singleProcessor = new MacroProcessor(definitions, Array.Empty<MacroSequence>());
        Assert.Equal(Ds4Buttons.Cross, singleProcessor.Process(Ds4Buttons.L1, TimeSpan.Zero));
    }
}