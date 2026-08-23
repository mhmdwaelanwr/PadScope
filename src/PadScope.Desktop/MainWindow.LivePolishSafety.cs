using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace PadScope.Desktop;

public partial class MainWindow
{
    /// <summary>
    /// The legacy Output Tests XAML nests sliders/buttons inside child StackPanels.
    /// RebuildOutputLab moves those same controls into a new visual structure.
    /// WPF does not allow a FrameworkElement to have two logical parents, so the
    /// movable controls must be detached from their immediate legacy parents first.
    /// ResetOutputButton intentionally stays attached: its direct parent is the
    /// Output Tests root panel that RebuildOutputLab uses as the replacement host.
    /// </summary>
    private void PrepareLegacyOutputControlsForReparenting()
    {
        FrameworkElement[] movableControls =
        {
            RumbleSmallSlider,
            RumbleLargeSlider,
            LightbarRedSlider,
            LightbarGreenSlider,
            LightbarBlueSlider,
            PulseRumbleButton,
            SetLightbarButton
        };

        foreach (FrameworkElement control in movableControls)
        {
            DetachFromLogicalParent(control);
        }
    }

    private static void DetachFromLogicalParent(FrameworkElement element)
    {
        switch (element.Parent)
        {
            case Panel panel:
                panel.Children.Remove(element);
                break;

            case Decorator decorator when ReferenceEquals(decorator.Child, element):
                decorator.Child = null;
                break;

            case ContentControl contentControl when ReferenceEquals(contentControl.Content, element):
                contentControl.Content = null;
                break;
        }
    }

    private void ReportNonCriticalPolishFailure(Exception exception)
    {
        Debug.WriteLine($"PadScope Live Input polish fallback: {exception}");

        // Visual polish must never make the diagnostics application unusable.
        // Keep the original controls available and surface a non-blocking status.
        if (StatusText is not null)
        {
            StatusText.Text = "Live Input loaded with compatibility styling";
        }
    }
}
