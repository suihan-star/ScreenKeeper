using OLED_Sleeper.Features.MonitorPrivacy;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorPrivacy;

public class ScreenApplicationServiceTests
{
    private static readonly Rect Screen = new(0, 0, 100, 100);
    private static readonly ApplicationWindow Target = new("one", 1, @"C:\Apps\app.exe", "Test", "", Screen, false, 0);
    private sealed class Access : IApplicationWindowAccess, IDesktopWindowAccess
    {
        public List<ApplicationWindow> Apps = [Target];
        public List<DesktopWindow> Windows = [];
        public int Launches, Places, Minimizes;
        public bool Running, Covered = true, Placement = true;
        public Action? OnMinimize;
        public Action? OnPlace;
        public IReadOnlyList<ApplicationWindow> Enumerate(bool titles = false) => Apps.ToArray();
        public bool Launch(string path) { Launches++; Apps.Add(Target); return true; }
        public bool IsRunning(string path) => Running || Apps.Count > 0;
        public bool Place(ApplicationWindow window, Rect bounds) { Places++; OnPlace?.Invoke(); return Placement; }
        public bool Covers(ApplicationWindow window, Rect bounds) => Covered;
        public IReadOnlyList<DesktopWindow> EnumerateVisibleWindows() => Windows.ToArray();
        public bool TryMinimize(DesktopWindow window, Rect screen) { Minimizes++; Windows.Remove(window); OnMinimize?.Invoke(); return true; }
    }
    private static PrivacyOptions Options() => new() { ApplicationPath = Target.Path, WindowClass = Target.ClassName };
    [Fact]
    public async Task AmbiguousWindowsDoNotMoveOrMinimizeAnything()
    {
        var access = new Access(); access.Apps.Add(Target with { Token = "two", Handle = 2 });
        Assert.False(await new ScreenApplicationService(access, access).ShowAsync(Options(), Screen));
        Assert.Equal(0, access.Places); Assert.Equal(0, access.Minimizes);
    }
    [Fact]
    public async Task MissingChosenWindowNeverFallsBackToAnotherWindow()
    {
        var access = new Access(); var options = Options(); options.WindowToken = "closed";
        Assert.False(await new ScreenApplicationService(access, access).ShowAsync(options, Screen));
        Assert.Equal(0, access.Places);
    }
    [Fact]
    public async Task ExactWindowCanBeChosenAmongMultipleWindows()
    {
        var access = new Access(); access.Apps.Add(Target with { Token = "two", Handle = 2 });
        var options = Options(); options.WindowToken = "one";
        Assert.True(await new ScreenApplicationService(access, access).ShowAsync(options, Screen));
    }
    [Fact]
    public async Task CrossScreenMoveNeedsExplicitOption()
    {
        var access = new Access { Apps = [Target with { Bounds = new Rect(100, 0, 100, 100) }] };
        var service = new ScreenApplicationService(access, access); var options = Options();
        Assert.False(await service.ShowAsync(options, Screen)); Assert.Equal(0, access.Places);
        options.MoveFromOtherScreen = true;
        Assert.True(await service.ShowAsync(options, Screen));
    }
    [Fact]
    public async Task MissingAppNeedsLaunchPermission_AndExistingProcessIsNeverDuplicated()
    {
        var access = new Access { Apps = [] }; var options = Options();
        var service = new ScreenApplicationService(access, access);
        Assert.False(await service.ShowAsync(options, Screen)); Assert.Equal(0, access.Launches);
        options.LaunchIfMissing = true; access.Running = true;
        Assert.False(await service.ShowAsync(options, Screen)); Assert.Equal(0, access.Launches);
        access.Running = false; Assert.True(await service.ShowAsync(options, Screen)); Assert.Equal(1, access.Launches);
    }
    [Fact]
    public async Task PrivateWindowsAreMinimized_AndFailedPlacementDoesNotClaimSuccess()
    {
        var access = new Access { Windows = [new(9, Screen, true)], Placement = false };
        Assert.False(await new ScreenApplicationService(access, access).ShowAsync(Options(), Screen));
        Assert.Equal(1, access.Minimizes); Assert.Equal(1, access.Places);
    }
    [Fact]
    public async Task CancellationAfterMinimizeNeverPlacesTarget()
    {
        using var cancellation = new CancellationTokenSource();
        var access = new Access { Windows = [new(9, Screen, true)], OnMinimize = cancellation.Cancel };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ScreenApplicationService(access, access).ShowAsync(Options(), Screen, cancellation.Token));
        Assert.Equal(0, access.Places);
    }
    [Fact]
    public async Task TargetPopupRetainsCurtainBeforeHidingAnything()
    {
        var access = new Access { Windows = [new(2, Screen, true, Target.Handle)] };
        Assert.False(await new ScreenApplicationService(access, access).ShowAsync(Options(), Screen));
        Assert.Equal(0, access.Minimizes);
    }

    private static Access WeWork()
    {
        var windows = new[] { ApplicationWindowFamilyPolicyTests.Main, ApplicationWindowFamilyPolicyTests.Title, ApplicationWindowFamilyPolicyTests.Shadow };
        return new Access { Apps = windows.ToList(), Windows = windows.Select(w => new DesktopWindow(w.Handle, w.Bounds, true, w.Owner)).ToList() };
    }
    private static PrivacyOptions WeWorkOptions() => new() { ApplicationPath = ApplicationWindowFamilyPolicyTests.Main.Path, WindowClass = "WeWorkWindow", WindowToken = "main" };

    [Fact]
    public async Task WeWorkChromeIsKeptWhileOtherPrivateWindowsAreMinimized()
    {
        var access = WeWork(); access.Windows.Add(new(99, Screen, true));
        var service = new ScreenApplicationService(access, access);
        Assert.True(await service.ShowAsync(WeWorkOptions(), Screen));
        Assert.Equal(1, access.Minimizes); Assert.Equal(1, access.Places);
        Assert.Equal(new nint[] { 10, 11, 12 }, access.Windows.Select(w => w.Handle));
        Assert.Null(service.FailureFor(Screen));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    public async Task RealDialogOnMainOrChromeStillBlocksBeforeAnyAction(int owner)
    {
        var access = WeWork(); access.Windows.Add(new(99, Screen, true, owner));
        var service = new ScreenApplicationService(access, access);
        Assert.False(await service.ShowAsync(WeWorkOptions(), Screen));
        Assert.Equal(0, access.Minimizes); Assert.Equal(0, access.Places);
        Assert.Contains("弹窗", service.FailureFor(Screen));
    }

    [Fact]
    public async Task PopupAppearingWhilePrivateWindowsHidePreventsPlacement()
    {
        var access = WeWork(); access.Windows.Add(new(99, Screen, true));
        access.OnMinimize = () => access.Windows.Add(new(100, Screen, true, 10));
        Assert.False(await new ScreenApplicationService(access, access).ShowAsync(WeWorkOptions(), Screen));
        Assert.Equal(1, access.Minimizes); Assert.Equal(0, access.Places);
    }

    [Fact]
    public async Task ChangedCompanionAfterPlacementDoesNotReleaseCurtain()
    {
        var access = WeWork();
        access.OnPlace = () => access.Apps[1] = access.Apps[1] with { Bounds = new Rect(100, 0, 100, 10) };
        Assert.False(await new ScreenApplicationService(access, access).ShowAsync(WeWorkOptions(), Screen));
        Assert.Equal(0, access.Minimizes); Assert.Equal(1, access.Places);
    }

    [Fact]
    public async Task FailedPlacementHasActionableStatus_AndLaterSuccessClearsIt()
    {
        var access = new Access { Placement = false }; var service = new ScreenApplicationService(access, access);
        Assert.False(await service.ShowAsync(Options(), Screen));
        Assert.Contains("窗口位置", service.FailureFor(Screen));
        access.Placement = true;
        Assert.True(await service.ShowAsync(Options(), Screen));
        Assert.Null(service.FailureFor(Screen));
    }

    [Fact]
    public async Task ClosingTheChosenWindowDuringMinimizationReportsChangedWindow()
    {
        var access = WeWork(); access.Windows.Add(new(99, Screen, true));
        access.OnMinimize = () => access.Apps.Clear();
        var service = new ScreenApplicationService(access, access);
        Assert.False(await service.ShowAsync(WeWorkOptions(), Screen));
        Assert.Equal(0, access.Places);
        Assert.Contains("发生变化", service.FailureFor(Screen));
    }
}
