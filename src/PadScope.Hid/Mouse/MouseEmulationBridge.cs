using System.Diagnostics;
using PadScope.Core.Input;

namespace PadScope.Hid.Mouse;

public sealed class MouseEmulationBridge : IDisposable
{
    private readonly Ds4ControllerSession _physical;
    private readonly IMouseSink _sink;
    private readonly TouchpadMouseMapper? _touchpad;
    private readonly GyroMouseMapper? _gyro;
    private readonly Stopwatch _clock = new();
    private bool _disposed;

    public MouseEmulationBridge(
        Ds4ControllerSession physical,
        IMouseSink sink,
        TouchpadMouseSettings? touchpad = null,
        GyroMouseSettings? gyro = null)
    {
        _physical = physical;
        _sink = sink;
        _touchpad = touchpad is null ? null : new TouchpadMouseMapper(touchpad);
        _gyro = gyro is null ? null : new GyroMouseMapper(gyro);
    }

    public bool TryStart(out string? error)
    {
        _physical.StateUpdated += OnState;

        if (!_physical.TryStart(out error))
        {
            _physical.StateUpdated -= OnState;
            return false;
        }

        _clock.Restart();
        return true;
    }

    public void Stop()
    {
        _physical.Stop();
    }

    private void OnState(Ds4InputState state)
    {
        TimeSpan elapsed = _clock.Elapsed;
        _clock.Restart();

        _touchpad?.Update(
            state.Touch1,
            state.Touch2,
            state.Buttons.HasFlag(Ds4Buttons.TouchpadClick),
            _sink);
        _gyro?.Update(state.GyroX, state.GyroY, state.GyroZ, elapsed, _sink);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _physical.StateUpdated -= OnState;
        _physical.Stop();
    }
}