using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Messaging.Interfaces;
using Serilog;

namespace OLED_Sleeper.Features.MonitorBehavior.Handlers
{
    /// <summary>
    /// Handles the execution of the <see cref="RestoreMonitorStateCommand"/>.
    /// Restores a monitor to its default state by hiding overlays and undimming.
    /// </summary>
    public class RestoreMonitorStateCommandHandler : ICommandHandler<RestoreMonitorStateCommand>
    {
        private readonly IMediator _mediator;
        private readonly Infrastructure.Runtime.ReminderDisplayGate? _reminders;

        public RestoreMonitorStateCommandHandler(IMediator mediator, Infrastructure.Runtime.ReminderDisplayGate? reminders = null)
        {
            _mediator = mediator;
            _reminders = reminders;
        }

        /// <summary>
        /// Handles the command to restore the monitor's state.
        /// </summary>
        /// <param name="command">The command containing the monitor's hardware ID.</param>
        public async Task HandleAsync(RestoreMonitorStateCommand command)
        {
            Log.Information("Restoring state for monitor {HardwareId}.", command.HardwareId);
            bool restored = true;
            try { await _mediator.SendAsync(new HideBlackoutOverlayCommand { HardwareId = command.HardwareId }); }
            catch (Exception ex) { restored = false; Log.Error(ex, "Overlay restore failed."); }
            try { await _mediator.SendAsync(new ApplyUndimCommand { HardwareId = command.HardwareId }); }
            catch (Exception ex) { restored = false; Log.Error(ex, "Brightness restore failed."); }
            if (restored) _reminders?.Awake(command.HardwareId);
        }
    }
}
