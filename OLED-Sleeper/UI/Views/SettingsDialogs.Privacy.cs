using Microsoft.Win32;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Models;
using OLED_Sleeper.UI.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OLED_Sleeper.UI.Views;

public static partial class SettingsDialogs
{
    public static void Images(Window owner, MainViewModel main, MonitorConfigurationViewModel config)
    {
        CreatePrivacyAppearanceDialog(owner, main, config).ShowDialog();
    }
    public static SettingsDialog CreatePrivacyAppearanceDialog(Window owner, MainViewModel main, MonitorConfigurationViewModel config)
    {
        var dialog = new SettingsDialog(owner, "隐私幕外观");
        var options = config.Options.Copy();
        var modes = new WrapPanel { Margin = new Thickness(0, 0, 0, 16) };
        var custom = new StackPanel();
        foreach (string key in new[] { "默认山林", "深绿纯色", "自定义图片" })
        {
            var choice = new RadioButton { GroupName = "CurtainBackground", Tag = key, Style = (Style)dialog.FindResource("ModeButton"), Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(10, 8, 10, 8) };
            SettingsDialog.Bind(choice, ContentControl.ContentProperty, key);
            choice.IsChecked = key == (options.UseImages ? "自定义图片" : options.UseBuiltInBackground ? "默认山林" : "深绿纯色");
            choice.Checked += (_, _) => { options.UseImages = key == "自定义图片"; options.UseBuiltInBackground = key == "默认山林"; custom.Visibility = options.UseImages ? Visibility.Visible : Visibility.Collapsed; };
            modes.Children.Add(choice);
        }
        custom.Visibility = options.UseImages ? Visibility.Visible : Visibility.Collapsed;
        dialog.Body.Children.Add(modes);
        dialog.Body.Children.Add(SettingsDialog.Text("仅修改本屏外观草稿，保存隐私设置后生效。"));
        dialog.Body.Children.Add(custom);
        var images = config.Images.ToList();
        var thumbnails = new WrapPanel();
        var status = SettingsDialog.Text("");
        var count = SettingsDialog.Text("");
        var fit = new CheckBox(); SettingsDialog.Bind(fit, ContentControl.ContentProperty, "完整显示图片（留边）"); fit.IsChecked = config.ContainImages;
        var slideshow = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        slideshow.Children.Add(SettingsDialog.Text("每张图片停留"));
        var timing = new StackPanel { Orientation = Orientation.Horizontal };
        var value = new TextBox { Text = config.SlideValueText, Width = 76 };
        System.Windows.Automation.AutomationProperties.SetName(value, UiText.Get("轮换间隔"));
        var unit = new ComboBox { SelectedValuePath = "Tag", Width = 112, Margin = new Thickness(8, 0, 0, 0) };
        foreach (var (tag, key) in new[] { (TimeUnit.Seconds, "秒"), (TimeUnit.Minutes, "分钟") })
        {
            var item = new ComboBoxItem { Tag = tag }; SettingsDialog.Bind(item, ContentControl.ContentProperty, key); unit.Items.Add(item);
        }
        unit.SelectedValue = config.SlideUnit;
        timing.Children.Add(value); timing.Children.Add(unit); slideshow.Children.Add(timing);
        void Refresh()
        {
            count.Text = string.Format(UiText.Get("已选 {0} 张"), images.Count);
            slideshow.Visibility = images.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            thumbnails.Children.Clear();
            if (images.Count == 0) thumbnails.Children.Add(SettingsDialog.Text("选择一张背景，或多张自动轮播。"));
            foreach (var name in images.ToArray())
            {
                var tile = new StackPanel { Width = 130, Margin = new Thickness(0, 0, 12, 12) };
                var image = new Image { Height = 80, Stretch = Stretch.UniformToFill };
                try { image.Source = main.Images?.Load(name); } catch { tile.Children.Add(SettingsDialog.Text("图片无法读取")); }
                tile.Children.Add(image);
                tile.Children.Add(SettingsDialog.Button("移除", () => { images.Remove(name); Refresh(); })); thumbnails.Children.Add(tile);
            }
        }
        bool ReadOptions()
        {
            options.Images = images.ToArray(); options.ContainImages = fit.IsChecked == true;
            if (unit.SelectedValue is TimeUnit selected && int.TryParse(value.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && PrivacyOptions.ValidDuration(number, selected, 3600))
            { options.SlideUnit = selected; options.SlideSeconds = number * PrivacyOptions.Factor(selected); }
            else if (options.UseImages && images.Count > 1) { status.Text = UiText.Get("图片轮换间隔必须是 1–3600 秒的整数。"); return false; }
            if (options.ValidateAppearance() is { } error) { status.Text = UiText.Get(error); return false; }
            return true;
        }
        custom.Children.Add(SettingsDialog.Text("最多 12 张，每张不超过 10 MB。"));
        custom.Children.Add(SettingsDialog.Button("添加图片", () =>
        {
            if (main.Images == null) return;
            var file = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.webp", Multiselect = true };
            if (file.ShowDialog(dialog) != true) return;
            if (images.Count + file.FileNames.Length > 12) { status.Text = UiText.Get("请选择 1–12 张有效的隐私幕图片。"); return; }
            try { var imported = file.FileNames.Select(main.Images.Import).ToArray(); images.AddRange(imported); status.Text = ""; Refresh(); }
            catch { status.Text = UiText.Get("图片导入失败。请检查格式、10 MB 大小限制及目录权限。WebP 需要系统解码器。"); }
        }));
        custom.Children.Add(count); custom.Children.Add(thumbnails); custom.Children.Add(fit); custom.Children.Add(slideshow); dialog.Body.Children.Add(status);
        dialog.Body.Children.Add(SettingsDialog.Button("预览隐私幕", () =>
        {
            if (!ReadOptions()) return;
            var curtain = new PrivacyCurtainWindow(); curtain.Configure(config.ScreenBounds, curtain.Dismiss, false);
            curtain.SetAppearance(main.Images, options); curtain.Present();
        }));
        dialog.Action("取消", () => dialog.Close());
        dialog.Action("应用为草稿", () =>
        {
            if (!ReadOptions()) return;
            config.ApplyPrivacyAppearance(options); dialog.Close();
        }, true);
        Refresh(); return dialog;
    }

    public static void TestApplication(Window owner, MainViewModel main, MonitorConfigurationViewModel config)
    {
        if (main.Applications == null) return;
        TestProtection(owner, main, config, true);
    }
    public static void TestDesktop(Window owner, MainViewModel main, MonitorConfigurationViewModel config)
    {
        if (main.Desktop == null) return;
        TestProtection(owner, main, config, false);
    }
    private static void TestProtection(Window owner, MainViewModel main, MonitorConfigurationViewModel config, bool application)
    {
        var dialog = new SettingsDialog(owner, application ? "测试切换" : "测试回到桌面");
        dialog.Body.Children.Add(SettingsDialog.Text(application
            ? "测试会立即遮挡本屏并隐藏本屏其他窗口，再切换到所选应用。不会保存草稿，也不会自动还原原窗口。"
            : "测试会立即遮挡本屏并最小化本屏窗口。不会保存草稿，也不会自动还原原窗口。"));
        var result = SettingsDialog.Text(""); dialog.Body.Children.Add(result);
        var start = dialog.Action("开始测试", () => { }, true);
        CancellationTokenSource? pending = null;
        PrivacyCurtainWindow? activeCurtain = null;
        dialog.Closed += (_, _) => { pending?.Cancel(); };
        dialog.Action("完成", () => dialog.Close());
        start.Click += async (_, _) =>
        {
            if (application && !config.HasApplication) { result.Text = UiText.Get("请选择本地应用程序（.exe）。"); return; }
            if (activeCurtain != null) return;
            start.IsEnabled = false;
            using var cancellation = new CancellationTokenSource(); pending = cancellation;
            var curtain = new PrivacyCurtainWindow(); activeCurtain = curtain;
            curtain.Configure(config.ScreenBounds, () => { pending?.Cancel(); curtain.Dismiss(); }, true);
            // Releasing the curtain cancels the request, but the service may still be unwinding.
            // Keep testing disabled until the outstanding operation has actually completed.
            curtain.Closed += (_, _) => { pending?.Cancel(); activeCurtain = null; start.IsEnabled = pending == null; };
            curtain.SetAppearance(main.Images, config.Options);
            try
            {
                curtain.Present();
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
                var ok = application
                    ? await main.Applications!.ShowAsync(config.Options.Copy(), config.ScreenBounds, cancellation.Token)
                    : await main.Desktop!.ShowDesktopAsync(config.ScreenBounds, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (ok) curtain.Dismiss(); else curtain.Failed();
                result.Text = UiText.Get(ok
                    ? application ? "已切换到指定应用，不会还原原窗口。" : "已回到桌面，不会自动还原原窗口。"
                    : application ? main.Applications!.FailureFor(config.ScreenBounds) ?? "无法安全切换到指定应用，已保留隐私幕。" : "无法安全回到桌面，已保留隐私幕。");
            }
            catch (OperationCanceledException) { result.Text = UiText.Get("测试已取消。"); }
            catch
            {
                if (!curtain.IsVisible)
                {
                    activeCurtain = null; curtain.Dismiss();
                    result.Text = UiText.Get("隐私幕无法显示，请使用 Windows 锁屏。");
                }
                else
                {
                    curtain.Failed(); result.Text = UiText.Get(application ? "无法安全切换到指定应用，已保留隐私幕。" : "无法安全回到桌面，已保留隐私幕。");
                }
            }
            finally { pending = null; start.IsEnabled = activeCurtain == null; }
        };
        dialog.ShowDialog();
    }
}
