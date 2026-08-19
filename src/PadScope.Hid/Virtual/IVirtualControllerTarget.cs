using PadScope.Core.Input;

namespace PadScope.Hid.Virtual;

public sealed record VirtualControllerFeedback(
    byte SmallMotor,
    byte LargeMotor,
    byte Red,
    byte Green,
    byte Blue,
    byte LedNumber);

public interface IVirtualControllerTarget : IDisposable
{
    bool IsConnected { get; }

    bool TryConnect(out string? error);

    void Disconnect();

    void Update(Ds4InputState state);

    event Action<VirtualControllerFeedback>? FeedbackReceived;
}