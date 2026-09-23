using Moq;
using OLED_Sleeper.Features.MonitorBlackout.Services;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Infrastructure.Hosting;
using OLED_Sleeper.Infrastructure.Hosting.Interfaces;
using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using OLED_Sleeper.Tests.TestDoubles;
using System.Reflection;

namespace OLED_Sleeper.Tests.Hosting;

public class ApplicationShutdownTests
{
    [Fact]
    public async Task ShutdownClearsCoversBeforeWaitingForHardwareAndOnlyRunsOnce()
    {
        var factory = new FakeOverlayWindowFactory();
        var overlays = new MonitorBlackoutService(new ImmediateDispatcher(), factory);
        await overlays.ShowBlackoutOverlayAsync("fixture", new(0, 0, 100, 100));
        var privacy = new Mock<IMonitorPrivacyService>();
        bool privacyDisposed = false;
        privacy.As<IDisposable>().Setup(p => p.Dispose()).Callback(() => privacyDisposed = true);
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(IMonitorPrivacyService))).Returns(privacy.Object);
        provider.Setup(p => p.GetService(typeof(IMonitorBlackoutService))).Returns(overlays);
        var orchestrator = new Mock<IApplicationOrchestrator>();
        bool coversClearedBeforeRestore = false;
        orchestrator.Setup(o => o.StopAsync()).Returns(() =>
        {
            coversClearedBeforeRestore = privacyDisposed && factory.Created.All(w => w.IsClosed);
            return Task.CompletedTask;
        });
        var shutdown = new Mock<IApplicationShutdown>();
        var bootstrapper = new ApplicationBootstrapper([]);
        // Inject only lifecycle boundaries: never start the real container, UI or monitor backend.
        SetField(bootstrapper, "_serviceProvider", provider.Object);
        SetField(bootstrapper, "_orchestrator", orchestrator.Object);
        SetField(bootstrapper, "_applicationShutdown", shutdown.Object);

        bootstrapper.ShutdownApp(); bootstrapper.ShutdownApp();

        Assert.True(coversClearedBeforeRestore);
        orchestrator.Verify(o => o.StopAsync(), Times.Once);
        privacy.As<IDisposable>().Verify(p => p.Dispose(), Times.Once);
        shutdown.Verify(s => s.Shutdown(), Times.Once);
        Assert.False(await overlays.TryShowBlackoutOverlayAsync("fixture", new(0, 0, 100, 100), () => true));
    }

    private static void SetField(object target, string name, object value) =>
        typeof(ApplicationBootstrapper).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
}
