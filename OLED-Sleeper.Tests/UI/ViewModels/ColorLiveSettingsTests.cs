using Moq;
using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Storage;
using OLED_Sleeper.Tests.TestDoubles;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.ViewModels;
using OLED_Sleeper.UI.Views;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OLED_Sleeper.Tests.UI.ViewModels;

[Collection("UI language")]
public sealed class ColorLiveSettingsTests
{
    [WpfFact]
    public void ResumeObservationKeepsVerifiedScreenAndDraftWhileExplainingUnavailablePreview()
    {
        var language = UiText.Current.Language;
        try
        {
            var fixture = new Fixture(); var model = fixture.Model;
            model.Enabled = true; model.DayUsesOriginal = false; model.DayKelvin = "5100";
            const string waiting = "正在等待屏幕连接和原色读数稳定。";
            foreach (var lang in new[] { "zh-CN", "en" })
            {
                UiText.Current.SetLanguage(lang);
                model.UpdateCapabilities([new(Fixture.Scope("one"), ColorCapabilityState.Supported, "fixture")],
                    [new("one", ColorControlState.Detecting, waiting)], "");
                var screen = Assert.Single(model.VerifiedScreens);
                Assert.Same(screen, model.SelectedScreen);
                Assert.False(screen.NeedsRecovery);
                Assert.False(screen.NeedsValidation);
                Assert.False(screen.CanPreview);
                Assert.False(model.CanLivePreview);
                Assert.Equal(lang == "en" ? "Waiting for the display connection and original-color readings to stabilize." : waiting,
                    screen.Status);
                Assert.Equal(screen.Status, screen.PreviewUnavailableReason);
                Assert.Equal("5100", model.DayKelvin);
                Assert.True(model.IsDirty);
            }
            model.UpdateCapabilities([new(Fixture.Scope("one"), ColorCapabilityState.Supported, "fixture")],
                [new("one", ColorControlState.Warm, "已写入经过验证的色温策略。")], "");
            Assert.True(model.CanLivePreview);
            Assert.Empty(fixture.Calls);
            Assert.Empty(fixture.Applied);
        }
        finally { UiText.Current.SetLanguage(language); }
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConflictSelectionSettlesBeforeSynchronousPreviewCancellationRefresh(bool hasAlternative)
    {
        ColorSettingsViewModel? model = null;
        ColorCapability[] capabilities = [new(Fixture.Scope("one"), ColorCapabilityState.Supported, "fixture"),
            new(Fixture.Scope("two"), hasAlternative ? ColorCapabilityState.Supported : ColorCapabilityState.Unverified, "fixture")];
        ColorScreenState[] states = [new("one", ColorControlState.Conflict, "external change")];
        var observations = new List<(string? Selected, string? Preview)>();
        bool replayConflict = false;
        var fixture = new Fixture(cancelPreview: () =>
        {
            if (model != null && replayConflict)
            {
                observations.Add((model.SelectedScreen?.HardwareId, model.PreviewScreen?.HardwareId));
                // Bound the old recursive path so this regression fails without crashing the test host.
                if (observations.Count < 8) model.UpdateCapabilities(capabilities, states, "conflict");
            }
            return Task.CompletedTask;
        });
        model = fixture.Model;
        model.DayUsesOriginal = false; model.DayKelvin = "5300";
        replayConflict = true;
        model.UpdateCapabilities(capabilities, states, "conflict");

        string? expected = hasAlternative ? "two" : null;
        Assert.Equal((expected, expected), Assert.Single(observations));
        Assert.Equal(expected, model.SelectedScreen?.HardwareId);
        Assert.Equal(expected, model.PreviewScreen?.HardwareId);
        Assert.True(model.Screens[0].NeedsRecovery);
        Assert.True(model.IsDirty);
        Assert.Empty(fixture.Calls);
        Assert.Empty(fixture.Applied);
        replayConflict = false;
        model.UpdateCapabilities([new(Fixture.Scope("one"), ColorCapabilityState.Supported, "fixture"),
            new(Fixture.Scope("two"), ColorCapabilityState.Supported, "fixture")], [], "");
        model.SelectedScreen = model.Screens[0];
        Assert.Equal("5300", model.DayKelvin);
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void TargetChangeIsVisibleBeforeCancellationCallback(bool changeScheduleScreen)
    {
        ColorSettingsViewModel? model = null;
        var observations = new List<(string? Selected, string? Preview)>();
        var fixture = new Fixture(cancelPreview: () =>
        {
            if (model != null) observations.Add((model.SelectedScreen?.HardwareId, model.PreviewScreen?.HardwareId));
            return Task.CompletedTask;
        });
        model = fixture.Model;
        if (changeScheduleScreen) model.SelectedScreen = model.Screens[1];
        else model.PreviewScreen = model.Screens[1];

        Assert.Equal((changeScheduleScreen ? "two" : "one", "two"), Assert.Single(observations));
        Assert.Empty(fixture.Calls);
        model.LeavePage();
    }

    [WpfFact]
    public void DuplicateModelNamesUseAllConnectedScreensAndPreserveDraftIdentity()
    {
        var f = new Fixture(); var vm = f.Model;
        var first = Fixture.Scope("one") with { DisplayName = "Q27P10", DeviceName = @"\\.\DISPLAY1" };
        var second = Fixture.Scope("two") with { DisplayName = "Q27P10", DeviceName = @"\\.\DISPLAY2" };
        ColorCapability[] capabilities = [new(first, ColorCapabilityState.Supported, ""), new(second, ColorCapabilityState.Unverified, "")];
        vm.DayUsesOriginal = false; vm.DayKelvin = "5100";
        vm.UpdateCapabilities(capabilities, [], "");
        Assert.Single(vm.VerifiedScreens);
        Assert.Equal("Q27P10 · 1", vm.SelectedScreen!.Name);
        Assert.Equal("Q27P10 · 2", vm.Screens.Single(s => s.HardwareId == "two").Name);
        vm.UpdateCapabilities(capabilities.Reverse().ToArray(), [], "");
        Assert.Equal("Q27P10 · 1", vm.SelectedScreen.Name);
        Assert.Equal("5100", vm.DayKelvin);
        Assert.True(vm.IsDirty);
        Assert.Empty(f.Applied);
        Assert.Empty(f.Calls);
    }

    [WpfFact]
    public void OriginalTargetsCanSaveWithHiddenInvalidInputAndKeepLastValidCandidates()
    {
        var f = new Fixture(); var vm = f.Model;
        vm.DayUsesOriginal = false; vm.DayKelvin = "5100"; vm.NightKelvin = "4200";
        vm.DayKelvin = "bad"; vm.NightKelvin = "1";
        Assert.NotNull(vm.Error); Assert.False(vm.TrySave());
        vm.DayUsesOriginal = true; vm.NightUsesOriginal = true;
        Assert.Null(vm.Error);
        f.Fs.WritesFail = true; Assert.False(vm.TrySave()); Assert.Equal("bad", vm.DayKelvin); Assert.True(vm.IsDirty);
        f.Fs.WritesFail = false; Assert.True(vm.TrySave()); Assert.False(vm.IsDirty);
        vm.DayUsesOriginal = false; vm.NightUsesOriginal = false;
        Assert.Equal("5100", vm.DayKelvin); Assert.Equal("4200", vm.NightKelvin);
        Assert.Equal(4200, f.Store.Load().ForScreen("one").NightKelvin);
        vm.NightKelvin = "invalid"; vm.NightUsesOriginal = true; vm.Mode = "Manual";
        Assert.NotNull(vm.Error); Assert.False(vm.TrySave());
    }

    [WpfFact]
    public void PerScreenDraftsSaveTogetherAndFailureKeepsBothIndependent()
    {
        var f = new Fixture(); var vm = f.Model;
        Assert.False(vm.Enabled);
        vm.Enabled = true; vm.DayUsesOriginal = false; vm.DayKelvin = "3900"; vm.NightKelvin = "4200";
        vm.SelectedScreen = vm.Screens[1];
        Assert.False(vm.Enabled); Assert.Equal("5300", vm.DayKelvin);
        vm.Enabled = true; vm.MorningStart = "09:30"; vm.NightKelvin = "4800";
        Assert.Equal(2, vm.DirtyScreenCount);
        f.Fs.WritesFail = true; Assert.False(vm.TrySave()); Assert.Equal(2, vm.DirtyScreenCount);
        f.Fs.WritesFail = false; Assert.True(vm.TrySave());
        var saved = f.Store.Load(); Assert.Equal(2, saved.Schema);
        Assert.Equal(3900, saved.ForScreen("one").DayKelvin);
        Assert.Equal(4200, saved.ForScreen("one").NightKelvin);
        Assert.Equal("09:30", saved.ForScreen("two").MorningStart);
        Assert.Equal(4800, saved.ForScreen("two").NightKelvin);
        vm.SelectedScreen = vm.Screens[0]; vm.DayKelvin = "3";
        vm.SelectedScreen = vm.Screens[1]; Assert.Equal("4800", vm.NightKelvin);
        vm.SelectedScreen = vm.Screens[0]; Assert.Equal("3", vm.DayKelvin); Assert.False(vm.TrySave());
        vm.Discard(); Assert.Equal("3900", vm.DayKelvin); Assert.False(vm.IsDirty);
    }

    [WpfTheory]
    [InlineData("07:15", "18:45", 435, 690, 315, true)]
    [InlineData("18:45", "07:15", 435, 690, 315, false)]
    [InlineData("00:00", "00:05", 0, 5, 1435, true)]
    public void TimelineUsesExactPeriodBoundariesEvenWithIdenticalTargets(string morning, string evening, double first, double middle, double last, bool startsAtRest)
    {
        var vm = new Fixture().Model;
        vm.Enabled = true; vm.DayUsesOriginal = false; vm.DayKelvin = "3900"; vm.NightKelvin = "3900";
        vm.TransitionMinutes = "0"; vm.MorningStart = morning; vm.EveningStart = evening;
        Assert.Equal(new[] { first, middle, last }, vm.SchedulePeriods.Select(p => p.Minutes));
        Assert.Equal(new[] { startsAtRest, !startsAtRest, startsAtRest }, vm.SchedulePeriods.Select(p => p.IsRest));
        Assert.Equal(1440, vm.SchedulePeriods.Sum(p => p.Minutes));
        Assert.Contains(morning + " → " + evening, vm.WorkScheduleLabel);
        Assert.Contains("3900 K", vm.WorkScheduleLabel); Assert.Contains("3900 K", vm.RestScheduleLabel);
        vm.DayUsesOriginal = true; Assert.Contains(UiText.Get("原色"), vm.WorkScheduleLabel);
        vm.MorningStart = evening; Assert.False(vm.HasValidSchedule); Assert.NotEmpty(vm.ScheduleInputHint);
        vm.MorningStart = "bad"; Assert.Empty(vm.SchedulePeriods); Assert.NotNull(vm.Error);
        vm.Discard(); Assert.True(vm.HasValidSchedule); Assert.Equal("07:00", vm.MorningStart);
    }

    [WpfFact]
    public async Task TimelineAndParticipationFeedbackRenderInBothThemesAndLanguages()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        string language = UiText.Current.Language;
        try
        {
            foreach (string theme in new[] { "forest", "fog" })
                foreach (string lang in new[] { "zh-CN", "en" })
                    foreach (int width in new[] { 460, 720 })
                    {
                        UiText.Current.SetLanguage(lang);
                        var f = new Fixture(); var vm = f.Model;
                        vm.Enabled = true; vm.DayUsesOriginal = false; vm.DayKelvin = "3900"; vm.NightKelvin = "3900";
                        vm.MorningStart = "07:15"; vm.EveningStart = "18:45";
                        var view = new ColorSettingsView { DataContext = vm, Width = width, Height = 650 };
                        var palette = OLED_Sleeper.UI.Themes.UiTheme.Palette(theme);
                        foreach (var key in palette.Keys) view.Resources[key] = palette[key];
                        view.SetResourceReference(Control.BackgroundProperty, "PrimaryBackgroundColor");
                        view.SetResourceReference(Control.ForegroundProperty, "PrimaryForegroundColor");
                        var size = new Size(width, 650);
                        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                        view.Measure(size); view.Arrange(new Rect(size)); view.UpdateLayout();
                        Assert.DoesNotContain(Descendants<TextBlock>(view), t => t.Text.Contains("OLED_Sleeper."));
                        var picker = Descendants<ComboBox>(view).Single(c => c.ItemsSource == vm.VerifiedScreens);
                        if (width == 460) Assert.Contains(Descendants<TextBlock>(picker), t => t.Text == vm.SelectedScreen!.SelectionLabel);
                        Assert.True(Descendants<ScrollViewer>(view).First().ExtentWidth <= Descendants<ScrollViewer>(view).First().ViewportWidth + 1);
                        Render(view, size, $"color-participation-{theme}-{lang}-{width}");
                        var scroll = Descendants<ScrollViewer>(view).First(); scroll.ScrollToBottom(); view.UpdateLayout();
                        var timeline = (Grid)view.FindName("ScheduleTimeline");
                        Assert.True(timeline.ActualWidth > 0);
                        var periods = timeline.Children.OfType<Border>().ToArray();
                        Assert.Equal(3, periods.Length);
                        Assert.NotEqual(((SolidColorBrush)periods[0].Background).Color, ((SolidColorBrush)periods[1].Background).Color);
                        Assert.Equal(timeline.ActualWidth * 435 / 1440, periods[0].ActualWidth, 1);
                        Assert.Contains(Descendants<TextBlock>(view), t => t.Text == vm.WorkScheduleLabel);
                        Render(view, size, $"color-timeline-{theme}-{lang}-{width}");
                        vm.MorningStart = "09:30";
                        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                        view.UpdateLayout();
                        Assert.Equal(timeline.ActualWidth * 570 / 1440, periods[0].ActualWidth, 1);
                        vm.LeavePage();
                    }
        }
        finally { UiText.Current.SetLanguage(language); }
    }

    [WpfFact]
    public async Task LeavingScheduleTabCancelsPendingPreviewButKeepsDraftAndSelection()
    {
        var f = new Fixture();
        f.Model.PreviewScreen = f.Model.Screens[0];
        f.Model.Enabled = true; f.Model.PreviewRestCommand.Execute(null); f.Model.NightKelvin = "4100";
        f.Model.SelectedPage = 1;
        await Task.Delay(250);
        Assert.Empty(f.Calls);
        Assert.True(f.Model.IsDirty);
        Assert.Equal("4100", f.Model.NightKelvin);
        Assert.Same(f.Model.Screens[0], f.Model.PreviewScreen);
        Assert.False(f.Model.CanCompareOriginal);
        f.Model.SelectedPage = 0;
        await Task.Delay(200);
        Assert.Empty(f.Calls);
    }

    [WpfFact]
    public async Task EditingWithoutTargetDoesNotPreviewAndRapidInputsAreCoalesced()
    {
        var fixture = new Fixture(); var vm = fixture.Model;
        vm.NightKelvin = "5000";
        await Task.Delay(200); Assert.Empty(fixture.Calls);
        vm.Enabled = true; vm.PreviewRestCommand.Execute(null);
        vm.NightKelvin = "5100"; vm.NightKelvin = "5200"; vm.Enabled = true; vm.NightKelvin = "5300"; vm.PreviewRestCommand.Execute(null);
        await Until(() => fixture.Calls.Count > 0 && vm.CanCompareOriginal);
        Assert.Equal(("one", (int?)5300), Assert.Single(fixture.Calls));
        Assert.True(vm.IsDirty); Assert.Equal(4500, fixture.Store.Load().NightKelvin);
        await vm.EndPreviewAsync();
    }

    [WpfFact]
    public async Task HeldOriginalReturnsToEditedValueAndLateReleaseCannotRestartAfterLeaving()
    {
        var fixture = new Fixture(); var vm = fixture.Model; vm.PreviewScreen = vm.Screens[0];
        vm.Enabled = true; vm.DayUsesOriginal = false; vm.DayKelvin = "5700"; vm.PreviewWorkCommand.Execute(null);
        await Until(() => vm.CanCompareOriginal);
        vm.BeginOriginalComparison(); await Until(() => fixture.Calls.Count == 2);
        Assert.Null(fixture.Calls[1].Kelvin);
        vm.EndOriginalComparison(); await Until(() => fixture.Calls.Count == 3);
        Assert.Equal(5700, fixture.Calls[2].Kelvin);
        vm.BeginOriginalComparison(); await Until(() => fixture.Calls.Count == 4);
        vm.LeavePage(); vm.EndOriginalComparison();
        await Task.Delay(250); Assert.Equal(4, fixture.Calls.Count); Assert.False(vm.CanCompareOriginal);
    }

    [WpfFact]
    public async Task SwitchingTargetCancelsPendingAndInFlightWorkWithoutSendingAStaleValue()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken firstToken = default;
        var fixture = new Fixture(async (_, _, token) => { firstToken = token; entered.TrySetResult(); await release.Task; return true; });
        var vm = fixture.Model; vm.Enabled = true; vm.NightKelvin = "5000"; vm.PreviewRestCommand.Execute(null);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        vm.NightKelvin = "5500"; vm.SelectedScreen = vm.Screens[1];
        Assert.True(firstToken.IsCancellationRequested);
        release.SetResult(); await Task.Delay(250);
        Assert.Single(fixture.Calls); Assert.False(vm.CanCompareOriginal);
        vm.NightKelvin = "5600"; vm.LeavePage();
        await Task.Delay(250); Assert.Single(fixture.Calls);
    }

