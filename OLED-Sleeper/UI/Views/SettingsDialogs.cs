using Microsoft.Win32;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Infrastructure.Helpers;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Models;
using OLED_Sleeper.UI.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OLED_Sleeper.UI.Views;

public static partial class SettingsDialogs
{
    public static void Copy(Window owner, MainViewModel main, MonitorConfigurationViewModel source)
    {
        var dialog = new SettingsDialog(owner, "应用到其他屏幕");
        dialog.Body.Children.Add(SettingsDialog.Text("复制当前设置为目标屏幕的草稿，保存后生效。目标屏幕的启用状态保持不变。"));
        dialog.Body.Children.Add(SettingsDialog.Text("已有草稿将被覆盖；不支持调暗的屏幕将改为遮黑。同一应用窗口不能分配给多个启用屏幕。"));
        var targets = main.Monitors.Where(m => m.Configuration != source).ToArray();
        var checks = targets.Select(m => new CheckBox { Content = m.MonitorTitle, Tag = m, Margin = new Thickness(0, 5, 0, 5) }).ToArray();
        var all = new CheckBox { Content = UiText.Get("全部其他屏幕") };
        all.Click += (_, _) => { foreach (var check in checks) check.IsChecked = all.IsChecked; };
        dialog.Body.Children.Add(all);
        foreach (var check in checks) dialog.Body.Children.Add(check);
        if (checks.Length == 0) dialog.Body.Children.Add(SettingsDialog.Text("没有其他可用屏幕。"));
        dialog.Action("取消", () => dialog.Close());
        var apply = dialog.Action("应用为草稿", () =>
        {
            foreach (var check in checks.Where(c => c.IsChecked == true)) source.CopyTo(((MonitorLayoutViewModel)check.Tag).Configuration, main.ScreenScope);
            dialog.Close();
        }, true);
        apply.IsEnabled = false;
        foreach (var check in checks) check.Checked += (_, _) => apply.IsEnabled = true;
        foreach (var check in checks) check.Unchecked += (_, _) => apply.IsEnabled = checks.Any(c => c.IsChecked == true);
        dialog.ShowDialog();
    }
    public static void Pause(Window owner, PrivacyOptions options, Action<int> pause)
    {
        var dialog = new SettingsDialog(owner, "暂缓自动保护");
        dialog.Body.Children.Add(SettingsDialog.Text("只暂缓自动计时，不会解除已经开启的隐私幕。"));
        dialog.Body.Children.Add(SettingsDialog.Text("仅用于本次，不修改默认时长。屏幕休息继续运行。"));
        var value = new TextBox { Text = options.PauseValue.ToString(), Width = 100, HorizontalAlignment = HorizontalAlignment.Left };
        var unit = new ComboBox { Width = 130, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 12), SelectedValuePath = "Tag" };
        unit.Items.Add(new ComboBoxItem { Content = UiText.Get("分钟"), Tag = TimeUnit.Minutes });
        unit.Items.Add(new ComboBoxItem { Content = UiText.Get("小时"), Tag = TimeUnit.Hours }); unit.SelectedValue = options.PauseUnit;
        dialog.Body.Children.Add(value); dialog.Body.Children.Add(unit);
        var error = SettingsDialog.Text(""); dialog.Body.Children.Add(error);
        dialog.Action("取消", () => dialog.Close());
        dialog.Action("开始暂缓", () =>
        {
            if (!int.TryParse(value.Text, out var number) || unit.SelectedValue is not TimeUnit selected || !PrivacyOptions.ValidDuration(number, selected, 86400))
            { error.Text = UiText.Get("暂缓时长必须是 1 分钟至 24 小时的整数。"); return; }
            pause(number * PrivacyOptions.Factor(selected)); dialog.Close();
        }, true); dialog.ShowDialog();
    }
    public static void Application(Window owner, MainViewModel main, MonitorConfigurationViewModel config)
    {
        if (main.ApplicationWindows == null) return;
        var dialog = new SettingsDialog(owner, "选择应用");
        dialog.Body.Children.Add(SettingsDialog.Text("选择一个正在运行的窗口，或选择本地程序。重启后遇到多个匹配窗口时将保留隐私幕。"));
        var search = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
        System.Windows.Automation.AutomationProperties.SetName(search, UiText.Get("搜索应用或窗口"));
        var list = new ListBox { Height = 180, Background = (Brush)dialog.FindResource("PrimaryBackgroundColor"), Foreground = dialog.Foreground, DisplayMemberPath = "Title" };
        var path = new TextBox { Margin = new Thickness(0, 12, 0, 8), Text = config.ApplicationPath };
        var status = SettingsDialog.Text("");
        IReadOnlyList<ApplicationWindow> windows = [];
        void Refresh()
        {
            try { windows = main.ApplicationWindows.Enumerate(true); list.ItemsSource = windows.Where(w => (w.Title + w.Path).Contains(search.Text, StringComparison.CurrentCultureIgnoreCase)).Select(w => w with { Title = string.IsNullOrWhiteSpace(w.Title) ? Path.GetFileName(w.Path) : w.Title }).ToArray(); }
            catch { status.Text = UiText.Get("无法读取窗口列表，请重试或选择本地程序。"); }
        }
        search.TextChanged += (_, _) => Refresh();
        list.SelectionChanged += (_, _) => { if (list.SelectedItem is ApplicationWindow selected) path.Text = selected.Path; };
        dialog.Body.Children.Add(SettingsDialog.Text("搜索应用或窗口"));
        dialog.Body.Children.Add(search); dialog.Body.Children.Add(list);
        dialog.Body.Children.Add(SettingsDialog.Button("刷新窗口", Refresh));
        dialog.Body.Children.Add(path);
        dialog.Body.Children.Add(SettingsDialog.Button("浏览本地程序", () =>
        {
            var file = new OpenFileDialog { Filter = "Application (*.exe)|*.exe", CheckFileExists = true };
            if (file.ShowDialog(dialog) == true) { list.SelectedItem = null; path.Text = file.FileName; }
        }));
        dialog.Body.Children.Add(status);
        dialog.Action("取消", () => dialog.Close());
        dialog.Action("选择应用", () =>
        {
            if (!PrivacyOptions.IsLocalExecutable(path.Text) || !File.Exists(path.Text)) { status.Text = UiText.Get("请选择本地应用程序（.exe）。"); return; }
            var selected = list.SelectedItem as ApplicationWindow;
            if (!string.Equals(path.Text, selected?.Path, StringComparison.OrdinalIgnoreCase)) selected = null;
            config.SetApplication(path.Text, selected?.ClassName ?? "", selected?.Token, selected?.Title);
            dialog.Close();
        }, true);
        Refresh(); dialog.ShowDialog();
    }
    public static void Preview(Window owner, MainViewModel main, MonitorConfigurationViewModel config)
    {
        var curtain = new PrivacyCurtainWindow();
        curtain.Configure(config.ScreenBounds, () => curtain.Dismiss(), false);
        curtain.SetAppearance(main.Images, config.Options); curtain.Present();
    }
    public static void General(Window owner, MainViewModel main)
    {
        var dialog = new SettingsDialog(owner, "通用设置");
        dialog.Body.Children.Add(new ThemePickerView { DataContext = main });
        dialog.Body.Children.Add(SettingsDialog.Text("语言 / Language"));
        var language = new ComboBox { SelectedValuePath = "Tag", Width = 180, HorizontalAlignment = HorizontalAlignment.Left };
        language.Items.Add(new ComboBoxItem { Content = "简体中文", Tag = "zh-CN" }); language.Items.Add(new ComboBoxItem { Content = "English", Tag = "en" });
        language.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, new Binding(nameof(main.Language)) { Source = main, Mode = BindingMode.TwoWay });
        dialog.Body.Children.Add(language);
        var startup = new CheckBox { Content = UiText.Get("开机启动"), Margin = new Thickness(0, 20, 0, 12) };
        try { startup.IsChecked = StartupHelper.IsRunAtStartupEnabled(); } catch { startup.IsEnabled = false; }
        var status = SettingsDialog.Text("");
        startup.Click += (_, _) =>
        {
            try { StartupHelper.SetRunAtStartup(startup.IsChecked == true); startup.IsChecked = StartupHelper.IsRunAtStartupEnabled(); }
            catch { status.Text = UiText.Get("启动项未能保存，请检查权限。"); }
        };
        SettingsDialog.Bind(startup, ContentControl.ContentProperty, "开机启动");
        dialog.Body.Children.Add(startup); dialog.Body.Children.Add(status);
        dialog.Body.Children.Add(SettingsDialog.Text("关闭窗口后仍在托盘运行。"));
        dialog.Action("完成", () => dialog.Close(), true); dialog.ShowDialog();
    }
}
