using System.Diagnostics;
using PadScope.Core.Input;

namespace PadScope.Hid.Virtual;

public sealed class Ds4PassThrough : IDisposable
{
    private readonly Ds4ControllerSession _physical;
    private readonly IVirtualControllerTarget _virtual;
    private readonly ControllerProfile? _profile;
    private readonly MacroProcessor? _macros;
    private readonly Stopwatch _macroClock = new();
    private readonly System.Threading.Timer? _rumbleResetTimer;
    private bool _disposed;

    public Ds4PassThrough(
        Ds4ControllerSession physical,
        IVirtualControllerTarget virtualTarget,
        ControllerProfile? profile = null)
    {
        _physical = physical;
        _virtual = virtualTarget;
        _profile = profile;
        _macros = profile is not null &&
                  (profile.Macros.Count > 0 || profile.Sequences.Count > 0)
            ? new MacroProcessor(profile.Macros, profile.Sequences)
            : null;

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

        if (!_physical.TryStart(out error))
        {
            return false;
        }

        _macroClock.Restart();
        return true;
    }

    public void Stop()
    {
        _physical.Stop();
        _virtual.Disconnect();
    }

    private void ForwardInput(Ds4InputState state)
    {
        Ds4InputState output = _profile is null ? state : Ds4Remapper.Apply(_profile, state);

        if (_macros is not null)
        {
            TimeSpan elapsed = _macroClock.Elapsed;
            _macroClock.Restart();

            if (elapsed > TimeSpan.FromMilliseconds(250))
            {
                elapsed = TimeSpan.FromMilliseconds(250);
            }

            Ds4Buttons buttons = _macros.Process(output.Buttons, elapsed);
            output = output with { Buttons = buttons };
        }

        _virtual.Update(output);
    }

    private void ForwardFeedback(VirtualControllerFeedback feedback)
    {
        if (_profile?.ApplyRumble ?? true)
        {
            _physical.TrySendRumble(feedback.SmallMotor, feedback.LargeMotor, out _);
        }

        if ((_profile?.ApplyLightbar ?? true) &&
            (feedback.Red != 0 || feedback.Green != 0 || feedback.Blue != 0))
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

        _physical.TrySendRumble((byte)0, (byte)0, out string? _);
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