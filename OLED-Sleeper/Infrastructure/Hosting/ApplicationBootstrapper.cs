using Microsoft.Extensions.DependencyInjection;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Infrastructure.Helpers;
using OLED_Sleeper.Infrastructure.Hosting.Interfaces;
using OLED_Sleeper.Infrastructure.Runtime;
using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using OLED_Sleeper.Storage;
using OLED_Sleeper.Storage.Interfaces;
using OLED_Sleeper.UI.Services;
using OLED_Sleeper.UI.Services.Interfaces;
using Serilog;
using System;
using System.Linq;
using System.Windows;

namespace OLED_Sleeper.Infrastructure.Hosting
{
    /// <summary>
    /// Handles application startup, dependency injection, single-instance enforcement, orchestrator startup, and shutdown logic.
    /// Keeps <see cref="Application"/> subclasses lightweight and focused on WPF lifecycle events.
    /// </summary>
    public class ApplicationBootstrapper(string[] args) : IDisposable
    {
        /// <summary>
        /// How long shutdown waits for the monitor restore to finish.
        /// </summary>
        private const int RestoreOnShutdownTimeoutMs = 10000;

        /// <summary>The title of the dialog shown when the storage root cannot be written to.</summary>
        private const string StorageErrorCaption = ProductIdentity.DisplayName;

        private readonly ApplicationOptions _applicationOptions = CommandLineHelper.ParseArguments(args);

        /// <summary>
        /// Ends the process. Created here rather than resolved, since shutdown has to work both
        /// before the container is built and after it has been disposed.
        /// </summary>
        private readonly IApplicationShutdown _applicationShutdown = new ApplicationShutdown();

        /// <summary>
        /// Reaches the UI thread. Created here for the same reason as <see cref="_applicationShutdown"/>.
        /// </summary>
        private readonly IDispatcher _dispatcher = new ApplicationDispatcher();

        private IStorageRoot? _storageRoot;
        private IServiceProvider? _serviceProvider;
        private ITrayIconService? _trayIconService;
        private IMainWindowService? _mainWindowService;
        private IApplicationInstanceManager? _instanceManager;
        private IApplicationOrchestrator? _orchestrator;
        private IUnsavedSettingsService? _unsavedSettingsService;
        private WellbeingCoordinator? _wellbeing;
        private bool _isExiting = false;
        private bool _interactiveExit;

        /// <summary>
        /// Initializes the application: storage, logging, single-instance, DI, orchestrator, main window, and tray icon.
        /// A second instance stops after the single-instance check and starts no services.
        /// </summary>
        public void Initialize()
        {
            if (!TryPrepareStorage())
            {
                return;
            }

            LoggingConfigurator.Configure(_storageRoot!.DirectoryPath);
            InitializeInstanceManager();

            if (!_instanceManager!.IsFirstInstance)
            {
                Log.Information("Another instance is already running. Exiting without starting any services.");
                return;
            }

            ConfigureServices();
            _serviceProvider!.GetRequiredService<UiPreferencesService>().Load();
            StartOrchestrator();

            SetupMainWindowService();
            _wellbeing = _serviceProvider!.GetRequiredService<WellbeingCoordinator>();
            _wellbeing.Start();
            SetupTrayIconService();
            HookInstanceManagerShowWindow();
        }

        /// <summary>
        /// Resolves the storage root, creates it and confirms it is writable, reporting the failure and ending
        /// the process when it is not. Runs before logging, whose file sink writes into the same directory and
        /// reports nothing of its own when it cannot.
        /// </summary>
        /// <returns><c>true</c> when startup can continue; otherwise, <c>false</c>.</returns>
        private bool TryPrepareStorage()
        {
            var fileSystem = new FileSystem();
            _storageRoot = new StorageRoot(fileSystem, BuildMode.IsPortable);

            if (new StorageRootPreparer(fileSystem, _storageRoot).TryPrepare(out var error))
            {
                return true;
            }

            var guidance = BuildMode.IsPortable
                ? "请将 ScreenKeeper 文件夹移到可写入的位置（如桌面），然后重新启动。"
                : "请检查当前用户的数据目录是否可访问，然后重新启动多屏管家。";

            new MessageBoxDialogService(new MainWindowAccessor()).ShowError($"{error}{Environment.NewLine}{Environment.NewLine}{guidance}", StorageErrorCaption);
            _applicationShutdown.Shutdown();
            return false;
        }

        /// <summary>
        /// Initializes the single-instance manager before any other services.
        /// Its seams are constructed here because the check runs before the container exists.
        /// </summary>
        private void InitializeInstanceManager()
        {
            _instanceManager = new ApplicationInstanceManager(_dispatcher, _applicationShutdown);
            _instanceManager.Initialize();
        }

        /// <summary>
        /// Configures dependency injection services and builds the service provider using <see cref="ServiceConfigurator"/>.
        /// </summary>
        private void ConfigureServices()
        {
            _serviceProvider = ServiceConfigurator.ConfigureServices(_applicationOptions, _storageRoot!);
        }

        /// <summary>
        /// Starts the application orchestrator service.
        /// </summary>
        private void StartOrchestrator()
        {
            if (_serviceProvider != null)
            {
                _orchestrator = _serviceProvider.GetRequiredService<IApplicationOrchestrator>();
                _orchestrator.Start();
            }
        }

