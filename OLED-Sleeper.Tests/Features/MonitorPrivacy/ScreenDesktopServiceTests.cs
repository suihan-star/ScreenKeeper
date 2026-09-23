using OLED_Sleeper.Features.MonitorPrivacy;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorPrivacy;

public class ScreenDesktopServiceTests
{
    private static readonly Rect Screen = new(0, 0, 100, 100);

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    public void MaximizedResizeBorderIsClippedUsingSystemDpiMetrics(int border)
    {
        var monitor = new Rect(2560, 0, 1920, 1080);
        var frame = new Rect(2560 - border, -border, 1920 + 2 * border, 1032 + 2 * border);
        var normalized = DesktopWindowPolicy.MaximizedVisibleBounds(frame, monitor, border, border);
        Assert.True(monitor.Contains(normalized));
        Assert.Equal(2560, normalized.X);
        Assert.Equal(1920, normalized.Width);
    }

    [Fact]
    public void MaximizedWindowBeyondResizeBorderAllowanceRemainsCrossScreen()
    {
        var frame = new Rect(2400, 0, 2080, 1080);
        var monitor = new Rect(2560, 0, 1920, 1080);
        Assert.Equal(frame, DesktopWindowPolicy.MaximizedVisibleBounds(frame, monitor, 8, 8));
        Assert.Equal(frame, DesktopWindowPolicy.MaximizedVisibleBounds(frame, monitor, 0, 0));
    }

    [Fact]
    public void AuxiliaryWindowPastOuterDesktopEdgeIsNotMistakenForSpanningWindow()
    {
        var screens = new[] { new Rect(0, 0, 2560, 1440), new Rect(2560, 0, 1920, 1080) };
        var visible = DesktopWindowPolicy.VisibleBounds(new Rect(4250, 776, 235, 44), screens);
        Assert.Equal(new Rect(4250, 776, 230, 44), visible);
        Assert.True(DesktopWindowPolicy.FitsScreen(new DesktopWindow(1, visible, false), screens[1]));
    }

    [Fact]
    public void RealCrossScreenContentStillBlocks_EvenWithOffDesktopClipping()
    {
        var screens = new[] { new Rect(-100, 0, 100, 100), Screen };
        var visible = DesktopWindowPolicy.VisibleBounds(new Rect(-1, 0, 101, 100), screens);
        Assert.False(DesktopWindowPolicy.FitsScreen(new DesktopWindow(1, visible, false), Screen));
    }