    [WpfFact]
    public async Task BothTargetsSaveTogetherAndFailedSaveOrInvalidInputKeepDrafts()
    {
        var fixture = new Fixture(); var vm = fixture.Model; vm.PreviewScreen = vm.Screens[0];
        vm.DayUsesOriginal = false; vm.DayKelvin = "5800"; vm.NightKelvin = "4200";
        fixture.Fs.WritesFail = true;
        Assert.False(vm.TrySave()); Assert.True(vm.IsDirty);
        await Task.Delay(200); Assert.Empty(fixture.Calls);
        fixture.Fs.WritesFail = false;
        Assert.True(vm.TrySave());
        var saved = fixture.Store.Load().ForScreen("one"); Assert.Equal(5800, saved.DayKelvin); Assert.Equal(4200, saved.NightKelvin); Assert.False(saved.DayUsesOriginal);
        vm.DayKelvin = "4900"; vm.DayUsesOriginal = true; vm.NightKelvin = "4000"; vm.Discard();
        Assert.Equal("5800", vm.DayKelvin); Assert.Equal("4200", vm.NightKelvin); Assert.False(vm.DayUsesOriginal); Assert.False(vm.IsDirty);
        vm.DayKelvin = "abc"; Assert.NotNull(vm.Error); Assert.False(vm.TrySave()); Assert.True(vm.IsDirty);
        vm.Discard(); await vm.EndPreviewAsync();
    }

