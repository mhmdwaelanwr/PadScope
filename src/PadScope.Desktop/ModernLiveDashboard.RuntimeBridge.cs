using PadScope.Core.Input;

namespace PadScope.Desktop;

public partial class ModernLiveDashboard
{
    internal void EnsureControllerPolish() => InstallControllerPolish();

    internal void ApplyExtendedButtonTelemetry(Ds4InputState state)
    {
        InstallControllerPolish();
        UpdateExtendedButtonTelemetry(state);

        Ds4Buttons[] pressed = Enum.GetValues<Ds4Buttons>()
            .Where(button => button != Ds4Buttons.None && state.Buttons.HasFlag(button))
            .ToArray();

        PressedButtonsText.Text = pressed.Length == 0
            ? "No buttons pressed · all DS4 buttons monitored"
            : string.Join("  ·  ", pressed.Select(button => button.ToString()));
    }
}
