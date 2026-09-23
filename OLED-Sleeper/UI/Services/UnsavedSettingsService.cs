using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Services.Interfaces;
using System.Windows;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Prompts through <see cref="IDialogService"/>, saving the settings on Yes and discarding them on No.
    /// </summary>
    public class UnsavedSettingsService(
        IUnsavedSettings unsavedSettings,
        IDialogService dialogService) : IUnsavedSettingsService
    {
        private static string Caption => UiText.Get("尚未保存的更改");

        /// <inheritdoc />
        public bool ConfirmHide() => true;

        /// <inheritdoc />
        public bool ConfirmExit() =>
            Confirm(UiText.Get("有尚未保存的更改，是否在退出前保存？") +
                (string.IsNullOrWhiteSpace(unsavedSettings.DirtyModules) ? "" : "\n\n" + unsavedSettings.DirtyModules));

        /// <summary>
        /// Prompts only when there is something to lose. Yes answers with whether the save succeeded;
        /// No discards the changes.
        /// </summary>
        private bool Confirm(string question)
        {
            if (!unsavedSettings.IsDirty) return true;

            var result = dialogService.AskYesNoCancel(question, Caption);

            if (result == MessageBoxResult.Cancel) return false;

            if (result == MessageBoxResult.Yes) return unsavedSettings.TrySaveChanges();

            unsavedSettings.DiscardChanges();
            return true;
        }
    }
}