    [WpfFact]
    public async Task UnverifiedDisplayCannotPreviewAndFailureDoesNotClaimSuccess()
    {
        var fixture = new Fixture((_, _, _) => Task.FromResult(false)); var vm = fixture.Model;
        vm.UpdateCapabilities([new(Fixture.Scope("one"), ColorCapabilityState.Unverified, "fixture")], [], "");
        vm.PreviewScreen = vm.Screens[0]; vm.NightKelvin = "5000";
        await Task.Delay(200); Assert.Empty(fixture.Calls); Assert.False(vm.CanLivePreview);
        vm.UpdateCapabilities([new(Fixture.Scope("one"), ColorCapabilityState.Supported, "fixture")], [], "");
        vm.Enabled = true; vm.NightKelvin = "5300"; vm.PreviewRestCommand.Execute(null);
        await Until(() => vm.LivePreviewMessage == UiText.Get("试调未生效，请检查所选屏幕状态。"));
        Assert.False(vm.CanCompareOriginal); await vm.EndPreviewAsync();
    }

    [WpfFact]
    public async Task SelectingBlockedDisplaysShowsReasonsAndKeepsVerificationEntryReachable()
    {
        var fixture = new Fixture(); var vm = fixture.Model;
        var unverified = new ColorCapability(Fixture.Scope("one"), ColorCapabilityState.Unverified, "尚未完成此设备的实机调色与恢复验证。", false, false, true);
        vm.UpdateCapabilities([unverified], [], "", validationBlockReason: "请先关闭色温调节并保存，再进行验证。");
        vm.PreviewScreen = vm.Screens[0];
        Assert.False(vm.CanLivePreview); Assert.Contains("关闭色温", vm.PreviewScreenStatus);
        Assert.False(vm.PreviewScreen.CanValidate); Assert.True(vm.PreviewScreen.ValidateCommand.CanExecute(null));
        vm.PreviewScreen.ValidateCommand.Execute(null); Assert.Equal(1, fixture.ValidationCalls);
        vm.NightKelvin = "5100"; await Task.Delay(200); Assert.Empty(fixture.Calls);
        vm.UpdateCapabilities([unverified], [new("one", ColorControlState.RecoveryPending, "存在外部调色或未确认的恢复记录。")], "");
        Assert.Contains("重新检查原色", vm.PreviewScreenStatus);
        Assert.True(vm.PreviewScreen.RecheckOriginalCommand.CanExecute(null));
        vm.PreviewScreen.RecheckOriginalCommand.Execute(null); Assert.Equal(1, fixture.RecheckCalls);
        Assert.True(vm.ResetBaselineCommand.CanExecute(null)); vm.ResetBaselineCommand.Execute(null); Assert.Equal(1, fixture.ResetCalls);
        vm.UpdateCapabilities([unverified with { State = ColorCapabilityState.Supported }], [new("one", ColorControlState.Paused, "已临时恢复原色。")], "");
        Assert.Contains("恢复已保存策略", vm.PreviewScreenStatus); Assert.False(vm.CanLivePreview);
        vm.UpdateCapabilities([unverified with { State = ColorCapabilityState.Supported }], [], "");
        vm.Enabled = true; Assert.True(vm.CanLivePreview); await vm.EndPreviewAsync();
        Assert.False(vm.ResetBaselineCommand.CanExecute(null));
    }

