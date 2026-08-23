using System.Windows;
using System.Windows.Controls;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private bool _legacyInputButtonsPolished;

    private void PolishLegacyInputButtonsSafe()
    {
        if (_legacyInputButtonsPolished) return;
        _legacyInputButtonsPolished = true;

        Style stateStyle = CreateLegacyStateButtonStyle();

        foreach (Button button in new[] { DpadUpButton, DpadDownButton, DpadLeftButton, DpadRightButton })
        {
            button.Style = stateStyle;
            button.Width = 36;
            button.Height = 36;
            button.MinWidth = 0;
            button.Margin = new Thickness(2);
            button.FontSize = 15;
            button.FontWeight = FontWeights.SemiBold;
        }
        DpadUpButton.Content = "↑";
        DpadDownButton.Content = "↓";
        DpadLeftButton.Content = "←";
        DpadRightButton.Content = "→";
        DpadUpButton.ToolTip = "D-Pad Up";
        DpadDownButton.ToolTip = "D-Pad Down";
        DpadLeftButton.ToolTip = "D-Pad Left";
        DpadRightButton.ToolTip = "D-Pad Right";

        foreach (Button button in new[] { TriangleButton, CrossButton, SquareButton, CircleButton })
        {
            button.Style = stateStyle;
            button.Width = 36;
            button.Height = 36;
            button.MinWidth = 0;
            button.Margin = new Thickness(2);
            button.FontSize = 15;
            button.FontWeight = FontWeights.SemiBold;
        }
        TriangleButton.Content = "△";
        CrossButton.Content = "×";
        SquareButton.Content = "□";
        CircleButton.Content = "○";
        TriangleButton.ToolTip = "Triangle";
        CrossButton.ToolTip = "Cross";
        SquareButton.ToolTip = "Square";
        CircleButton.ToolTip = "Circle";

        foreach (Button button in new[]
                 {
                     L1Button, R1Button, L2Button, R2Button, L3Button, R3Button,
                     ShareButton, OptionsButton, PsButton, TouchpadButton
                 })
        {
            button.Style = stateStyle;
            button.MinWidth = 0;
            button.Width = 48;
            button.Height = 34;
            button.Margin = new Thickness(0, 0, 6, 6);
            button.Padding = new Thickness(4, 0, 4, 0);
            button.FontSize = 10.5;
        }

        ShareButton.Content = "Share";
        ShareButton.Width = 60;
        OptionsButton.Content = "Options";
        OptionsButton.Width = 66;
        TouchpadButton.Content = "Touch";
        TouchpadButton.Width = 58;
        PsButton.Content = "PS";

        L1Button.ToolTip = "Left shoulder (L1)";
        R1Button.ToolTip = "Right shoulder (R1)";
        L2Button.ToolTip = "Left trigger digital state (L2)";
        R2Button.ToolTip = "Right trigger digital state (R2)";
        L3Button.ToolTip = "Left stick click (L3)";
        R3Button.ToolTip = "Right stick click (R3)";
        ShareButton.ToolTip = "Share / Create";
        OptionsButton.ToolTip = "Options";
        PsButton.ToolTip = "PlayStation / Home";
        TouchpadButton.ToolTip = "Touchpad click";
    }

    private Style CreateLegacyStateButtonStyle()
    {
        Style baseStyle = (Style)FindResource("Sec");
        Style style = new(typeof(Button), baseStyle);
        style.Setters.Add(new Setter(Control.MinWidthProperty, 0d));
        style.Setters.Add(new Setter(Control.HeightProperty, 34d));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        return style;
    }
}
