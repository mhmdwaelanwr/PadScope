using System.Windows;

namespace PadScope.Desktop;

public partial class MainWindow
{
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
