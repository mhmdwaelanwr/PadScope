namespace PadScope.Core.Input;

public sealed class GyroMouseMapper
{
    private const double GyroToPixels = 0.35;

    private readonly GyroMouseSettings _settings;
    private double _smoothX;
    private double _smoothY;

    public GyroMouseMapper(GyroMouseSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void Update(short gx, short gy, short gz, TimeSpan elapsed, IMouseSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        double rawX = _settings.SwapAxes ? gx : gz;
        double rawY = _settings.SwapAxes ? gz : gx;

        double seconds = elapsed.TotalSeconds;
        double deltaX = AxisDelta(rawX, _settings.InvertX, seconds);
        double deltaY = AxisDelta(rawY, _settings.InvertY, seconds);

        _smoothX = _settings.Smoothing * _smoothX + (1 - _settings.Smoothing) * deltaX;
        _smoothY = _settings.Smoothing * _smoothY + (1 - _settings.Smoothing) * deltaY;

        int pixelsX = (int)Math.Round(_smoothX);
        int pixelsY = (int)Math.Round(_smoothY);

        if (pixelsX != 0 || pixelsY != 0)
        {
            sink.Send(new MouseAction(MouseActionKind.Move, pixelsX, pixelsY));
        }
    }

    private double AxisDelta(double raw, bool invert, double seconds)
    {
        if (Math.Abs(raw) < _settings.Deadzone)
        {
            return 0;
        }

        double sign = invert ? -1.0 : 1.0;
        return sign * raw * _settings.Sensitivity * GyroToPixels * seconds;
    }
}