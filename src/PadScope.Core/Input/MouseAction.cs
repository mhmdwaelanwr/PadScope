namespace PadScope.Core.Input;

public enum MouseActionKind
{
    Move,
    ButtonDown,
    ButtonUp,
    Wheel
}

public enum MouseButton
{
    Left,
    Right,
    Middle
}

public readonly record struct MouseAction(
    MouseActionKind Kind,
    int DeltaX = 0,
    int DeltaY = 0,
    MouseButton Button = MouseButton.Left,
    int WheelDelta = 0);

public interface IMouseSink
{
    void Send(MouseAction action);
}