    [WpfFact]
    public async Task BaselineResetRequiresExplicitObservationAndRendersTheAffectedDisplays()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        string language = UiText.Current.Language;
        try
        {
            foreach (string lang in new[] { "zh-CN", "en" })
            {
                UiText.Current.SetLanguage(lang);
                var scopes = new[] { Fixture.Scope("one"), Fixture.Scope("two") };
                var linear = new ColorValue { Channels = Enumerable.Range(0, 768).Select(i => (ushort)(i % 256 * 257)).ToArray() };
                var document = new ColorRecoveryDocument
                {
                    Records = scopes.Select(s => new ColorRecoveryRecord
                    { Scope = s, Baseline = GammaTarget.Create(linear, 5000, 1), LastApplied = GammaTarget.Create(linear, 4000, 1), Conflicted = true }).ToList()
                };
                var backend = new Mock<IColorTrialBackend>();
                var currentScopes = scopes.Select(s => s with { ModeFingerprint = "reconnected-sdr" }).ToArray();
                backend.Setup(b => b.DiscoverAsync(It.IsAny<CancellationToken>())).ReturnsAsync(currentScopes.Select(s => new ColorCapability(s, ColorCapabilityState.Unverified, "fixture", true, false, true)).ToArray());
                backend.Setup(b => b.ReadAsync(It.IsAny<ColorDisplayScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => linear.Copy());
                var store = new Mock<IColorRecoveryStore>(); store.Setup(s => s.Read()).Returns(() => new ColorRecoveryReadResult(document.Copy(), true));
                store.Setup(s => s.TryArchive(It.IsAny<ColorRecoveryArchive>())).Returns(true);
                store.Setup(s => s.TryWrite(It.IsAny<ColorRecoveryDocument>())).Callback<ColorRecoveryDocument>(value => document = value.Copy()).Returns(true);
                var registry = new Mock<IColorValidationRegistry>(); registry.SetupGet(r => r.IsAvailable).Returns(true);
                registry.Setup(r => r.TryRevoke(It.IsAny<IReadOnlyCollection<string>>())).Returns(true);
                var service = new ColorTemperatureService(backend.Object, store.Object);
                var dialog = new ColorBaselineResetWindow(service, registry.Object);
                var body = Assert.IsAssignableFrom<FrameworkElement>(dialog.Content);
                body.Measure(new Size(570, 1000)); body.Arrange(new Rect(0, 0, 570, body.DesiredSize.Height)); body.UpdateLayout();
                var ready = Descendants<CheckBox>(body).Single();
                var confirm = Descendants<Button>(body).Single(b => Equals(b.Content, UiText.Get("备份旧记录并重新建立")));
                Assert.False(confirm.IsEnabled);
                dialog.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var retry = Descendants<Button>(body).Single(b => Equals(b.Content, UiText.Get("重新检查")));
                await Until(() => retry.IsEnabled);
                Assert.Equal(Visibility.Collapsed, ready.Visibility);
                Assert.Equal(Visibility.Collapsed, confirm.Visibility);
                Assert.Contains(Descendants<TextBlock>(body), t => t.Text == UiText.Get("无法唯一确认待处理的物理屏幕，或当前不是可读取颜色的 SDR 扩展模式。请检查连接与显示设置后重试。"));
                RenderRecoveryDialog(dialog, "color-baseline-blocked-" + lang);

                var validation = new ColorValidationWindow(service, registry.Object, "one", "Example one");
                var validationBody = (FrameworkElement)validation.Content;
                validationBody.Measure(new Size(540, 900)); validationBody.Arrange(new Rect(0, 0, 540, validationBody.DesiredSize.Height)); validationBody.UpdateLayout();
                Assert.Contains(Descendants<TextBlock>(validationBody), t => t.Text.Contains("Example two", StringComparison.Ordinal));
                Assert.Contains(Descendants<Button>(validationBody), b => Equals(b.Content, UiText.Get("处理恢复记录")) && b.IsEnabled);
                RenderRecoveryDialog(validation, "color-validation-recovery-" + lang);
                validation.Close();

                backend.Setup(b => b.DiscoverAsync(It.IsAny<CancellationToken>())).ReturnsAsync(currentScopes.Select(s => new ColorCapability(s, ColorCapabilityState.Unverified, "fixture", false, false, true)).ToArray());
                retry.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Until(() => ready.IsEnabled);
                Assert.False(confirm.IsEnabled); store.Verify(s => s.TryArchive(It.IsAny<ColorRecoveryArchive>()), Times.Never);
                foreach (var scope in scopes) Assert.Contains(Descendants<TextBlock>(body), t => t.Text == "• " + scope.DisplayName);
                dialog.Content = null;
                ((ScrollViewer)body).Foreground = (Brush)Application.Current.FindResource("PrimaryForegroundColor");
                var surface = new Border { Background = (Brush)Application.Current.FindResource("SecondaryBackgroundColor"), Child = body };
                surface.Measure(new Size(570, 1000)); var size = new Size(570, surface.DesiredSize.Height); surface.Arrange(new Rect(size)); surface.UpdateLayout();
                Render(surface, size, $"color-baseline-reset-{lang}");
                ready.IsChecked = true; Assert.True(confirm.IsEnabled);
                confirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Until(() => confirm.Visibility == Visibility.Collapsed);
                Assert.Empty(document.Records); Assert.Equal(2, document.ValidationRequiredScreens.Count);
                store.Verify(s => s.TryArchive(It.IsAny<ColorRecoveryArchive>()), Times.Once);
                backend.Verify(b => b.WriteAsync(It.IsAny<ColorDisplayScope>(), It.IsAny<ColorValue>(), It.IsAny<CancellationToken>()), Times.Never);
                backend.Verify(b => b.RestoreAsync(It.IsAny<ColorDisplayScope>(), It.IsAny<ColorValue>(), It.IsAny<CancellationToken>()), Times.Never);
                dialog.Close(); await service.StopAsync();
            }
        }
        finally { UiText.Current.SetLanguage(language); }
    }

