using System.Windows;

namespace OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for asynchronous blackout overlay management on monitors.
    /// </summary>
    public interface IMonitorBlackoutService
    {
        /// <summary>
        /// Asynchronously shows a blackout overlay on the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="bounds">The bounds of the monitor in screen coordinates.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ShowBlackoutOverlayAsync(string hardwareId, Rect bounds);

        /// <summary>Rechecks eligibility on the UI thread and returns false for stale or stopped requests.</summary>
        Task<bool> TryShowBlackoutOverlayAsync(string hardwareId, Rect bounds, Func<bool> stillIdle);

        /// <summary>Closes all blackout windows and rejects queued/future shows. Called on the UI thread at exit.</summary>
        void Stop();

        /// <summary>
        /// Asynchronously hides the blackout overlay for the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HideBlackoutOverlayAsync(string hardwareId);

        /// <summary>
        /// Determines whether the specified window handle belongs to an overlay window.
        /// </summary>
        /// <param name="windowHandle">The window handle to check.</param>
        /// <returns>True if the handle is an overlay window; otherwise, false.</returns>
        bool IsOverlayWindow(nint windowHandle);
    }
}
