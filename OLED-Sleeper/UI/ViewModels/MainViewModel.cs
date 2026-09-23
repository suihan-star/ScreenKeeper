using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Infrastructure;
using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using OLED_Sleeper.UI.Commands;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Services;
using OLED_Sleeper.UI.Services.Interfaces;
using Serilog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using ICommand = System.Windows.Input.ICommand;

namespace OLED_Sleeper.UI.ViewModels
{
    /// <summary>
    /// The main ViewModel for the application's main window.
    /// It orchestrates the various services and manages the overall state of the UI.
    /// </summary>
    public partial class MainViewModel : ViewModelBase, IUnsavedSettings
    {
        #region Private Fields

        /// <summary>
        /// Service for building and managing the monitor workspace layout.
        /// </summary>
        private readonly IWorkspaceService _workspaceService;

        /// <summary>
        /// Validates and saves the monitor settings.
        /// </summary>
        private readonly IMonitorSettingsSaveService _saveService;

        /// <summary>
        /// Runs actions on the UI thread.
        /// </summary>
        private readonly IDispatcher _dispatcher;

        /// <summary>
        /// The width of the container used for monitor layout calculations.
        /// </summary>
        private double _containerWidth;

        /// <summary>
        /// The height of the container used for monitor layout calculations.
        /// </summary>
        private double _containerHeight;

        /// <summary>
        /// The currently selected monitor in the UI.
        /// </summary>
        private MonitorLayoutViewModel? _selectedMonitor;

        /// <summary>
        /// Indicates whether any monitor settings have unsaved changes.
        /// </summary>
        private bool _isDirty;

        /// <summary>
        /// The text displayed in the main window's title bar.
        /// </summary>
        private string _windowTitle = UiText.Get(ProductIdentity.SettingsTitle);

        /// <summary>
        /// The text displayed on the save button.
        /// </summary>
        private string _saveButtonText = "保存全部设置";

        /// <summary>
        /// Indicates whether the workspace is currently loading.
        /// </summary>
        private bool _isLoading;

