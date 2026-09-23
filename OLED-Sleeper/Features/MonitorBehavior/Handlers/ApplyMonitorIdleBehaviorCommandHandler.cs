using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Messaging.Interfaces;
using Serilog;

namespace OLED_Sleeper.Features.MonitorBehavior.Handlers
{
    /// <summary>
    /// Handles the <see cref="ApplyMonitorIdleBehaviorCommand"/> by dispatching the appropriate dim or blackout command.
    /// </summary>
    public class ApplyMonitorIdleBehaviorCommandHandler : ICommandHandler<ApplyMonitorIdleBehaviorCommand>
    {
        private readonly IMediator _mediator;
        private readonly IMonitorPrivacyService? _privacy;
        private readonly Infrastructure.Runtime.ReminderDisplayGate? _reminders;

        public ApplyMonitorIdleBehaviorCommandHandler(IMediator mediator, IMonitorPrivacyService? privacy = null, Infrastructure.Runtime.ReminderDisplayGate? reminders = null)
        {
            _mediator = mediator;
            _privacy = privacy;
            _reminders = reminders;
        }

        /// <summary>
        /// Handles the <see cref="ApplyMonitorIdleBehaviorCommand"/> by dispatching the appropriate dim or blackout command for the monitor.
        /// </summary>
        /// <param name="command">The command containing the monitor idle event arguments.</param>
        public async Task HandleAsync(ApplyMonitorIdleBehaviorCommand command)
        {
            var e = command.EventArgs;
            bool StillIdle() => e.StillIdle?.Invoke() ?? true;
            if (!StillIdle()) { e.IsIgnored = true; return; }
            if (_privacy != null && e.Settings.Behavior != MonitorBehaviorType.None &&
                !await _privacy.TryPrepareForSleepAsync(e.HardwareId, StillIdle)) { e.IsIgnored = true; return; }
            if (!StillIdle()) { e.IsIgnored = true; return; }
            if (e.Settings.Behavior == MonitorBehaviorType.Dim) _reminders?.Sleep(e.HardwareId);
            switch (e.Settings.Behavior)
            {
                case MonitorBehaviorType.Blackout:
                    var blackout = new ApplyBlackoutOverlayCommand { HardwareId = e.HardwareId, LowerBrightness = e.Settings.LowerBrightnessOnBlackout, StillIdle = StillIdle };
                    await _mediator.SendAsync(blackout);
                    e.IsIgnored = blackout.IsIgnored;
                    break;
                case MonitorBehaviorType.Dim:
                    await _mediator.SendAsync(new ApplyDimCommand { HardwareId = e.HardwareId, DimLevel = (int)e.Settings.DimLevel });
                    break;
                default:
                    Log.Information("No idle behavior to apply for monitor {HardwareId}.", e.HardwareId);
                    break;
            }
        }
    }
}
