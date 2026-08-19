using PadScope.Core.Input;

namespace PadScope.Hid.Virtual;

public sealed class Ds4PassThrough : IDisposable
{
    private readonly Ds4ControllerSession _physical;
    private readonly IVirtualControllerTarget _virtual;
    private readonly System.Threading.Timer? _rumbleResetTimer;
    private bool _disposed;

    public Ds4PassThrough(Ds4ControllerSession physical, IVirtualControllerTarget virtualTarget)
    {
        _physical = physical;
        _virtual = virtualTarget;

        _physical.StateUpdated += ForwardInput;
        _virtual.FeedbackReceived += ForwardFeedback;

        _rumbleResetTimer = new System.Threading.Timer(
            ResetRumble,
            null,
            Timeout.Infinite,
            Timeout.Infinite);
    }

    public bool TryStart(out string? error)
    {
        if (!_virtual.TryConnect(out error))
        {
            return false;
        }

        return _physical.TryStart(out error);
    }

    public void Stop()
    {
        _physical.Stop();
        _virtual.Disconnect();
    }

    private void ForwardInput(Ds4InputState state)
    {
        _virtual.Update(state);
    }

    private void ForwardFeedback(VirtualControllerFeedback feedback)
    {
        _physical.TrySendRumble(feedback.SmallMotor, feedback.LargeMotor, out _);

        if (feedback.Red != 0 || feedback.Green != 0 || feedback.Blue != 0)
        {
            _physical.TrySendLightbar(feedback.Red, feedback.Green, feedback.Blue, out _);
        }

        _rumbleResetTimer?.Change(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
    }

    private void ResetRumble(object? _)
    {
        if (_disposed)
        {
            return;
        }

        _physical.TrySendRumble(0, 0, out _);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _physical.StateUpdated -= ForwardInput;
        _virtual.FeedbackReceived -= ForwardFeedback;
        _rumbleResetTimer?.Dispose();
        Stop();
    }
}