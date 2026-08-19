namespace PadScope.Core.Input;

public sealed class TouchpadMouseMapper
{
    private readonly TouchpadMouseSettings _settings;

    private int? _lastX;
    private int? _lastY;
    private long _totalMove;
    private bool _dragHeld;
    private bool _padHeld;
    private bool _rightHeld;
    private bool _twoFingers;

    public TouchpadMouseMapper(TouchpadMouseSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void Update(Ds4TouchPoint? touch1, Ds4TouchPoint? touch2, bool padPressed, IMouseSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        bool fingerDown = touch1?.Touching == true;
        bool twoFingers = fingerDown && touch2?.Touching == true;

        if (twoFingers && !_twoFingers)
        {
            if (_dragHeld)
            {
                sink.Send(Down(MouseButton.Left));
                _dragHeld = false;
            }

            if (!_rightHeld)
            {
                sink.Send(Down(MouseButton.Right));
                _rightHeld = true;
            }

            ResetTouchState();
        }
        else if (!twoFingers && _twoFingers)
        {
            if (_rightHeld)
            {
                sink.Send(Up(MouseButton.Right));
                _rightHeld = false;
            }

            ResetTouchState();
        }

        _twoFingers = twoFingers;

        if (twoFingers)
        {
            return;
        }

        if (padPressed && !_padHeld && !_dragHeld)
        {
            sink.Send(Down(MouseButton.Left));
            _padHeld = true;
        }
        else if (!padPressed && _padHeld)
        {
            sink.Send(Up(MouseButton.Left));
            _padHeld = false;
        }

        if (fingerDown && touch1 is { } point)
        {
            int x = point.X;
            int y = point.Y;

            if (_lastX is null)
            {
                _lastX = x;
                _lastY = y;
                _totalMove = 0;
                return;
            }

            int dx = x - _lastX.Value;
            int dy = y - _lastY.Value;
            _lastX = x;
            _lastY = y;
            _totalMove += Math.Abs(dx) + Math.Abs(dy);

            int scaledX = (int)Math.Round(dx * _settings.Sensitivity);
            int scaledY = (int)Math.Round(dy * _settings.Sensitivity);

            if (scaledX != 0 || scaledY != 0)
            {
                sink.Send(new MouseAction(MouseActionKind.Move, scaledX, scaledY));
            }

            if (!_dragHeld && !_padHeld && _totalMove > _settings.TapThreshold)
            {
                sink.Send(Down(MouseButton.Left));
                _dragHeld = true;
            }
        }
        else if (_lastX is not null)
        {
            if (_dragHeld)
            {
                sink.Send(Up(MouseButton.Left));
                _dragHeld = false;
            }
            else if (!_padHeld && _settings.EnableTapClick)
            {
                sink.Send(Down(MouseButton.Left));
                sink.Send(Up(MouseButton.Left));
            }

            ResetTouchState();
        }
    }

    private void ResetTouchState()
    {
        _lastX = null;
        _lastY = null;
        _totalMove = 0;
        _dragHeld = false;
    }

    private static MouseAction Down(MouseButton button)
    {
        return new MouseAction(MouseActionKind.ButtonDown, Button: button);
    }

    private static MouseAction Up(MouseButton button)
    {
        return new MouseAction(MouseActionKind.ButtonUp, Button: button);
    }
}