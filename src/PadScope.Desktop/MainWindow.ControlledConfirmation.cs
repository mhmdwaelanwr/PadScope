using System.Windows;

namespace PadScope.Desktop;

public partial class MainWindow
{
    private WeakReference<object>? _confirmedControllerOutputSession;

    /// <summary>
    /// Local MessageBox facade for MainWindow partials. It forwards normal dialogs unchanged,
    /// but remembers the user's approval for rumble/lightbar output for the current live
    /// controller session only. A new live session gets a new Ds4ControllerSession instance,
    /// so the previous approval cannot leak across reconnects or controller changes.
    /// </summary>
    private static class MessageBox
    {
        public static MessageBoxResult Show(
            Window owner,
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon)
        {
            if (owner is MainWindow window &&
                string.Equals(caption, "PadScope controlled action", StringComparison.Ordinal) &&
                IsNativeControllerOutputPrompt(messageBoxText) &&
                window._liveSession is not null)
            {
                if (window._confirmedControllerOutputSession is not null &&
                    window._confirmedControllerOutputSession.TryGetTarget(out object? approvedSession) &&
                    ReferenceEquals(approvedSession, window._liveSession))
                {
                    return MessageBoxResult.Yes;
                }

                string sessionNote = messageBoxText +
                    "\n\nIf you choose Yes, PadScope will remember this approval for rumble/lightbar " +
                    "until you stop or restart the current live controller session.";

                MessageBoxResult result = System.Windows.MessageBox.Show(
                    owner,
                    sessionNote,
                    caption,
                    button,
                    icon);

                if (result == MessageBoxResult.Yes)
                {
                    window._confirmedControllerOutputSession = new WeakReference<object>(window._liveSession);
                }

                return result;
            }

            return System.Windows.MessageBox.Show(owner, messageBoxText, caption, button, icon);
        }

        public static MessageBoxResult Show(
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon) =>
            System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);

        public static MessageBoxResult Show(
            Window owner,
            string messageBoxText,
            string caption,
            MessageBoxButton button) =>
            System.Windows.MessageBox.Show(owner, messageBoxText, caption, button);

        public static MessageBoxResult Show(
            string messageBoxText,
            string caption,
            MessageBoxButton button) =>
            System.Windows.MessageBox.Show(messageBoxText, caption, button);

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption) =>
            System.Windows.MessageBox.Show(owner, messageBoxText, caption);

        public static MessageBoxResult Show(string messageBoxText, string caption) =>
            System.Windows.MessageBox.Show(messageBoxText, caption);

        public static MessageBoxResult Show(string messageBoxText) =>
            System.Windows.MessageBox.Show(messageBoxText);

        private static bool IsNativeControllerOutputPrompt(string text)
        {
            return text.Contains("rumble", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("vibration", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("lightbar", StringComparison.OrdinalIgnoreCase);
        }
    }
}
