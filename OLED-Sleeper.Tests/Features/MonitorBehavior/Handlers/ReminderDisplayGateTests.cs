using Moq;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBehavior.Handlers;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Infrastructure.Runtime;
using OLED_Sleeper.Messaging.Interfaces;

namespace OLED_Sleeper.Tests.Features.MonitorBehavior.Handlers;

public sealed class ReminderDisplayGateTests
{
    [Fact]
    public async Task ReminderEligibilityWaitsForBothRestorations()
    {
        var hide = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var undim = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.SendAsync(It.IsAny<HideBlackoutOverlayCommand>())).Returns(hide.Task);
        mediator.Setup(m => m.SendAsync(It.IsAny<ApplyUndimCommand>())).Returns(undim.Task);
        var gate = new ReminderDisplayGate(); gate.Sleep("fixture");
        var handler = new RestoreMonitorStateCommandHandler(mediator.Object, gate);
        var restoring = handler.HandleAsync(new() { HardwareId = "fixture" });
        Assert.True(gate.IsSleeping("fixture")); hide.SetResult();
        Assert.True(gate.IsSleeping("fixture")); undim.SetResult();
        await restoring; Assert.False(gate.IsSleeping("fixture"));
    }
    [Fact]
    public async Task OverlayFailureStillRestoresBrightnessAndRetainsBlock()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.SendAsync(It.IsAny<HideBlackoutOverlayCommand>())).ThrowsAsync(new InvalidOperationException());
        var gate = new ReminderDisplayGate(); gate.Sleep("fixture");
        await new RestoreMonitorStateCommandHandler(mediator.Object, gate).HandleAsync(new() { HardwareId = "fixture" });
        Assert.True(gate.IsSleeping("fixture"));
        mediator.Verify(m => m.SendAsync(It.IsAny<ApplyUndimCommand>()), Times.Once);
    }
}
