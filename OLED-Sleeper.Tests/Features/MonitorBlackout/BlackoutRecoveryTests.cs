using Moq;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBehavior.Handlers;
using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Handlers;
using OLED_Sleeper.Features.MonitorBlackout.Services;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Models;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Infrastructure.Runtime;
using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using OLED_Sleeper.Messaging.Interfaces;
using OLED_Sleeper.Tests.TestDoubles;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorBlackout;

public class BlackoutRecoveryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WakeDuringMonitorLookupMustNotBeCoveredByLateBlackout(bool lowerBrightness)
    {
        var lookup = new TaskCompletionSource<IReadOnlyList<MonitorInfo>>();
        var monitors = new Mock<IMonitorInfoManager>();
        monitors.Setup(m => m.GetCurrentMonitorsAsync()).Returns(lookup.Task);
        var factory = new FakeOverlayWindowFactory();
        var overlays = new MonitorBlackoutService(new ImmediateDispatcher(), factory);
        var dimming = new Mock<IMonitorDimmingService>();
        var blackout = new ApplyBlackoutOverlayCommandHandler(monitors.Object, overlays, dimming.Object);
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.SendAsync(It.IsAny<ApplyBlackoutOverlayCommand>()))
            .Returns<ApplyBlackoutOverlayCommand>(blackout.HandleAsync);
        var idle = new ApplyMonitorIdleBehaviorCommandHandler(mediator.Object);
        bool stillIdle = true;
        var bounds = new Rect(0, 0, 100, 100);
        var args = new MonitorIdleStateEventArgs("fixture", 1, bounds,
            new MonitorSettings { Behavior = MonitorBehaviorType.Blackout, LowerBrightnessOnBlackout = lowerBrightness }, 0, ActivityReason.None)
        { StillIdle = () => stillIdle };

        var pending = idle.HandleAsync(new(args));
        stillIdle = false;
        await overlays.HideBlackoutOverlayAsync("fixture");
        lookup.SetResult([new MonitorInfo { HardwareId = "fixture", Bounds = bounds, Capabilities = new(true, 100) }]);
        await pending;

        Assert.Empty(factory.Created);
        Assert.True(args.IsIgnored);
        dimming.Verify(m => m.DimMonitorAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task WakeWhileUiShowIsQueuedMustNotCreateAnOverlayOrBlockReminders()
    {
        var dispatcher = new QueuedDispatcher();
        var factory = new FakeOverlayWindowFactory();
        var gate = new ReminderDisplayGate();
        var service = new MonitorBlackoutService(dispatcher, factory, reminders: gate);
        bool stillIdle = true;
        var pending = service.TryShowBlackoutOverlayAsync("fixture", new(0, 0, 100, 100), () => stillIdle);
        stillIdle = false;
        dispatcher.Drain();

        Assert.False(await pending);
        Assert.Empty(factory.Created);
        Assert.False(gate.IsSleeping("fixture"));
    }

    [Fact]
    public async Task StopClosesEveryOverlayAndRejectsAlreadyQueuedAndFutureShows()
    {
        var dispatcher = new QueuedDispatcher();
        var factory = new FakeOverlayWindowFactory();
        var service = new MonitorBlackoutService(dispatcher, factory);
        var first = service.ShowBlackoutOverlayAsync("one", new(0, 0, 100, 100));
        var second = service.ShowBlackoutOverlayAsync("two", new(100, 0, 100, 100));
        dispatcher.Drain(); await Task.WhenAll(first, second);
        var handles = factory.Created.Select(w => w.Handle).ToArray();
        var queued = service.TryShowBlackoutOverlayAsync("three", new(200, 0, 100, 100), () => true);

        service.Stop(); service.Stop();
        Assert.All(factory.Created, w => Assert.True(w.IsClosed));
        Assert.All(handles, h => Assert.False(service.IsOverlayWindow(h)));
        dispatcher.Drain(); Assert.False(await queued);
        var late = service.TryShowBlackoutOverlayAsync("one", new(0, 0, 100, 100), () => true);
        dispatcher.Drain(); Assert.False(await late);
        Assert.Equal(2, factory.Created.Count);
    }

    private sealed class QueuedDispatcher : IDispatcher
    {
        private readonly Queue<Action> _pending = new();
        public bool IsOnUiThread => true;
        public void Invoke(Action action) => action();
        public Task InvokeAsync(Action action)
        {
            var completion = new TaskCompletionSource();
            _pending.Enqueue(() =>
            {
                try { action(); completion.SetResult(); }
                catch (Exception error) { completion.SetException(error); }
            });
            return completion.Task;
        }
        public Task InvokeAfterInputAsync(Action action) => InvokeAsync(action);
        public void Drain() { while (_pending.TryDequeue(out var action)) action(); }
    }
}
