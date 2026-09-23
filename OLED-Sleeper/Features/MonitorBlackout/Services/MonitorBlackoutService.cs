using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Infrastructure.Runtime;
using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using Serilog;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorBlackout.Services
{
    /// <summary>
    /// Manages blackout overlay windows for monitors asynchronously in a WPF application.
    /// Provides creation, display, and removal of overlay windows, and tracks overlay window handles.
    /// </summary>
    public class MonitorBlackoutService : IMonitorBlackoutService
    {
        private readonly IDispatcher _dispatcher;
        private readonly IPrivacyCurtainHost? _privacy;
        private readonly IOverlayWindowFactory _overlayWindowFactory;
        private readonly ReminderDisplayGate? _reminders;
        private bool _stopped;

        /// <summary>
        /// Guards <see cref="_overlayWindows"/> and <see cref="_overlayHandles"/>. Overlays are created and
        /// closed on the dispatcher thread while <see cref="IsOverlayWindow"/> reads from the idle thread.
        /// Window creation, showing and closing all happen outside this lock.
        /// </summary>
        private readonly object _overlayLock = new();

        /// <summary>
        /// The overlay currently shown on each monitor, keyed by hardware ID. A monitor with no overlay
        /// has no entry.
        /// </summary>
        private readonly Dictionary<string, OverlayRegistration> _overlayWindows = new();

        /// <summary>
        /// The handles of every overlay currently shown, which is what <see cref="IsOverlayWindow"/>
        /// answers from. Overlays that never got a handle are absent.
        /// </summary>
        private readonly HashSet<nint> _overlayHandles = new();

        public MonitorBlackoutService(IDispatcher dispatcher, IOverlayWindowFactory overlayWindowFactory, IPrivacyCurtainHost? privacy = null, ReminderDisplayGate? reminders = null)
        {
            _dispatcher = dispatcher;
            _privacy = privacy;
            _overlayWindowFactory = overlayWindowFactory;
            _reminders = reminders;
        }

        /// <summary>
        /// An overlay window paired with the handle it was tracked under.
        /// </summary>
        /// <param name="Window">The overlay window.</param>
        /// <param name="Handle">The handle recorded when the overlay was shown, or zero if none was obtained.</param>
        private readonly record struct OverlayRegistration(IOverlayWindow Window, nint Handle);

        /// <inheritdoc />
        public async Task ShowBlackoutOverlayAsync(string hardwareId, Rect bounds) =>
            await TryShowBlackoutOverlayAsync(hardwareId, bounds, () => true);

        /// <inheritdoc />
        public async Task<bool> TryShowBlackoutOverlayAsync(string hardwareId, Rect bounds, Func<bool> stillIdle)
        {
            bool applied = false;
            await _dispatcher.InvokeAsync(() =>
            {
                // Lookup and dispatcher queuing can outlive a wake, settings change or shutdown.
                if (_stopped || !stillIdle()) return;
                _reminders?.Sleep(hardwareId);
                applied = true;
                lock (_overlayLock)
                {
                    if (_overlayWindows.ContainsKey(hardwareId)) { _privacy?.SetSleeping(hardwareId, true); return; }
                }

                if (_privacy?.SetSleeping(hardwareId, true) == true) return;
                var overlay = _overlayWindowFactory.Create();
                overlay.Show(bounds);

                nint hwnd = overlay.Handle;

                lock (_overlayLock)
                {
                    if (hwnd != nint.Zero)
                    {
                        _overlayHandles.Add(hwnd);
                    }

                    _overlayWindows[hardwareId] = new OverlayRegistration(overlay, hwnd);
                }
            });
            return applied;
        }

        /// <inheritdoc />
        public void Stop() => _dispatcher.Invoke(() =>
        {
            if (_stopped) return;
            _stopped = true;
            OverlayRegistration[] windows;
            lock (_overlayLock)
            {
                windows = _overlayWindows.Values.ToArray();
                _overlayWindows.Clear();
                _overlayHandles.Clear();
            }
            foreach (var registration in windows)
            {
                try { registration.Window.Close(); }
                catch (Exception ex) { Log.Error(ex, "Could not close a blackout window during shutdown."); }
            }
        });

        /// <inheritdoc />
        public async Task HideBlackoutOverlayAsync(string hardwareId)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                _privacy?.SetSleeping(hardwareId, false);
                OverlayRegistration registration;

                lock (_overlayLock)
                {
                    if (!_overlayWindows.Remove(hardwareId, out registration)) return;

                    // An overlay reports a zero handle once it is closed, so the handle recorded when it
                    // was shown is used here.
                    if (registration.Handle != nint.Zero)
                    {
                        _overlayHandles.Remove(registration.Handle);
                    }
                }

                registration.Window.Close();
            });
        }

        /// <inheritdoc />
        public bool IsOverlayWindow(nint windowHandle)
        {
            if (windowHandle == nint.Zero) return false;

            lock (_overlayLock)
            {
                return _overlayHandles.Contains(windowHandle) || _privacy?.IsCurtainWindow(windowHandle) == true;
            }
        }
    }
}
