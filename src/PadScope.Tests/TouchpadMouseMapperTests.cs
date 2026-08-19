using PadScope.Core.Input;
using Xunit;

namespace PadScope.Tests;

public class TouchpadMouseMapperTests
{
    private static Ds4TouchPoint Touch(int x, int y)
    {
        return new Ds4TouchPoint(Touching: true, FingerId: 1, X: (ushort)x, Y: (ushort)y);
    }

    private static Ds4TouchPoint Lifted()
    {
        return new Ds4TouchPoint(Touching: false, FingerId: 1, X: 0, Y: 0);
    }

    [Fact]
    public void FingerMove_EmitsScaledMovement()
    {
        var sink = new FakeMouseSink();
        var mapper = new TouchpadMouseMapper(new TouchpadMouseSettings());

        mapper.Update(Touch(100, 100), null, padPressed: false, sink);
        mapper.Update(Touch(110, 100), null, padPressed: false, sink);

        MouseAction move = Assert.Single(sink.Actions);
        Assert.Equal(MouseActionKind.Move, move.Kind);
        Assert.Equal(10, move.DeltaX);
        Assert.Equal(0, move.DeltaY);
    }

    [Fact]
    public void FingerTap_EmitsLeftClick()
    {
        var sink = new FakeMouseSink();
        var mapper = new TouchpadMouseMapper(new TouchpadMouseSettings());

        mapper.Update(Touch(100, 100), null, padPressed: false, sink);
        mapper.Update(Lifted(), null, padPressed: false, sink);

        Assert.Equal(2, sink.Actions.Count);
        Assert.Equal(MouseActionKind.ButtonDown, sink.Actions[0].Kind);
        Assert.Equal(MouseButton.Left, sink.Actions[0].Button);
        Assert.Equal(MouseActionKind.ButtonUp, sink.Actions[1].Kind);
        Assert.Equal(MouseButton.Left, sink.Actions[1].Button);
    }

    [Fact]
    public void FingerDrag_EmitsLeftDownThenUp()
    {
        var sink = new FakeMouseSink();
        var mapper = new TouchpadMouseMapper(new TouchpadMouseSettings());

        mapper.Update(Touch(100, 100), null, padPressed: false, sink);
        mapper.Update(Touch(130, 100), null, padPressed: false, sink);
        mapper.Update(Touch(140, 100), null, padPressed: false, sink);
        mapper.Update(Lifted(), null, padPressed: false, sink);

        Assert.Equal(4, sink.Actions.Count);
        Assert.Equal(MouseActionKind.ButtonDown, sink.Actions[0].Kind);
        Assert.Equal(MouseButton.Left, sink.Actions[0].Button);
        Assert.Equal(MouseActionKind.Move, sink.Actions[1].Kind);
        Assert.Equal(MouseActionKind.Move, sink.Actions[2].Kind);
        Assert.Equal(MouseActionKind.ButtonUp, sink.Actions[3].Kind);
        Assert.Equal(MouseButton.Left, sink.Actions[3].Button);
    }

    [Fact]
    public void TwoFingers_HoldRightButton()
    {
        var sink = new FakeMouseSink();
        var mapper = new TouchpadMouseMapper(new TouchpadMouseSettings());

        mapper.Update(Touch(100, 100), null, padPressed: false, sink);
        mapper.Update(Touch(110, 100), Touch(200, 200), padPressed: false, sink);
        mapper.Update(Touch(110, 100), Lifted(), padPressed: false, sink);

        Assert.Equal(2, sink.Actions.Count);
        Assert.Equal(MouseActionKind.ButtonDown, sink.Actions[0].Kind);
        Assert.Equal(MouseButton.Right, sink.Actions[0].Button);
        Assert.Equal(MouseActionKind.ButtonUp, sink.Actions[1].Kind);
        Assert.Equal(MouseButton.Right, sink.Actions[1].Button);
    }

    [Fact]
    public void PadPress_HoldsLeftButton()
    {
        var sink = new FakeMouseSink();
        var mapper = new TouchpadMouseMapper(new TouchpadMouseSettings());

        mapper.Update(null, null, padPressed: true, sink);
        mapper.Update(null, null, padPressed: false, sink);

        Assert.Equal(2, sink.Actions.Count);
        Assert.Equal(MouseActionKind.ButtonDown, sink.Actions[0].Kind);
        Assert.Equal(MouseButton.Left, sink.Actions[0].Button);
        Assert.Equal(MouseActionKind.ButtonUp, sink.Actions[1].Kind);
        Assert.Equal(MouseButton.Left, sink.Actions[1].Button);
    }

    [Fact]
    public void Sensitivity_ScalesMovement()
    {
        var sink = new FakeMouseSink();
        var mapper = new TouchpadMouseMapper(new TouchpadMouseSettings { Sensitivity = 2.0 });

        mapper.Update(Touch(100, 100), null, padPressed: false, sink);
        mapper.Update(Touch(105, 100), null, padPressed: false, sink);

        MouseAction move = Assert.Single(sink.Actions);
        Assert.Equal(10, move.DeltaX);
    }
}