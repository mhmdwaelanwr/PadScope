using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using PadScope.Core.Input;

namespace PadScope.Hid.Virtual;

public sealed class ViGEmXbox360Target : IVirtualControllerTarget
{
    private ViGEmClient? _client;
    private IXbox360Controller? _pad;
    private bool _isConnected;
    private bool _disposed;

    public bool IsConnected => _isConnected;

    public event Action<VirtualControllerFeedback>? FeedbackReceived;

    public bool TryConnect(out string? error)
    {
        try
        {
            _client = new ViGEmClient();
            _pad = _client.CreateXbox360Controller();
            _pad.AutoSubmitReport = false;
            _pad.FeedbackReceived += OnFeedback;
            _pad.Connect();
            _isConnected = true;
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            if (_pad is not null)
            {
                _pad.FeedbackReceived -= OnFeedback;
            }

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
        if (_pad is not null)
        {
            _pad.FeedbackReceived -= OnFeedback;

            if (_isConnected)
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

        Xbox360InputState x = Ds4ToXboxMapper.Map(state);

        _pad.SetButtonState(Xbox360Button.A, x.A);
        _pad.SetButtonState(Xbox360Button.B, x.B);
        _pad.SetButtonState(Xbox360Button.X, x.X);
        _pad.SetButtonState(Xbox360Button.Y, x.Y);
        _pad.SetButtonState(Xbox360Button.LeftShoulder, x.LeftShoulder);
        _pad.SetButtonState(Xbox360Button.RightShoulder, x.RightShoulder);
        _pad.SetButtonState(Xbox360Button.Back, x.Back);
        _pad.SetButtonState(Xbox360Button.Start, x.Start);
        _pad.SetButtonState(Xbox360Button.Guide, x.Guide);
        _pad.SetButtonState(Xbox360Button.LeftThumb, x.LeftThumb);
        _pad.SetButtonState(Xbox360Button.RightThumb, x.RightThumb);
        _pad.SetButtonState(Xbox360Button.Up, x.DpadUp);
        _pad.SetButtonState(Xbox360Button.Down, x.DpadDown);
        _pad.SetButtonState(Xbox360Button.Left, x.DpadLeft);
        _pad.SetButtonState(Xbox360Button.Right, x.DpadRight);

        _pad.SetSliderValue(Xbox360Slider.LeftTrigger, x.LeftTrigger);
        _pad.SetSliderValue(Xbox360Slider.RightTrigger, x.RightTrigger);
        _pad.SetAxisValue(Xbox360Axis.LeftThumbX, x.LeftThumbX);
        _pad.SetAxisValue(Xbox360Axis.LeftThumbY, x.LeftThumbY);
        _pad.SetAxisValue(Xbox360Axis.RightThumbX, x.RightThumbX);
        _pad.SetAxisValue(Xbox360Axis.RightThumbY, x.RightThumbY);

        _pad.SubmitReport();
    }

    private void OnFeedback(object? sender, Xbox360FeedbackReceivedEventArgs e)
    {
        FeedbackReceived?.Invoke(new VirtualControllerFeedback(
            SmallMotor: e.SmallMotor,
            LargeMotor: e.LargeMotor,
            Red: 0,
            Green: 0,
            Blue: 0,
            LedNumber: e.LedNumber));
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