    [Fact]
    public void EmptySpaceBetweenUnevenDisplaysDoesNotCountAsAnotherScreen()
    {
        var screens = new[] { Screen, new Rect(150, 50, 100, 100) };
        var visible = DesktopWindowPolicy.VisibleBounds(new Rect(90, 0, 100, 40), screens);
        Assert.Equal(new Rect(90, 0, 10, 40), visible);
        Assert.True(DesktopWindowPolicy.FitsScreen(new DesktopWindow(1, visible, false), Screen));
        Assert.True(DesktopWindowPolicy.VisibleBounds(new Rect(300, 300, 10, 10), screens).IsEmpty);
    }
    private sealed class Windows : IDesktopWindowAccess
    {
        public List<DesktopWindow> Visible = new();
        public List<nint> Requests = new();
        public Action<int>? Reading;
        public Func<DesktopWindow, bool>? Minimizing;
        private int _reads;
        public IReadOnlyList<DesktopWindow> EnumerateVisibleWindows()
        {
            Reading?.Invoke(++_reads);
            return Visible.ToArray();
        }
        public bool TryMinimize(DesktopWindow window, Rect screen)
        {
            Requests.Add(window.Handle);
            if (Minimizing != null) return Minimizing(window);
            Visible.Remove(window);
            return true;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActuallyMinimizesContainedWindow_WithOrWithoutStandardMinimizeButton(bool hasButton)
    {
        var windows = new Windows { Visible = new() { new(1, Screen, hasButton) } };
        Assert.True(await new ScreenDesktopService(windows).ShowDesktopAsync(Screen));
        Assert.Equal(new nint[] { 1 }, windows.Requests);
    }

    [Fact]
    public async Task OwnedPopupCanDisappearWithOwner_WithoutItsOwnMinimizeRequest()
    {
        var windows = new Windows { Visible = new() { new(2, Screen, false, 1), new(1, Screen, true) } };
        windows.Minimizing = window =>
        {
            Assert.Equal((nint)1, window.Handle);
            windows.Visible.Clear();
            return true;
        };
        Assert.True(await new ScreenDesktopService(windows).ShowDesktopAsync(Screen));
        Assert.Single(windows.Requests);
    }

    [Fact]
    public async Task RemainingOwnedPopupIsTriedAfterOwner()
    {
        var windows = new Windows { Visible = new() { new(2, Screen, false, 1), new(1, Screen, true) } };
        Assert.True(await new ScreenDesktopService(windows).ShowDesktopAsync(Screen));
        Assert.Equal(new nint[] { 1, 2 }, windows.Requests);
    }

    [Fact]
    public async Task OtherScreenWindowIsUntouched()
    {
        var other = new DesktopWindow(2, new Rect(100, 0, 100, 100), false);
        var windows = new Windows { Visible = new() { new(1, Screen, false), other } };
        Assert.True(await new ScreenDesktopService(windows).ShowDesktopAsync(Screen));
        Assert.Equal(new nint[] { 1 }, windows.Requests);
        Assert.Equal(other, Assert.Single(windows.Visible));
    }

    [Fact]
    public async Task SpanningWindowKeepsCurtain_WithoutChangingAnyWindow()
    {
        var windows = new Windows { Visible = new() { new(1, Screen, true), new(2, new Rect(90, 0, 50, 50), false) } };
        Assert.False(await new ScreenDesktopService(windows).ShowDesktopAsync(Screen));
        Assert.Empty(windows.Requests);
    }

    [Fact]
    public async Task OwnerWithPopupOnOtherScreenIsNotMinimized()
    {
        var windows = new Windows { Visible = new() { new(1, Screen, true), new(2, new Rect(100, 0, 50, 50), false, 1) } };
        Assert.False(await new ScreenDesktopService(windows).ShowDesktopAsync(Screen));
        Assert.Empty(windows.Requests);
    }

    [Fact]
    public async Task ClosedDuringRequestIsSuccess_EvenIfApiReturnsFalse()
    {
        var windows = new Windows { Visible = new() { new(1, Screen, false) } };
        windows.Minimizing = _ => { windows.Visible.Clear(); return false; };
        Assert.True(await new ScreenDesktopService(windows).ShowDesktopAsync(Screen));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StillVisibleIsFailure_RegardlessOfApiReturnValue(bool accepted)
    {
        var windows = new Windows { Visible = new() { new(1, Screen, false) }, Minimizing = _ => accepted };
        Assert.False(await new ScreenDesktopService(windows).ShowDesktopAsync(Screen));
        Assert.Single(windows.Requests);
    }

    [Fact]
    public async Task DelayedWindowResponseBeyondOldHalfSecondTimeoutCanComplete()
    {
        var windows = new Windows { Visible = new() { new(1, Screen, false) }, Minimizing = _ => true };
        windows.Reading = read => { if (read == 8) windows.Visible.Clear(); };
        Assert.True(await new ScreenDesktopService(windows).ShowDesktopAsync(Screen));
        Assert.Single(windows.Requests);
    }

    [Fact]
    public async Task CancellationStopsFurtherMinimizationRequests()
    {
        using var cancellation = new CancellationTokenSource();
        var windows = new Windows { Visible = new() { new(1, Screen, false), new(2, Screen, false) } };
        windows.Minimizing = _ => { cancellation.Cancel(); return true; };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ScreenDesktopService(windows).ShowDesktopAsync(Screen, cancellation.Token));
        Assert.Equal(new nint[] { 1 }, windows.Requests);
    }

    [Fact]
    public async Task WindowMovingAcrossScreensDuringVerificationRetainsCurtain()
    {
        var windows = new Windows { Visible = new() { new(1, Screen, false) }, Minimizing = _ => true };
        windows.Reading = read => { if (read == 2) windows.Visible[0] = new(1, new Rect(90, 0, 50, 50), false); };
        Assert.False(await new ScreenDesktopService(windows).ShowDesktopAsync(Screen));
        Assert.Single(windows.Requests);
    }
}
