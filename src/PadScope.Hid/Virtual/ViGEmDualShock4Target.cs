using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using PadScope.Core.Input;

namespace PadScope.Hid.Virtual;

public sealed class ViGEmDualShock4Target : IVirtualControllerTarget
{
    private ViGEmClient? _client;
    private IDualShock4Controller? _pad;
    private Thread? _feedbackThread;
    private volatile bool _keepReading;
    private bool _isConnected;
    private bool _disposed;

    public bool IsConnected => _isConnected;

    public event Action<VirtualControllerFeedback>? FeedbackReceived;

    public bool TryConnect(out string? error)
    {
        try
        {
            _client = new ViGEmClient();
            _pad = _client.CreateDualShock4Controller();
            _pad.AutoSubmitReport = false;
            _pad.Connect();
            _isConnected = true;
            error = null;

            StartFeedbackLoop();
            return true;
        }
        catch (Exception ex)
        {
            _keepReading = false;
            _pad?.Dispose();
            _pad = null;
            _client?.Dispose();
            _client = null;
            error = DescribeException(ex);
            return false;
        }
    }

    public void Disconnect()
    {
        _keepReading = false;
        _feedbackThread?.Join(TimeSpan.FromMilliseconds(500));
        _feedbackThread = null;

        if (_pad is not null && _isConnected)
        {
            try
            {
                _pad.Disconnect();
            }
            catch
            {
                // The bus may already have removed the device.
            }
        }

        _isConnected = false;

        _pad?.Dispose();
        _pad = null;
        _client?.Dispose();
        _client = null;
    }

    public void Update(Ds4InputState state)
    {
        if (_pad is null || !_isConnected)
        {
            return;
        }

        _pad.SetAxisValue(DualShock4Axis.LeftThumbX, state.LeftStickX);
        _pad.SetAxisValue(DualShock4Axis.LeftThumbY, state.LeftStickY);
        _pad.SetAxisValue(DualShock4Axis.RightThumbX, state.RightStickX);
        _pad.SetAxisValue(DualShock4Axis.RightThumbY, state.RightStickY);
        _pad.SetSliderValue(DualShock4Slider.LeftTrigger, state.LeftTrigger);
        _pad.SetSliderValue(DualShock4Slider.RightTrigger, state.RightTrigger);

        _pad.SetButtonState(DualShock4Button.Triangle, state.Buttons.HasFlag(Ds4Buttons.Triangle));
        _pad.SetButtonState(DualShock4Button.Circle, state.Buttons.HasFlag(Ds4Buttons.Circle));
        _pad.SetButtonState(DualShock4Button.Cross, state.Buttons.HasFlag(Ds4Buttons.Cross));
        _pad.SetButtonState(DualShock4Button.Square, state.Buttons.HasFlag(Ds4Buttons.Square));
        _pad.SetButtonState(DualShock4Button.ShoulderLeft, state.Buttons.HasFlag(Ds4Buttons.L1));
        _pad.SetButtonState(DualShock4Button.ShoulderRight, state.Buttons.HasFlag(Ds4Buttons.R1));
        _pad.SetButtonState(DualShock4Button.TriggerLeft, state.Buttons.HasFlag(Ds4Buttons.L2));
        _pad.SetButtonState(DualShock4Button.TriggerRight, state.Buttons.HasFlag(Ds4Buttons.R2));
        _pad.SetButtonState(DualShock4Button.Share, state.Buttons.HasFlag(Ds4Buttons.Share));
        _pad.SetButtonState(DualShock4Button.Options, state.Buttons.HasFlag(Ds4Buttons.Options));
        _pad.SetButtonState(DualShock4Button.ThumbLeft, state.Buttons.HasFlag(Ds4Buttons.L3));
        _pad.SetButtonState(DualShock4Button.ThumbRight, state.Buttons.HasFlag(Ds4Buttons.R3));
        _pad.SetButtonState(DualShock4SpecialButton.Ps, state.Buttons.HasFlag(Ds4Buttons.Ps));
        _pad.SetButtonState(DualShock4SpecialButton.Touchpad, state.Buttons.HasFlag(Ds4Buttons.TouchpadClick));

        _pad.SetDPadDirection(ToDpadDirection(state.Buttons));

        _pad.SubmitReport();
    }

    private void StartFeedbackLoop()
    {
        _keepReading = true;
        _feedbackThread = new Thread(FeedbackLoop)
        {
            IsBackground = true,
            Name = "PadScope.ViGEm.Ds4Feedback"
        };
        _feedbackThread.Start();
    }

    private void FeedbackLoop()
    {
        if (_pad is null)
        {
            return;
        }

        while (_keepReading)
        {
            try
            {
                IEnumerable<byte> buffer = _pad.AwaitRawOutputReport(250, out bool timedOut);
                if (timedOut)
                {
                    continue;
                }

                byte[] bytes = buffer.ToArray();
                if (bytes.Length < 6)
                {
                    continue;
                }

                FeedbackReceived?.Invoke(new VirtualControllerFeedback(
                    SmallMotor: bytes[1],
                    LargeMotor: bytes[2],
                    Red: bytes[3],
                    Green: bytes[4],
                    Blue: bytes[5],
                    LedNumber: 0));
            }
            catch (Exception)
            {
                if (!_keepReading)
                {
                    return;
                }

                Thread.Sleep(200);
            }
        }
    }

    private static DualShock4DPadDirection ToDpadDirection(Ds4Buttons buttons)
    {
        bool up = buttons.HasFlag(Ds4Buttons.DpadUp);
        bool down = buttons.HasFlag(Ds4Buttons.DpadDown);
        bool left = buttons.HasFlag(Ds4Buttons.DpadLeft);
        bool right = buttons.HasFlag(Ds4Buttons.DpadRight);

        if (up && right) return DualShock4DPadDirection.Northeast;
        if (up && left) return DualShock4DPadDirection.Northwest;
        if (down && right) return DualShock4DPadDirection.Southeast;
        if (down && left) return DualShock4DPadDirection.Southwest;
        if (up) return DualShock4DPadDirection.North;
        if (down) return DualShock4DPadDirection.South;
        if (left) return DualShock4DPadDirection.West;
        if (right) return DualShock4DPadDirection.East;
        return DualShock4DPadDirection.None;
    }

    private static string DescribeException(Exception ex)
    {
        return ex switch
        {
            VigemBusNotFoundException => "ViGEmBus driver is not installed or the service is not running.",
            VigemNoFreeSlotException => "ViGEmBus has no free controller slot left.",
            _ => $"ViGEmBus error: {ex.Message}"
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Disconnect();
    }
}