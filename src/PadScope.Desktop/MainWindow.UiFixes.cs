using System.Windows;

namespace PadScope.Desktop;

public partial class MainWindow
{
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        ApplyDarkTheme();
    }

    private void RefreshUiColors()
    {
        if (_isLightTheme)
        {
            ApplyLightTheme();
        }
        else
        {
            ApplyDarkTheme();
        }
    }
}
