using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Models;
using System.Windows;
using System.Windows.Controls;

namespace OLED_Sleeper.UI.Views;

public static partial class SettingsDialogs
{
    public static void Snooze(Window owner, string screen, MonitorSettingsScope scope, int seconds, bool canJoint, Action<int, bool> apply)
        => CreateSnoozeDialog(owner, screen, scope, seconds, canJoint, apply).ShowDialog();

    public static SettingsDialog CreateSnoozeDialog(Window owner, string screen, MonitorSettingsScope scope, int seconds, bool canJoint, Action<int, bool> apply)
    {
        var dialog = new SettingsDialog(owner, scope == MonitorSettingsScope.Rest ? "暂缓休息" : "暂缓保护");
        dialog.Body.Children.Add(new TextBlock { Text = screen, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });
        dialog.Body.Children.Add(SettingsDialog.Text("只影响本次自动计时；不会解除隐私幕、唤醒已休息屏幕或保存设置草稿。"));
        var value = new TextBox { Text = Math.Max(1, seconds / 60).ToString(), Width = 100, HorizontalAlignment = HorizontalAlignment.Left };
        var unit = new ComboBox { Width = 130, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 12), SelectedValuePath = "Tag" };
        var minutesItem = new ComboBoxItem { Tag = TimeUnit.Minutes }; SettingsDialog.Bind(minutesItem, ContentControl.ContentProperty, "分钟");
        var hoursItem = new ComboBoxItem { Tag = TimeUnit.Hours }; SettingsDialog.Bind(hoursItem, ContentControl.ContentProperty, "小时");
        unit.Items.Add(minutesItem); unit.Items.Add(hoursItem); unit.SelectedValue = TimeUnit.Minutes;
        System.Windows.Automation.AutomationProperties.SetName(value, UiText.Get("本次暂缓时长"));
        System.Windows.Automation.AutomationProperties.SetName(unit, UiText.Get("时间单位"));
        var shortcuts = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        foreach (int minutes in new[] { 15, 30, 60 })
        {
            var button = SettingsDialog.Button(minutes == 60 ? "1 小时" : minutes == 30 ? "30 分钟" : "15 分钟", () => { value.Text = minutes.ToString(); unit.SelectedValue = TimeUnit.Minutes; });
            button.Margin = new Thickness(0, 0, 8, 0); shortcuts.Children.Add(button);
        }
        dialog.Body.Children.Add(shortcuts); dialog.Body.Children.Add(value); dialog.Body.Children.Add(unit);
        var joint = new CheckBox { Content = UiText.Get(scope == MonitorSettingsScope.Rest ? "同时暂缓隐私保护" : "同时暂缓屏幕休息"), IsEnabled = canJoint, Margin = new Thickness(0, 4, 0, 12) };
        dialog.Body.Children.Add(joint);
        if (!canJoint) dialog.Body.Children.Add(SettingsDialog.Text("另一项功能尚未开启，当前仅暂缓本项。"));
        var error = SettingsDialog.Text(""); dialog.Body.Children.Add(error);
        dialog.Action("取消", () => dialog.Close());
        var start = dialog.Action("开始暂缓", () => { apply(int.Parse(value.Text) * PrivacyOptions.Factor((TimeUnit)unit.SelectedValue), joint.IsChecked == true); dialog.Close(); }, true);
        void Validate()
        {
            start.IsEnabled = int.TryParse(value.Text, out var number) && unit.SelectedValue is TimeUnit selected && PrivacyOptions.ValidDuration(number, selected, 86400);
            error.Text = start.IsEnabled ? "" : UiText.Get("暂缓时长必须是 1 分钟至 24 小时的整数。");
        }
        value.TextChanged += (_, _) => Validate(); unit.SelectionChanged += (_, _) => Validate(); Validate();
        return dialog;
    }
}
