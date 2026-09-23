using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.Views;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Shows dialogs through <see cref="MessageBox"/>, owned by the main window whenever it is on screen.
    /// An unowned dialog is positioned against whatever window Windows considers active, which for a dialog
    /// raised from the tray menu is the menu's own popup, under the pointer.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MessageBoxDialogService(IMainWindowAccessor mainWindowAccessor) : IDialogService
    {
        /// <inheritdoc />
        public MessageBoxResult AskYesNoCancel(string message, string caption)
        {
            var owner = Owner();

            var dialog = new AppDialog(message, caption, true);
            if (owner != null) dialog.Owner = owner;
            else dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
            return dialog.Result;
        }

        /// <inheritdoc />
        public void ShowError(string message, string caption)
        {
            var owner = Owner();

            var dialog = new AppDialog(message, caption, false);
            if (owner != null) dialog.Owner = owner;
            else dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
        }

        /// <summary>
        /// The window to own the dialog. Null when no main window has been registered or it is off screen,
        /// as it is for the storage failure reported before the window exists.
        /// </summary>
        private Window? Owner()
        {
            var window = mainWindowAccessor.MainWindow;

            return window?.IsVisible == true ? window : null;
        }
    }
}
