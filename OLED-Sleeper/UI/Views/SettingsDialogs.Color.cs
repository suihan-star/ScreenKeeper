using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace OLED_Sleeper.UI.Views;

public static partial class SettingsDialogs
{
    public static void ColorSetup(Window owner, ColorSettingsViewModel model, ColorScreenViewModel screen, Action returnToSwitch) =>
        CreateColorSetupDialog(owner, model, screen, returnToSwitch).ShowDialog();

    public static SettingsDialog CreateColorSetupDialog(Window owner, ColorSettingsViewModel model, ColorScreenViewModel screen, Action returnToSwitch)
    {
        var dialog = new SettingsDialog(owner, "屏幕设置与验证");
        dialog.Body.Children.Add(new TextBlock { Text = screen.Name, FontSize = 18, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 14) });
        var status = SettingsDialog.Text("");
        status.SetBinding(TextBlock.TextProperty, new Binding(nameof(screen.Status)) { Source = screen });
        dialog.Body.Children.Add(status);
        var steps = new StackPanel();
        dialog.Body.Children.Add(steps);
        dialog.Action("返回设置", () => dialog.Close());
        string? previous = null;
        void Refresh(object? sender = null, PropertyChangedEventArgs? args = null)
        {
            // Device polling must not continually rebuild the focused controls.
            var signature = $"{model.SavedEnabled}/{screen.NeedsRecovery}/{screen.NeedsValidation}/{screen.IsSupported}/{screen.ValidationMessage}/{model.CanEdit}";
            if (previous == signature) return;
            previous = signature;
            steps.Children.Clear();
            if ((model.SavedEnabled || !model.CanEdit) && (screen.NeedsRecovery || screen.NeedsValidation))
            {
                steps.Children.Add(SettingsDialog.Text("验证前需先关闭已开启的屏幕调节，并等待原色恢复。"));
                steps.Children.Add(new TextBlock { Text = model.EnabledScreenNames, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) });
                steps.Children.Add(SettingsDialog.Text("关闭状态将保存；未保存的时间与色温会保留，验证后由你重新开启。"));
                var stop = SettingsDialog.Button("关闭这些屏幕并继续验证", () => { });
                stop.IsEnabled = model.CanEdit;
                stop.Click += async (_, _) =>
                {
                    stop.IsEnabled = false;
                    bool success = await model.DisableForValidationAsync();
                    if (!success) status.Text = model.Notice;
                    previous = null;
                    Refresh();
                };
                steps.Children.Add(stop);
            }
            else if (screen.NeedsRecovery)
            {
                steps.Children.Add(SettingsDialog.Text("先退出其他调色软件并恢复正常颜色，再重新检查原色。检查不会改动屏幕颜色。"));
                var check = SettingsDialog.Button("重新检查原色", () => { });
                check.Command = screen.RecheckOriginalCommand;
                steps.Children.Add(check);
                var more = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
                more.Children.Add(SettingsDialog.Text("如果检查仍提示原色不同，可重新建立原色。此流程将列出所有受影响屏幕，需要你确认画面正常，并保留旧记录归档。"));
                var reset = SettingsDialog.Button("重新建立原色", () => { dialog.Close(); model.ResetBaselineCommand.Execute(null); });
                reset.IsEnabled = model.ResetBaselineCommand.CanExecute(null);
                more.Children.Add(reset);
                steps.Children.Add(new Expander { Header = UiText.Get("检查后仍需处理"), Content = more, Margin = new Thickness(0, 16, 0, 0) });
            }
            else if (screen.NeedsValidation)
            {
                steps.Children.Add(SettingsDialog.Text(screen.ValidationMessage));
                steps.Children.Add(SettingsDialog.Text("下一步只测试这块屏幕：调暖 15 秒，自动恢复，再由你确认实际效果。通过后在作息与色温中开启此屏并保存。"));
                var start = SettingsDialog.Button("进入 15 秒验证", () => { dialog.Close(); screen.ValidateCommand.Execute(null); });
                start.IsEnabled = screen.ValidateCommand.CanExecute(null);
                steps.Children.Add(start);
            }
            else if (screen.IsSupported)
                steps.Children.Add(SettingsDialog.Text("此屏幕已可用。在作息与色温中开启此屏并保存。"));
            else steps.Children.Add(SettingsDialog.Text("当前连接或显示模式暂不可用。调整为受支持的 SDR 扩展模式后，重新查看此屏幕状态。"));
        }
        model.PropertyChanged += Refresh;
        screen.PropertyChanged += Refresh;
        dialog.Closed += (_, _) => { model.PropertyChanged -= Refresh; screen.PropertyChanged -= Refresh; };
        Refresh();
        return dialog;
    }
}