    [WpfFact]
    public async Task BlockedPickerAndValidationRequirementsRenderInBothLanguages()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        string language = UiText.Current.Language;
        try
        {
            foreach (string lang in new[] { "zh-CN", "en" })
            {
                UiText.Current.SetLanguage(lang);
                var fixture = new Fixture(); var vm = fixture.Model;
                var capability = new ColorCapability(Fixture.Scope("one"), ColorCapabilityState.Unverified, "尚未完成此设备的实机调色与恢复验证。", false, false, true);
                vm.UpdateCapabilities([capability, capability with { Scope = Fixture.Scope("two"), State = ColorCapabilityState.Supported }],
                    [new("one", ColorControlState.RecoveryPending, "存在外部调色或未确认的恢复记录。"), new("two", ColorControlState.Conflict, "检测到外部调色，已停止竞争。请检查当前颜色。")], "请先关闭色温调节并保存，再进行验证。",
                    validationBlockReason: "请先关闭色温调节并保存，再进行验证。");
                var view = new ColorSettingsView { DataContext = vm, Width = 460, Height = 650, Background = (Brush)Application.Current.FindResource("PrimaryBackgroundColor"), Foreground = (Brush)Application.Current.FindResource("PrimaryForegroundColor") };
                var size = new Size(460, 650); view.Measure(size); view.Arrange(new Rect(size)); view.UpdateLayout();
                Assert.Empty(vm.VerifiedScreens); Assert.True(vm.HasNoVerifiedScreens);
                vm.SelectedPage = 1; view.UpdateLayout();
                foreach (var screen in vm.Screens)
                {
                    Assert.Contains(Descendants<TextBlock>(view), t => t.Text == screen.Name);
                    Assert.Contains(Descendants<Button>(view), b => Equals(b.Content, screen.PrimaryActionText) && b.IsEnabled);
                }
                Render(view, size, $"color-blocked-screens-{lang}");
                await vm.EndPreviewAsync();

                var backend = new Mock<IColorTrialBackend>();
                backend.Setup(b => b.DiscoverAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { capability });
                var store = new Mock<IColorRecoveryStore>(); store.Setup(s => s.Read()).Returns(new ColorRecoveryReadResult(new(), true));
                var service = new ColorTemperatureService(backend.Object, store.Object);
                await service.ApplySettingsAsync(new() { Enabled = true });
                var registry = new Mock<IColorValidationRegistry>(); registry.SetupGet(r => r.IsAvailable).Returns(true);
                var dialog = new ColorValidationWindow(service, registry.Object, "one", "Example display");
                var body = Assert.IsAssignableFrom<FrameworkElement>(dialog.Content);
                body.Measure(new Size(540, 900)); var dialogSize = new Size(540, body.DesiredSize.Height);
                body.Arrange(new Rect(dialogSize)); body.UpdateLayout();
                var start = Descendants<Button>(body).Single(b => Equals(b.Content, UiText.Get("开始 15 秒验证")));
                Assert.False(start.IsEnabled);
                Assert.Contains(Descendants<TextBlock>(body), t => t.Visibility == Visibility.Visible && t.Text == UiText.Get("请先关闭色温调节并保存，再进行验证。"));
                dialog.Content = null;
                ((ScrollViewer)body).Foreground = (Brush)Application.Current.FindResource("PrimaryForegroundColor");
                var surface = new Border { Background = (Brush)Application.Current.FindResource("SecondaryBackgroundColor"), Child = body };
                surface.Measure(dialogSize); surface.Arrange(new Rect(dialogSize)); surface.UpdateLayout();
                Render(surface, dialogSize, $"color-verification-blocked-{lang}");
                dialog.Close(); await service.StopAsync();
            }
        }
        finally { UiText.Current.SetLanguage(language); }
    }

    [WpfFact]
    public void TwoSlidersRenderAtBothSizesAndKeepInvalidTypedValuesForValidation()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        string language = UiText.Current.Language;
        try
        {
            foreach (string lang in new[] { "zh-CN", "en" })
                foreach (int width in new[] { 460, 720 })
                {
                    UiText.Current.SetLanguage(lang);
                    var fixture = new Fixture(); fixture.Model.Enabled = true; fixture.Model.DayUsesOriginal = false; fixture.Model.PreviewScreen = fixture.Model.Screens[0];
                    var view = new ColorSettingsView { DataContext = fixture.Model, Width = width, Height = 650, Background = (Brush)Application.Current.FindResource("PrimaryBackgroundColor"), Foreground = (Brush)Application.Current.FindResource("PrimaryForegroundColor") };
                    var size = new Size(width, 650); view.Measure(size); view.Arrange(new Rect(size)); view.UpdateLayout();
                    var scroll = Descendants<ScrollViewer>(view).First();
                    var sliders = Descendants<Slider>(view).ToArray(); Assert.Equal(2, sliders.Length);
                    foreach (var slider in sliders) { Assert.Equal(3500, slider.Minimum); Assert.Equal(6500, slider.Maximum); }
                    scroll.ScrollToVerticalOffset(sliders[0].TranslatePoint(new Point(), (UIElement)scroll.Content).Y - 130); view.UpdateLayout();
                    Render(view, size, $"color-sliders-{lang}-{width}");
                    fixture.Model.DayKelvin = "3"; view.UpdateLayout(); Assert.Equal("3", fixture.Model.DayKelvin); Assert.NotNull(fixture.Model.Error);
                    fixture.Model.Discard();
                }
        }
        finally { UiText.Current.SetLanguage(language); }
    }

    [WpfFact]
    public async Task OriginalChoiceAndTurningOffCancelTrialWithoutForgettingCustomTemperature()
    {
        var f = new Fixture(); var vm = f.Model;
        vm.Enabled = true; vm.UsesDayTemperature = true; vm.DayKelvin = "5600";
        vm.PreviewWorkCommand.Execute(null); await Until(() => vm.CanCompareOriginal);
        vm.DayUsesOriginal = true; Assert.False(vm.CanCompareOriginal); Assert.Equal("5600", vm.DayKelvin);
        vm.UsesDayTemperature = true; await Task.Delay(200); Assert.Single(f.Calls);
        vm.PreviewWorkCommand.Execute(null); vm.Enabled = false;
        await Task.Delay(200); Assert.Single(f.Calls); Assert.Equal("5600", vm.DayKelvin);
        Assert.True(vm.TrySave()); Assert.False(f.Store.Load().ForScreen("one").Enabled);
        Assert.Equal(5600, f.Store.Load().ForScreen("one").DayKelvin);
    }

    [WpfFact]
    public async Task VerificationShutdownWaitsForRestorationAndOnlySavesActiveSwitches()
    {
        var restored = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var saved = new ColorTemperatureSettings { Enabled = true, ParticipatingScreens = new() { ["one"] = true, ["two"] = true } };
        var f = new Fixture(saved: saved, applyAndWait: _ => restored.Task); var vm = f.Model;
        vm.DayKelvin = "3"; vm.SelectedScreen = vm.Screens[1]; vm.MorningStart = "09:30";
        var stopping = vm.DisableForValidationAsync();
        Assert.False(stopping.IsCompleted); Assert.False(vm.CanEdit);
        Assert.False(f.Store.Load().AnyEnabled);
        Assert.Equal(5300, f.Store.Load().ForScreen("one").DayKelvin);
        Assert.Equal("07:00", f.Store.Load().ForScreen("two").MorningStart);
        restored.SetResult(); Assert.True(await stopping); Assert.True(vm.CanEdit); Assert.False(vm.SavedEnabled);
        Assert.Equal("09:30", vm.MorningStart); Assert.False(vm.Enabled);
        vm.SelectedScreen = vm.Screens[0]; Assert.Equal("3", vm.DayKelvin); Assert.False(vm.Enabled);
        vm.Discard(); Assert.Equal("5300", vm.DayKelvin); Assert.False(vm.Enabled); Assert.False(vm.IsDirty);
    }

    [WpfFact]
    public async Task ShutdownStorageFailureKeepsSavedStateAndDoesNotAttemptRestoration()
    {
        int calls = 0;
        var f = new Fixture(saved: new() { Enabled = true, ParticipatingScreens = new() { ["one"] = true } },
            applyAndWait: _ => { calls++; return Task.CompletedTask; });
        f.Model.NightKelvin = "4100"; f.Fs.WritesFail = true;
        Assert.False(await f.Model.DisableForValidationAsync()); Assert.True(f.Model.SavedEnabled);
        Assert.Equal(0, calls); Assert.Equal("4100", f.Model.NightKelvin); Assert.True(f.Model.IsDirty);
    }

    [WpfFact]
    public async Task ShutdownRestoreFailureKeepsOffStateAndReportsIncompleteRestoration()
    {
        var f = new Fixture(saved: new() { Enabled = true, ParticipatingScreens = new() { ["one"] = true } },
            applyAndWait: _ => throw new InvalidOperationException("fixture"));
        Assert.False(await f.Model.DisableForValidationAsync()); Assert.False(f.Model.SavedEnabled);
        Assert.False(f.Store.Load().AnyEnabled);
        Assert.Contains(UiText.Get("关闭状态已保存，但恢复未完成。请检查屏幕与恢复记录。"), f.Model.Notice);
    }

    [WpfFact]
    public void DiscoveryDoesNotEnrollNewDisplaysAndPreservesDisconnectedDrafts()
    {
        var f = new Fixture(); var vm = f.Model;
        vm.Enabled = true; vm.NightKelvin = "4100";
        vm.UpdateCapabilities([new(Fixture.Scope("two"), ColorCapabilityState.Supported, "fixture")], [], "");
        Assert.Single(vm.VerifiedScreens); Assert.Equal("two", vm.SelectedScreen!.HardwareId); Assert.False(vm.Enabled);
        vm.UpdateCapabilities([new(Fixture.Scope("one"), ColorCapabilityState.Supported, "fixture"), new(Fixture.Scope("two"), ColorCapabilityState.Supported, "fixture"), new(Fixture.Scope("three"), ColorCapabilityState.Unverified, "fixture")], [], "");
        vm.SelectedScreen = vm.Screens.Single(s => s.HardwareId == "one");
        Assert.Equal("4100", vm.NightKelvin); Assert.True(vm.Enabled);
        Assert.DoesNotContain(vm.VerifiedScreens, s => s.HardwareId == "three");
        Assert.False(vm.Screens.Single(s => s.HardwareId == "three").Selected);
    }

    [WpfFact]
    public async Task ScreenSelectorWorkChoiceAndEmptyStateRenderAndKeepDrafts()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        var f = new Fixture(); var vm = f.Model;
        var view = new ColorSettingsView { DataContext = vm, Width = 720, Height = 650 };
        view.SetResourceReference(Control.BackgroundProperty, "PrimaryBackgroundColor");
        view.SetResourceReference(Control.ForegroundProperty, "PrimaryForegroundColor");
        void Layout() { view.Measure(new Size(720, 650)); view.Arrange(new Rect(0, 0, 720, 650)); view.UpdateLayout(); }
        Layout(); Render(view, new(720, 650), "color-v5-off");
        var toggle = (CheckBox)view.FindName("ColorEnabledToggle"); toggle.IsChecked = true;
        Assert.True(vm.Enabled);
        var custom = Descendants<RadioButton>(view).Single(b => b.GroupName == "WorkTarget" && Equals(b.Content, UiText.Get("自定义色温")));
        custom.IsChecked = true; Assert.False(vm.DayUsesOriginal); vm.DayKelvin = "5600";
        Descendants<RadioButton>(view).Single(b => b.GroupName == "WorkTarget" && Equals(b.Content, UiText.Get("保持原色"))).IsChecked = true;
        Assert.True(vm.DayUsesOriginal); Assert.Equal("5600", vm.DayKelvin);
        var cards = (ListBox)view.FindName("VerifiedScreenCards"); cards.SelectedIndex = 1;
        Assert.False(vm.Enabled); Assert.Equal("5300", vm.DayKelvin);
        cards.SelectedIndex = 0; Assert.True(vm.Enabled); Assert.Equal("5600", vm.DayKelvin);
        vm.TrialExpanded = true;
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        Layout();
        Assert.True(Descendants<Button>(view).Single(b => b.Command == vm.PreviewWorkCommand).IsEnabled);
        var scroll = Descendants<ScrollViewer>(view).First(); scroll.ScrollToBottom(); view.UpdateLayout();
        Render(view, new(720, 650), "color-v5-trial");
        Assert.Empty(f.Calls);
        vm.UpdateCapabilities(Enumerable.Range(1, 4).Select(i => new ColorCapability(Fixture.Scope("display-" + i), ColorCapabilityState.Supported, "fixture")).ToArray(), [], "");
        Layout(); Assert.True(vm.UseScreenDropdown);
        var picker = Descendants<ComboBox>(view).Single(c => c.ItemsSource == vm.VerifiedScreens); picker.SelectedIndex = 3;
        Assert.Equal("display-4", vm.SelectedScreen!.HardwareId); Assert.False(vm.Enabled);
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        Layout();
        Assert.Contains(Descendants<TextBlock>(picker), t => t.Text == vm.SelectedScreen.SelectionLabel);
        Render(view, new(720, 650), "color-v5-four-displays");
        vm.UpdateCapabilities([], [], ""); Layout(); Assert.True(vm.HasNoVerifiedScreens);
        Render(view, new(720, 650), "color-v5-empty");
    }

    private static async Task Until(Func<bool> condition)
    {
        var end = Environment.TickCount64 + 3000;
        while (!condition() && Environment.TickCount64 < end) await Task.Delay(15);
        Assert.True(condition(), "Expected preview state was not reached.");
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject node) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i); if (child is T value) yield return value;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Render(FrameworkElement content, Size size, string name)
    {
        var output = Environment.GetEnvironmentVariable("SCREENKEEPER_RENDER_QA"); if (string.IsNullOrEmpty(output)) return;
        Directory.CreateDirectory(output);
        var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(output, name + ".png")); encoder.Save(stream);
    }
    [WpfFact]
    public async Task RestOriginalHasIndependentDraftSummaryAndPreviewTarget()
    {
        var f = new Fixture(); var vm = f.Model; vm.Enabled = true;
        Assert.True(vm.DayUsesOriginal); Assert.False(vm.NightUsesOriginal); Assert.Equal("4500", vm.NightKelvin);
        vm.NightKelvin = "4200"; vm.NightUsesOriginal = true;
        Assert.Contains("4500", vm.SavedRestSummary); Assert.Contains(UiText.Get("原色"), vm.RestScheduleLabel);
        Assert.True(vm.TrySave()); Assert.Contains(UiText.Get("保持原色"), vm.SavedRestSummary);
        Assert.True(f.Store.Load().ForScreen("one").NightUsesOriginal);
        vm.PreviewRestCommand.Execute(null); await Task.Delay(220); Assert.Null(f.Calls.Last().Kelvin);
        vm.NightUsesOriginal = false; Assert.Equal("4200", vm.NightKelvin); Assert.False(vm.CanCompareOriginal);
        vm.SelectedScreen = vm.Screens[1]; Assert.False(vm.NightUsesOriginal);
        vm.SelectedScreen = vm.Screens[0]; Assert.False(vm.NightUsesOriginal);
        vm.Discard(); Assert.True(vm.NightUsesOriginal); Assert.Equal("4200", vm.NightKelvin);
        vm.Mode = "Manual"; Assert.True(vm.UsesNightTemperature);
        vm.PreviewRestCommand.Execute(null); await Task.Delay(220); Assert.Equal(4200, f.Calls.Last().Kelvin);
        await vm.EndPreviewAsync();
    }

    private static void RenderRecoveryDialog(Window dialog, string name)
    {
        var body = (FrameworkElement)dialog.Content;
        dialog.Content = null;
        ((ScrollViewer)body).Foreground = (Brush)Application.Current.FindResource("PrimaryForegroundColor");
        var surface = new Border { Background = (Brush)Application.Current.FindResource("SecondaryBackgroundColor"), Child = body };
        surface.Measure(new Size(dialog.Width, 1000));
        var size = new Size(dialog.Width, surface.DesiredSize.Height);
        surface.Arrange(new Rect(size)); surface.UpdateLayout();
        Render(surface, size, name);
        surface.Child = null; dialog.Content = body;
    }

    private sealed class Fixture
    {
        public FakeFileSystem Fs { get; } = new();
        public IndependentSettingsStore<ColorTemperatureSettings> Store { get; }
        public ColorSettingsViewModel Model { get; }
        public List<(string Id, int? Kelvin)> Calls { get; } = [];
        public List<ColorTemperatureSettings> Applied { get; } = [];
        public int ValidationCalls, RecheckCalls, ResetCalls;
        public static ColorDisplayScope Scope(string id) => new(id, id, id, "Example " + id, "topology", "mode");
        public Fixture(Func<string, int?, CancellationToken, Task<bool>>? preview = null, ColorTemperatureSettings? saved = null, Func<ColorTemperatureSettings, Task>? applyAndWait = null,
            Func<Task>? cancelPreview = null)
        {
            var root = new StorageRoot(Fs, false); var files = new AppDataFileStore(Fs, root);
            Store = new(files, Fs, root, "color-temperature.json", () => new(), s => s.Validate(), maxSchema: 2);
            Model = new(Store, saved ?? Store.Load(), settings => Applied.Add(settings.Copy()), cancelPreview ?? (() => Task.CompletedTask), _ => Task.CompletedTask, _ => Task.CompletedTask, (_, _) => Task.CompletedTask,
                validate: _ => { ValidationCalls++; return Task.CompletedTask; },
                livePreview: async (id, kelvin, token) => { Calls.Add((id, kelvin)); return preview == null || await preview(id, kelvin, token); },
                recheckOriginal: _ => { RecheckCalls++; return Task.CompletedTask; }, resetBaseline: () => { ResetCalls++; return Task.CompletedTask; }, applyAndWait: applyAndWait);
            Model.UpdateCapabilities([new(Scope("one"), ColorCapabilityState.Supported, "fixture"), new(Scope("two"), ColorCapabilityState.Supported, "fixture")], [], "");
        }
    }
}
