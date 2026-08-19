using PadScope.Core.Input;
using Xunit;

namespace PadScope.Tests;

public class GyroMouseMapperTests
{
    private static readonly GyroMouseSettings Instant = new()
    {
        Smoothing = 0,
        Deadzone = 0
    };

    [Fact]
    public void ZeroElapsed_ProducesNoMovement()
    {
        var sink = new FakeMouseSink();
        var mapper = new GyroMouseMapper(Instant);

        mapper.Update(0, 0, 1000, TimeSpan.Zero, sink);

        Assert.Empty(sink.Actions);
    }

    [Fact]
    public void PositiveGyroZ_MovesPositiveX()
    {
        var sink = new FakeMouseSink();
        var mapper = new GyroMouseMapper(Instant);

        mapper.Update(0, 0, 1000, TimeSpan.FromSeconds(1), sink);

        MouseAction move = Assert.Single(sink.Actions);
        Assert.Equal(MouseActionKind.Move, move.Kind);
        Assert.Equal(350, move.DeltaX);
        Assert.Equal(0, move.DeltaY);
    }

    [Fact]
    public void GyroX_DrivesY_ByDefault()
    {
        var sink = new FakeMouseSink();
        var mapper = new GyroMouseMapper(Instant);

        mapper.Update(1000, 0, 0, TimeSpan.FromSeconds(1), sink);

        MouseAction move = Assert.Single(sink.Actions);
        Assert.Equal(0, move.DeltaX);
        Assert.Equal(350, move.DeltaY);
    }

    [Fact]
    public void SwapAxes_DrivesXFromGyroX()
    {
        var sink = new FakeMouseSink();
        var mapper = new GyroMouseMapper(Instant with { SwapAxes = true });

        mapper.Update(1000, 0, 0, TimeSpan.FromSeconds(1), sink);

        MouseAction move = Assert.Single(sink.Actions);
        Assert.Equal(350, move.DeltaX);
        Assert.Equal(0, move.DeltaY);
    }

    [Fact]
    public void InvertX_FlipsDirection()
    {
        var sink = new FakeMouseSink();
        var mapper = new GyroMouseMapper(Instant with { InvertX = true });

        mapper.Update(0, 0, -1000, TimeSpan.FromSeconds(1), sink);

        MouseAction move = Assert.Single(sink.Actions);
        Assert.Equal(350, move.DeltaX);
    }

    [Fact]
    public void Deadzone_SuppressesSmallRates()
    {
        var sink = new FakeMouseSink();
        var mapper = new GyroMouseMapper(Instant with { Deadzone = 50 });

        mapper.Update(0, 0, 20, TimeSpan.FromSeconds(1), sink);

        Assert.Empty(sink.Actions);
    }

    [Fact]
    public void Sensitivity_ScalesDelta()
    {
        var sink = new FakeMouseSink();
        var mapper = new GyroMouseMapper(Instant with { Sensitivity = 2.0 });

        mapper.Update(0, 0, 500, TimeSpan.FromSeconds(1), sink);

        MouseAction move = Assert.Single(sink.Actions);
        Assert.Equal(350, move.DeltaX);
    }

    [Fact]
    public void SmoothingAtMax_BlocksAllMovement()
    {
        var sink = new FakeMouseSink();
        var mapper = new GyroMouseMapper(Instant with { Smoothing = 1.0 });

        mapper.Update(0, 0, 1000, TimeSpan.FromSeconds(1), sink);

        Assert.Empty(sink.Actions);
    }
}