        /// <summary>
        /// Sets up the main window service and its data context.
        /// </summary>
        private void SetupMainWindowService()
        {
            if (_serviceProvider == null) return;
            _mainWindowService = _serviceProvider.GetRequiredService<IMainWindowService>();
            _mainWindowService.SetupMainWindow();
        }

        /// <summary>
        /// Configures and displays the tray icon using the tray icon service.
        /// </summary>
        private void SetupTrayIconService()
        {
            if (_serviceProvider == null) return;
            _unsavedSettingsService = _serviceProvider.GetRequiredService<IUnsavedSettingsService>();
            _trayIconService = _serviceProvider.GetRequiredService<ITrayIconService>();
            _trayIconService.Initialize(
                () => _mainWindowService?.ShowMainWindow(),
                RequestExit
            );
        }

        /// <summary>
        /// Handles the tray menu's Exit. The work is queued behind the menu's own input, so the click that
        /// picked Exit cannot answer the prompt that follows it.
        /// </summary>
        private void RequestExit() => _ = _dispatcher.InvokeAfterInputAsync(ConfirmAndShutdown);

        /// <summary>
        /// Shuts down unless the user cancels over unsaved settings. Only the tray exit asks; <c>OnExit</c>
        /// and <c>SessionEnding</c> shut down without a prompt.
        /// </summary>
        private void ConfirmAndShutdown()
        {
            if (_isExiting) return;
            if (_unsavedSettingsService?.ConfirmExit() == false)
            {
                Log.Information("Exit cancelled. Unsaved settings were kept.");
                return;
            }

            _interactiveExit = true;
            ShutdownApp();
        }

        /// <summary>
        /// Hooks up the delegate for showing the main window after DI and services are ready.
        /// </summary>
        private void HookInstanceManagerShowWindow()
        {
            _instanceManager?.SetShowMainWindowAction(() => _mainWindowService?.ShowMainWindow());
        }

        /// <summary>
        /// Tells the main window the application is exiting, stops the orchestrator, disposes the tray icon
        /// and the instance manager, flushes the log, and exits. A second instance has no orchestrator and
        /// restores nothing.
        /// </summary>
        public void ShutdownApp()
        {
            if (_isExiting) return; // Prevent re-entrancy
            _isExiting = true;

            _mainWindowService?.PrepareForShutdown();
            _wellbeing?.StopPresentation();

            // The UI is still available here. Remove topmost covers before stopping wake detection
            // or opening a recovery dialog, and reject any already queued presentation requests.
            (_serviceProvider?.GetService<IMonitorPrivacyService>() as IDisposable)?.Dispose();
            _serviceProvider?.GetService<IMonitorBlackoutService>()?.Stop();
            Log.Information("Shutdown screen overlays cleared.");

            StopOrchestrator();
            if (_interactiveExit && _wellbeing?.Color.States.Any(s => s.State is Features.ColorTemperature.ColorControlState.RecoveryPending or Features.ColorTemperature.ColorControlState.Failed or Features.ColorTemperature.ColorControlState.Conflict) == true)
            {
                Log.Warning("Showing shutdown color recovery notice after closing screen overlays.");
                new UI.Views.AppDialog(UI.Localization.UiText.Get("部分屏幕的原色恢复尚未完成，恢复记录已保留。下次启动请检查色温页面。"), UI.Localization.UiText.Get("色温调节"), false)
                { ShowInTaskbar = true, WindowStartupLocation = WindowStartupLocation.CenterScreen }.ShowDialog();
            }

            _trayIconService?.Dispose();
            _instanceManager?.Dispose();

            Log.Information("--- Application Exiting ---");
            Log.CloseAndFlush();

            _applicationShutdown.Shutdown();
        }

        /// <summary>
        /// Stops the orchestrator and blocks until the monitor restore finishes or
        /// <see cref="RestoreOnShutdownTimeoutMs"/> elapses.
        /// </summary>
        /// <remarks>
        /// The restore runs on the thread pool, since the UI thread is no longer processing dispatcher
        /// messages by the time <c>OnExit</c> reaches this. On timeout the brightness state file is left
        /// unchanged and the next launch restores the monitor.
        /// </remarks>
        private void StopOrchestrator()
        {
            if (_orchestrator == null) return;

            Log.Information("Shutdown initiated. Restoring all monitors...");

            try
            {
                var stopTask = Task.Run(async () =>
                {
                    await Task.WhenAll(_orchestrator.StopAsync(), _wellbeing?.RestoreColorAsync() ?? Task.CompletedTask);
                });
                if (!stopTask.Wait(RestoreOnShutdownTimeoutMs))
                {
                    Log.Warning("Monitor restore did not finish within {TimeoutMs} ms. Exiting anyway.", RestoreOnShutdownTimeoutMs);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to stop the orchestrator during shutdown.");
            }
        }

        /// <summary>
        /// Disposes resources used by the bootstrapper.
        /// </summary>
        public void Dispose()
        {
            _trayIconService?.Dispose();
            _instanceManager?.Dispose();
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }
}
