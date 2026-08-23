using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace PadScope.Desktop;

public partial class ModernLiveDashboard
{
    private bool _stabilityVisualCleanupApplied;

    internal void ApplyStabilityVisualCleanup()
    {
        if (_stabilityVisualCleanupApplied)
        {
            return;
        }

        // Keep one controller-polish implementation only. EnsureControllerPolish()
        // is the original layer already used by MainWindow.PostLoadPolish.
        EnsureControllerPolish();
        _stabilityVisualCleanupApplied = true;

        // The controller visualizer is display-only. Tooltips on the face buttons
        // were appearing as large white rectangles over the controller at runtime.
        // Disable hit testing/tooltips across the canvas without affecting live HID
        // telemetry updates to the visual elements.
        if (ControllerLeftStickDot.Parent is Canvas canvas)
        {
            foreach (UIElement child in canvas.Children)
            {
                child.IsHitTestVisible = false;
                if (child is FrameworkElement element)
                {
                    element.ToolTip = null;
                }
            }
        }

        foreach (Shape shape in new Shape[]
                 {
                     TriangleShape, CircleShape, CrossShape, SquareShape,
                     DpadUpShape, DpadDownShape, DpadLeftShape, DpadRightShape,
                     PsShape
                 })
        {
            shape.ToolTip = null;
            shape.IsHitTestVisible = false;
        }
    }
}
