using System.Windows;
using System.Windows.Threading;

namespace PadScope.Desktop;

public partial class App : Application
{
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() =>
            {
                if (MainWindow is PadScope.Desktop.MainWindow window)
                {
                    window.InstallResponsiveHidBehavior();
                }
            }));
    }
}