        /// <summary>
        /// Incremented for every workspace build. A build whose number is no longer current has been
        /// superseded and its result is discarded.
        /// </summary>
        private int _buildGeneration;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets or sets the currently selected monitor in the layout view. Updates selection state and notifies property changes.
        /// </summary>
        public MonitorLayoutViewModel? SelectedMonitor
        {
            get => _selectedMonitor;
            set
            {
                if (_selectedMonitor != null) { _selectedMonitor.IsSelected = false; }
                _selectedMonitor = value;
                if (!IsLoading && value != null && !IsScreenPage) CurrentModule = "Displays";
                if (_selectedMonitor != null) { _selectedMonitor.IsSelected = true; }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMonitorSelected));
                RefreshSnooze();
            }
        }

        /// <summary>
        /// Returns true if a monitor is currently selected in the UI.
        /// </summary>
        public bool IsMonitorSelected => SelectedMonitor != null;

        /// <summary>
        /// Gets or sets a value indicating whether any monitor settings have been changed and not saved.
        /// Updates the window title to reflect unsaved changes.
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty || Health?.IsDirty == true || Color?.IsDirty == true;
            set
            {
                if (_isDirty == value) return;
                _isDirty = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                WindowTitle = UiText.Get(ProductIdentity.SettingsTitle) + (IsDirty ? "*" : "");
            }
        }

        /// <summary>
        /// Gets or sets the text for the main window's title bar. Includes a '*' when changes are unsaved.
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the text for the save button (e.g., "保存设置" or "已保存").
        /// </summary>
        public string SaveButtonText
        {
            get => UiText.Get(_saveButtonText == "已保存" ? _saveButtonText : CurrentModule switch { "Health" => "保存健康提醒", "Color" => "保存色温设置", "Privacy" => "保存隐私保护", _ => "保存屏幕休息" });
            set { _saveButtonText = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The collection of monitor view models to be displayed in the layout view.
        /// </summary>
        public ObservableCollection<MonitorLayoutViewModel> Monitors { get; } = new ObservableCollection<MonitorLayoutViewModel>();

        /// <summary>
        /// Gets or sets a value indicating whether the workspace is currently loading.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string _workspaceMessage = string.Empty;

        /// <summary>Explains an empty layout or failed refresh without exposing exception details.</summary>
        public string WorkspaceMessage
        {
            get => UiText.Get(_workspaceMessage);
            private set { _workspaceMessage = value; OnPropertyChanged(); }
        }

        #endregion Public Properties

        private readonly UiPreferencesService? _preferences;
        private readonly IDialogService? _dialogs;
        private readonly IMonitorPrivacyService? _privacy;
        public ICommand ProtectPrivacyCommand { get; }
        public ICommand ProtectSelectedPrivacyCommand { get; }
        public ICommand PausePrivacyCommand { get; }
        public string PrivacyStatus => UiText.Get(_privacy?.GetStatus(SelectedMonitor?.HardwareId) ?? "保存并启用本屏隐私保护后，可使用立即保护和暂缓。");
        public string PausePrivacyText => _snooze == null ? UiText.Get(SelectedMonitor != null && _privacy?.IsPaused(SelectedMonitor.HardwareId) == true ? "恢复自动保护" : "暂缓…") : PauseText(Features.UserSettings.Models.MonitorSettingsScope.Privacy);
        public string PrivacyRuntimeNotice
        {
            get
            {
                if (SelectedMonitor == null || _privacy?.IsConfigured(SelectedMonitor.HardwareId) != true) return "";
                var paused = SnoozeNotice(Features.UserSettings.Models.MonitorSettingsScope.Privacy);
                if (paused.Length > 0) return paused;
                var status = _privacy.GetStatus(SelectedMonitor.HardwareId);
                return status == "按本屏实际操作独立计时。" ? "" : UiText.Get(status);
            }
        }
        private void RefreshPrivacyStatus()
        {
            OnPropertyChanged(nameof(PrivacyRuntimeTitle));
            OnPropertyChanged(nameof(PrivacyRuntimeDetail));
            OnPropertyChanged(nameof(PrivacyRuntimeError));
            OnPropertyChanged(nameof(PrivacyStatus));
            OnPropertyChanged(nameof(PrivacyRuntimeNotice));
            OnPropertyChanged(nameof(PausePrivacyText));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
        public string Language
        {
            get => UiText.Current.Language;
            set
            {
                if (value == Language) return;
                if (_preferences == null) UiText.Current.SetLanguage(value);
                else if (!_preferences.TrySetLanguage(value))
                {
                    _dialogs?.ShowError(UiText.Get("语言偏好未能保存，请检查配置目录的写入权限后重试。"), UiText.Get("设置未能保存"));
                    OnPropertyChanged();
                }
            }
        }
        public string? ConfigurationError => CurrentModule switch { "Health" => Health?.Error, "Color" => Color?.Error, _ => UI.Helpers.MonitorSettingsValidator.BuildValidationError(Monitors, ScreenScope) };
        public string StatusText => UiText.Get(CurrentModuleDirty ? "有未保存的更改" : "设置已保存");
        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            ModulesChanged();
            RefreshPrivacyStatus();
            OnPropertyChanged(nameof(ConfigurationError));
            WindowTitle = UiText.Get(ProductIdentity.SettingsTitle) + (IsDirty ? "*" : "");
            foreach (var name in new[] { nameof(Language), nameof(StatusText), nameof(SaveButtonText), nameof(WorkspaceMessage) }) OnPropertyChanged(name);
        }

        #region Commands

        /// <summary>
        /// Command to refresh the list of monitors from the system and update the UI.
        /// </summary>
        public ICommand ReloadMonitorsCommand { get; }

        /// <summary>
        /// Command to select a specific monitor from the layout view.
        /// </summary>
        public ICommand SelectMonitorCommand { get; }

        /// <summary>
        /// Command to validate and save all current monitor settings.
        /// </summary>
        public ICommand SaveSettingsCommand { get; }

        /// <summary>
        /// Command to discard any unsaved changes and reload monitor settings.
        /// </summary>
        public ICommand DiscardChangesCommand { get; }

        #endregion Commands

        #region Constructor

        public MainViewModel(
            IWorkspaceService workspaceService,
            IMonitorSettingsSaveService saveService,
            IDispatcher dispatcher,
            UiPreferencesService? preferences = null,
            IDialogService? dialogs = null,
            IMonitorPrivacyService? privacy = null,
            PrivacyImageStore? images = null, IApplicationWindowAccess? applicationWindows = null, IScreenApplicationService? applications = null, IScreenDesktopService? desktop = null,
            Infrastructure.Hosting.WellbeingCoordinator? wellbeing = null, HealthSettingsViewModel? healthSettings = null, ColorSettingsViewModel? colorSettings = null, Themes.UiTheme? theme = null, Features.MonitorIdleDetection.Services.ScreenSnoozeService? snooze = null)
        {
            Monitors.CollectionChanged += (_, _) => SetCompactLayout(_compactScreens);
            _workspaceService = workspaceService;
            _saveService = saveService;
            _dispatcher = dispatcher;
            _preferences = preferences;
            _theme = theme ?? Themes.UiTheme.Current;
            _dialogs = dialogs;
            _privacy = privacy;
            InitializeSnooze(snooze);
            Health = wellbeing?.HealthSettings ?? healthSettings; Color = wellbeing?.ColorSettings ?? colorSettings;
            if (Health != null) Health.PropertyChanged += (_, _) => ModulesChanged();
            if (Color != null) Color.PropertyChanged += (_, _) => ModulesChanged();
            SelectModuleCommand = new RelayCommand(p => CurrentModule = p as string ?? "Displays");
            Images = images; ApplicationWindows = applicationWindows; Applications = applications; Desktop = desktop;
            if (_privacy != null) _privacy.Changed += RefreshPrivacyStatus;
            ProtectPrivacyCommand = new AsyncRelayCommand(() => _privacy!.ProtectAllAsync(), () => _privacy?.HasConfiguredScreens == true);
            ProtectSelectedPrivacyCommand = new AsyncRelayCommand(() => SelectedMonitor is { } monitor ? _privacy!.ProtectScreenAsync(monitor.HardwareId) : Task.CompletedTask,
                () => SelectedMonitor is { } monitor && _privacy?.IsConfigured(monitor.HardwareId) == true && !_privacy.IsProtected(monitor.HardwareId));
            PausePrivacyCommand = new RelayCommand(PauseSelected, () => SelectedMonitor != null && _privacy?.IsConfigured(SelectedMonitor.HardwareId) == true);
            PropertyChangedEventManager.AddHandler(UiText.Current, OnLanguageChanged, string.Empty);

            PropertyChangedEventManager.AddHandler(_theme, (_, _) => { OnPropertyChanged(nameof(Theme)); OnPropertyChanged(nameof(ThemeIcon)); }, string.Empty);
            SelectMonitorCommand = new RelayCommand(ExecuteSelectMonitor);
            ReloadMonitorsCommand = new RelayCommand(() => RefreshMonitors(false), () => !IsDirty && !IsLoading);
            SaveSettingsCommand = new AsyncRelayCommand(ExecuteSaveSettings, () => CurrentModuleDirty && !IsLoading && ConfigurationError == null);
            DiscardChangesCommand = new RelayCommand(DiscardCurrentModule, () => CurrentModuleDirty);
        }

        #endregion Constructor

        #region Public Methods (for View Interaction)

        /// <summary>
        /// Initiates a full refresh of the monitor list, re-scanning the display set before rebuilding the layout.
        /// </summary>
        /// <param name="preserveSelection">Whether to reselect the current monitor once the rebuild finishes.</param>
        public void RefreshMonitors(bool preserveSelection)
        {
            if (IsDirty || IsLoading) return;
            ApplyWorkspace(_workspaceService.RefreshWorkspaceAsync, _containerWidth, _containerHeight, preserveSelection);
        }

        /// <summary>
        /// Recalculates the monitor layout based on a new container size, preserving the current selection if possible.
        /// </summary>
        /// <param name="width">The new width of the container.</param>
        /// <param name="height">The new height of the container.</param>
        public void RecalculateLayout(double width, double height)
        {
            ApplyWorkspace(_workspaceService.BuildWorkspaceAsync, width, height, preserveSelection: true);
        }

        /// <inheritdoc />
        public bool TrySaveChanges()
        {
            if ((_isDirty || (Health == null && Color == null)) && !_saveService.TrySave(Monitors)) return false;
            if (Health?.IsDirty == true && !Health.TrySave()) return false;
            if (Color?.IsDirty == true && !Color.TrySave()) return false;

            CheckDirtyState();
            return true;
        }

        /// <inheritdoc />
        public void DiscardChanges()
        {
            Health?.Discard(); Color?.Discard();
            foreach (var monitor in Monitors)
            {
                monitor.Configuration.Revert();
            }

            CheckDirtyState();
        }

        #endregion Public Methods (for View Interaction)

        #region Command Handlers

        /// <summary>
        /// Handles the selection of a monitor from the UI.
        /// </summary>
        /// <param name="parameter">The monitor to select.</param>
        private void ExecuteSelectMonitor(object? parameter)
        {
            if (parameter is MonitorLayoutViewModel monitor) { if (!IsScreenPage) CurrentModule = "Displays"; SelectedMonitor = monitor; }
        }

        /// <summary>
        /// Saves the monitor settings and reports the outcome on the save button.
        /// </summary>
        private async Task ExecuteSaveSettings()
        {
            bool saved = CurrentModule switch { "Health" => Health?.TrySave() == true, "Color" => Color?.TrySave() == true, _ => _saveService.TrySave(Monitors, ScreenScope) };
            CheckDirtyState(); ModulesChanged();
            if (!saved) return;

            await ProvideSaveFeedbackAsync();
        }

        #endregion Command Handlers

        #region Private Helper Methods

        // --- Save Process Helpers ---

        /// <summary>
        /// Provides user feedback after saving settings by updating the save button text temporarily.
        /// </summary>
        private async Task ProvideSaveFeedbackAsync()
        {
            SaveButtonText = "已保存";
            await Task.Delay(2000);
            SaveButtonText = "保存全部设置";
        }

        // --- Monitor Update Helpers ---

        /// <summary>
        /// Builds the workspace and applies the result to the UI. A build superseded by a later one
        /// leaves the monitor list untouched. Failures are logged; nothing is thrown to the caller.
        /// </summary>
        /// <param name="build">Produces the layout view models for the given container size.</param>
        /// <param name="width">The width of the container for layout.</param>
        /// <param name="height">The height of the container for layout.</param>
        /// <param name="preserveSelection">Whether to reselect the current monitor once the build finishes.</param>
        private async void ApplyWorkspace(
            Func<double, double, Task<ObservableCollection<MonitorLayoutViewModel>>> build,
            double width,
            double height,
            bool preserveSelection)
        {
            if (width <= 0 || height <= 0) return;
            _containerWidth = width;
            _containerHeight = height;

            var generation = ++_buildGeneration;
            var monitorIdToRestore = preserveSelection ? SelectedMonitor?.HardwareId : null;
            WorkspaceMessage = "正在检测显示器…";
            IsLoading = true;

            try
            {
                var newMonitorLayoutViewModels = await build(width, height);
                if (generation != _buildGeneration) return;

                foreach (var monitor in newMonitorLayoutViewModels)
                {
                    var previous = Monitors.FirstOrDefault(m => m.HardwareId == monitor.HardwareId);
                    if (previous?.IsDirty == true) monitor.Configuration.PreserveDraftFrom(previous.Configuration);
                    else if (previous != null) monitor.Configuration.RetainWindowIdentityFrom(previous.Configuration);
                }
                PopulateMonitors(newMonitorLayoutViewModels);
                RestoreSelection(monitorIdToRestore);
                CheckDirtyState();
                WorkspaceMessage = Monitors.Count == 0 ? "未检测到显示器，请检查连接后点击右上角重新检测。" : string.Empty;
                IsLoading = false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to build the monitor workspace.");
                if (generation == _buildGeneration)
                {
                    WorkspaceMessage = "显示器加载失败，请点击右上角重新检测。";
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// Populates the Monitors collection with new view models and wires up dirty state change notifications.
        /// </summary>
        /// <param name="newViewModels">The new monitor layout view models.</param>
        private void PopulateMonitors(ObservableCollection<MonitorLayoutViewModel> newViewModels)
        {
            if (!_dispatcher.IsOnUiThread)
            {
                _dispatcher.Invoke(() => PopulateMonitors(newViewModels));
                return;
            }

            foreach (var viewModel in Monitors)
            {
                viewModel.PropertyChanged -= OnMonitorPropertyChanged;
            }

            Monitors.Clear();
            foreach (var viewModel in newViewModels)
            {
                viewModel.PropertyChanged += OnMonitorPropertyChanged;
                Monitors.Add(viewModel);
            }
        }

        /// <summary>
        /// Re-evaluates the overall dirty state whenever one monitor's changes.
        /// </summary>
        private void OnMonitorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MonitorLayoutViewModel.IsDirty))
            {
                CheckDirtyState();
            }
        }

        /// <summary>
        /// Restores the monitor selection based on a hardware ID.
        /// </summary>
        /// <param name="hardwareId">The monitor to reselect, or null to clear the selection.</param>
        private void RestoreSelection(string? hardwareId)
        {
            SelectedMonitor = hardwareId != null
                ? Monitors.FirstOrDefault(m => m.HardwareId == hardwareId)
                : Monitors.FirstOrDefault();
        }

        /// <summary>
        /// Checks if any monitor is dirty and updates the IsDirty property accordingly.
        /// </summary>
        private void CheckDirtyState()
        {
            IsDirty = Monitors.Any(m => m.IsDirty);
            OnPropertyChanged(nameof(ConfigurationError));
            ModulesChanged();
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        #endregion Private Helper Methods
    }
}
