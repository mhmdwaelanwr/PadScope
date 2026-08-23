using PadScope.Core.Input;

namespace PadScope.Desktop;

public partial class ModernLiveDashboard
{
    private bool _controllerPolishReady;

    internal void EnsureControllerPolish()
    {
        if (_controllerPolishReady)
        {
            return;
        }

        InstallControllerPolish();
        _controllerPolishReady = true;
    }

    internal void ApplyExtendedButtonTelemetry(Ds4InputState state)
    {
        EnsureControllerPolish();
        UpdateExtendedButtonTelemetry(state);

        Ds4Buttons[] pressed = Enum.GetValues<Ds4Buttons>()
            .Where(button => button != Ds4Buttons.None && state.Buttons.HasFlag(button))
            .ToArray();

        PressedButtonsText.Text = pressed.Length == 0
            ? "No buttons pressed · all DS4 buttons monitored"
            : string.Join("  ·  ", pressed.Select(button => button.ToString()));
    }
}
