using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Models;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace OLED_Sleeper.UI.ViewModels
{
    /// <summary>
    /// ViewModel for configuring an individual monitor's behavior and settings.
    /// Handles state management, validation, and dirty tracking for the monitor configuration UI.
    /// </summary>
    public partial class MonitorConfigurationViewModel : ViewModelBase, IDataErrorInfo
    {
        /// <summary>
        /// The underlying monitor information model.
        /// </summary>
        private readonly MonitorInfo _monitorInfo;

        #region Properties

        /// <summary>
        /// The display title for the monitor, including primary indicator if applicable. A duplicated surface
        /// is titled the way Windows titles it, such as <c>Monitor 1|2</c>.
        /// </summary>
        public string MonitorTitle => MonitorTitleFormatter.Format(_monitorInfo);

        public string IdleValueError => this["IdleValue"];

        public string ActiveConditionsError => this["IsActiveOnInput"];

        // --- IsManaged ---
        private bool _isManaged;

        private bool _initialIsManaged;

        /// <summary>
        /// Gets or sets whether this monitor is managed by the application.
        /// </summary>
        public bool IsManaged
        {
            get => _isManaged;
            set
            {
                _isManaged = value;
                OnPropertyChanged(nameof(RestEnabled));
                OnPropertyChanged(nameof(ManagementText));
                OnPropertyChanged(nameof(PrivacyError));
                OnPropertyChanged();
                OnPropertyChanged(nameof(IdleValueError));
                OnPropertyChanged(nameof(ActiveConditionsError));
                OnPropertyChanged(nameof(BehaviorError)); // Ensure all errors update
                RefreshPrivacy();
                UpdateDirtyState();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the "Dim" behavior option should be enabled.
        /// </summary>
        public bool IsDimBehaviorEnabled => _monitorInfo.Capabilities?.IsSupported == true;

        /// <summary>
        /// Gets the tooltip for the entire Behavior section, changing based on DDC/CI support.
        /// </summary>
        public string BehaviorSectionTooltip
        {
            get
            {
                string baseTooltip = UiText.Get("选择显示器达到空闲时长后执行的操作。\n\n") +
                                    UiText.Get("• 遮黑：用黑色遮罩覆盖屏幕，不关闭显示器电源。\n") +
                                     UiText.Get("• 调暗：通过 DDC/CI 降低显示器亮度。");

                if (_monitorInfo.Capabilities?.IsSupported != true)
                {
                    baseTooltip = UiText.Get("当前显示器不支持 DDC/CI 调暗。\n\n") + baseTooltip;
                }

                return baseTooltip;
            }
        }

        // --- Behavior ---
        private MonitorBehaviorType _behavior;

        private MonitorBehaviorType _initialBehavior;

        /// <summary>
        /// Gets or sets the behavior for this monitor (e.g., Dim, Blackout).
        /// </summary>
        public MonitorBehaviorType Behavior
        {
            get => _behavior;
            set
            {
                _behavior = value;
                if (_behavior == MonitorBehaviorType.Blackout) { DimLevel = 0; }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDimSliderEnabled));
                OnPropertyChanged(nameof(IsSleepEnabled));
                OnPropertyChanged(nameof(IsBlackout));
                OnPropertyChanged(nameof(IdleValueError));
                OnPropertyChanged(nameof(ActiveConditionsError));
                OnPropertyChanged(nameof(IsLowerBrightnessOptionEnabled));
                OnPropertyChanged(nameof(LowerBrightnessTooltip));
                OnPropertyChanged(nameof(BehaviorError));
                UpdateDirtyState();
            }
        }

        /// <summary>
        /// Returns an error message if the behavior is invalid.
        /// </summary>
        public string BehaviorError => this["Behavior"];

        // --- DimLevel ---
        private double _dimLevel;

        private double _initialDimLevel;

        /// <summary>
        /// Gets or sets the dimming level for the monitor (0-100).
        /// </summary>
        public double DimLevel
        {
            get => _dimLevel;
            set { _dimLevel = value; OnPropertyChanged(); UpdateDirtyState(); }
        }

        /// <summary>
        /// Returns true if the dim slider should be enabled (only for Dim behavior).
        /// </summary>
        public bool IsDimSliderEnabled => Behavior == MonitorBehaviorType.Dim && IsDimBehaviorEnabled;

        // --- LowerBrightnessOnBlackout ---
        private bool _lowerBrightnessOnBlackout;

        private bool _initialLowerBrightnessOnBlackout;

        /// <summary>
        /// Gets or sets whether a blackout also sets this monitor's hardware brightness to zero.
        /// Always false on a monitor without DDC/CI support.
        /// </summary>
        public bool LowerBrightnessOnBlackout
        {
            get => _lowerBrightnessOnBlackout;
            set
            {
                _lowerBrightnessOnBlackout = value;
                OnPropertyChanged();
                UpdateDirtyState();
            }
        }

        /// <summary>
        /// Returns true if the lower-brightness option should be enabled (only for Blackout behavior on a monitor that supports DDC/CI).
        /// </summary>
        public bool IsLowerBrightnessOptionEnabled => Behavior == MonitorBehaviorType.Blackout && IsDimBehaviorEnabled;

        /// <summary>
        /// Gets the tooltip for the lower-brightness option, prefixed with the reason when the option is disabled.
        /// </summary>
        public string LowerBrightnessTooltip
        {
            get
            {
                string baseTooltip = UiText.Get("在黑色遮罩覆盖屏幕的同时，\n") +
                                     UiText.Get("将显示器自身亮度降至零。\n\n") +
                                     UiText.Get("适用于非 OLED 显示器，可降低黑色遮罩\n") +
                                     UiText.Get("背后仍然亮着的背光。");

                if (!IsDimBehaviorEnabled)
                    return UiText.Get("当前显示器不支持 DDC/CI 调暗。\n\n") + baseTooltip;

                if (Behavior != MonitorBehaviorType.Blackout)
                    return UiText.Get("仅在选择“遮黑”时可用。\n\n") + baseTooltip;

                return baseTooltip;
            }
        }

        // --- Idle Timer ---
        /// <summary>
        /// List of available time units for idle timer.
        /// </summary>
        public List<TimeUnit> TimeUnits { get; } = Enum.GetValues(typeof(TimeUnit)).Cast<TimeUnit>().ToList();

        private string? _idleValueText;
        public string IdleValueText
        {
            get => _idleValueText ?? IdleValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            set
            {
                IdleValue = int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
                _idleValueText = value;
                OnPropertyChanged();
            }
        }
        public string ManagementText => UiText.Get(IsManaged ? "已启用管理" : "未启用管理");
        public string CapabilityText => UiText.Get(IsDimBehaviorEnabled ? "单独设置，仅影响这块屏幕" : "此显示器不支持 DDC/CI 亮度调节");
        public string SelectedTimeUnitText => UiText.Get(SelectedTimeUnit switch { TimeUnit.Seconds => "秒", TimeUnit.Hours => "小时", _ => "分钟" });
        public void RefreshLanguage()
        {
            OptionsChanged();
            foreach (var name in new[] { nameof(SavedRule), nameof(MonitorTitle), nameof(ManagementText), nameof(CapabilityText), nameof(SelectedTimeUnitText), nameof(BehaviorSectionTooltip), nameof(LowerBrightnessTooltip), nameof(IdleValueError), nameof(ActiveConditionsError), nameof(BehaviorError) }) OnPropertyChanged(name);
        }
        private int? _idleValue;
        private int? _initialIdleValue;

        /// <summary>
        /// Gets or sets the idle time value before the monitor behavior triggers.
        /// </summary>
        public int? IdleValue
        {
            get => _idleValue;
            set
            {
                _idleValue = value;
                _idleValueText = null;
                OnPropertyChanged(nameof(IdleValueText));
                OnPropertyChanged();
                OnPropertyChanged(nameof(IdleValueError));
                UpdateDirtyState();
            }
        }

        private TimeUnit _selectedTimeUnit;
        private TimeUnit _initialSelectedTimeUnit;

        /// <summary>
        /// Gets or sets the selected time unit for the idle timer.
        /// </summary>
        public TimeUnit SelectedTimeUnit
        {
            get => _selectedTimeUnit;
            set { _selectedTimeUnit = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedTimeUnitText)); OnPropertyChanged(nameof(IdleValueError)); UpdateDirtyState(); }
        }

        // --- Active Conditions ---
        private bool _isActiveOnInput;

        private bool _initialIsActiveOnInput;

        /// <summary>
        /// Gets or sets whether keyboard/mouse input keeps the monitor active.
        /// </summary>
        public bool IsActiveOnInput
        {
            get => _isActiveOnInput;
            set
            {
                _isActiveOnInput = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveConditionsError));
                UpdateDirtyState();
            }
        }

        private bool _isActiveOnMousePosition;
        private bool _initialIsActiveOnMousePosition;

        /// <summary>
        /// Gets or sets whether mouse position keeps the monitor active.
        /// </summary>
        public bool IsActiveOnMousePosition
        {
            get => _isActiveOnMousePosition;
            set
            {
                _isActiveOnMousePosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveConditionsError));
                UpdateDirtyState();
            }
        }

        private bool _isActiveOnActiveWindow;
        private bool _initialIsActiveOnActiveWindow;

        /// <summary>
        /// Gets or sets whether the active window keeps the monitor active.
        /// </summary>
        public bool IsActiveOnActiveWindow
        {
            get => _isActiveOnActiveWindow;
            set
            {
                _isActiveOnActiveWindow = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveConditionsError));
                UpdateDirtyState();
            }
        }

        #endregion Properties

        /// <param name="monitorInfo">The monitor information model.</param>
        public MonitorConfigurationViewModel(MonitorInfo monitorInfo)
        {
            _monitorInfo = monitorInfo;
            PropertyChangedEventManager.AddHandler(UiText.Current, (_, _) => RefreshLanguage(), string.Empty);

            ApplySettings(new MonitorSettings());
            MarkAsSaved();
        }

        #region State Management (Dirty Tracking, Saving, Loading)

        /// <summary>
        /// Gets whether the configuration has unsaved changes.
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// Updates the IsDirty property based on whether any tracked property has changed from its initial value.
        /// Uses a tolerance for floating point comparison.
        /// </summary>
        private void UpdateDirtyState()
        {
            const double epsilon = 0.0001;
            IsDirty = !Options.SameAs(_initialOptions) || PrivacyMode != _initialPrivacyMode || PrivacyIdleSeconds != _initialPrivacySeconds || IsManaged != _initialIsManaged ||
                       Behavior != _initialBehavior ||
                       Math.Abs(DimLevel - _initialDimLevel) > epsilon ||
                       LowerBrightnessOnBlackout != _initialLowerBrightnessOnBlackout ||
                       IdleValue != _initialIdleValue ||
                       SelectedTimeUnit != _initialSelectedTimeUnit ||
                       IsActiveOnInput != _initialIsActiveOnInput ||
                       IsActiveOnMousePosition != _initialIsActiveOnMousePosition ||
                       IsActiveOnActiveWindow != _initialIsActiveOnActiveWindow;

            OnPropertyChanged(nameof(IsRestDirty));
            OnPropertyChanged(nameof(IsPrivacyDirty));
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(IsValid));
        }

        /// <summary>
        /// Marks the current state as saved, updating the initial values for dirty tracking.
        /// </summary>
        public void PreserveDraftFrom(MonitorConfigurationViewModel source)
        {
            ApplySettings(source.ToSettings());
            _initialOptions = source._initialOptions.Copy();
            _idleValueText = source._idleValueText;
            _privacyIdleText = source._privacyIdleText;
            OnPropertyChanged(nameof(PrivacyIdleText));
            _initialPrivacyMode = source._initialPrivacyMode;
            _initialPrivacySeconds = source._initialPrivacySeconds;
            _initialIsManaged = source._initialIsManaged;
            _initialBehavior = source._initialBehavior;
            _initialDimLevel = source._initialDimLevel;
            _initialLowerBrightnessOnBlackout = source._initialLowerBrightnessOnBlackout;
            _initialIdleValue = source._initialIdleValue;
            _initialSelectedTimeUnit = source._initialSelectedTimeUnit;
            _initialIsActiveOnInput = source._initialIsActiveOnInput;
            _initialIsActiveOnMousePosition = source._initialIsActiveOnMousePosition;
            _initialIsActiveOnActiveWindow = source._initialIsActiveOnActiveWindow;
            RefreshPrivacy();
            UpdateDirtyState();
            OnPropertyChanged(nameof(IdleValueText));
        }

        public void MarkAsSaved(MonitorSettingsScope scope = MonitorSettingsScope.All)
        {
            if (scope != MonitorSettingsScope.Rest)
            {
                _initialOptions = Options.Copy();
                _initialPrivacyMode = PrivacyMode;
                _initialPrivacySeconds = PrivacyIdleSeconds;
            }
            if (scope != MonitorSettingsScope.Privacy)
            {
                _initialIsManaged = IsManaged;
                _initialBehavior = Behavior;
                _initialDimLevel = DimLevel;
                _initialLowerBrightnessOnBlackout = LowerBrightnessOnBlackout;
                _initialIdleValue = IdleValue;
                _initialSelectedTimeUnit = SelectedTimeUnit;
                _initialIsActiveOnInput = IsActiveOnInput;
                _initialIsActiveOnMousePosition = IsActiveOnMousePosition;
                _initialIsActiveOnActiveWindow = IsActiveOnActiveWindow;
            }
            OnPropertyChanged(nameof(SavedRule));
            OnPropertyChanged(nameof(SavedRestRule)); OnPropertyChanged(nameof(HasSavedRest));
            RefreshPrivacy();
            UpdateDirtyState();
        }

        /// <summary>
        /// Returns every tracked property to the value it had when the configuration was last saved.
        /// </summary>
        public void Revert(MonitorSettingsScope scope = MonitorSettingsScope.All)
        {
            if (scope != MonitorSettingsScope.Rest)
            {
                Options = _initialOptions.Copy();
                PrivacyMode = _initialPrivacyMode;
                PrivacyIdleSeconds = _initialPrivacySeconds;
            }
            if (scope != MonitorSettingsScope.Privacy)
            {
                IsManaged = _initialIsManaged;
                Behavior = _initialBehavior;
                DimLevel = _initialDimLevel;
                LowerBrightnessOnBlackout = _initialLowerBrightnessOnBlackout;
                IdleValue = _initialIdleValue;
                SelectedTimeUnit = _initialSelectedTimeUnit;
                IsActiveOnInput = _initialIsActiveOnInput;
                IsActiveOnMousePosition = _initialIsActiveOnMousePosition;
                IsActiveOnActiveWindow = _initialIsActiveOnActiveWindow;
            }
            UpdateDirtyState();
        }

        /// <summary>
        /// Applies settings from a MonitorSettings model to this ViewModel.
        /// </summary>
        /// <param name="settings">The settings to apply.</param>
        public void ApplySettings(MonitorSettings settings)
        {
            Options = (settings.PrivacyOptions ?? new()).Copy();
            if (settings.PrivacyMode != Features.MonitorPrivacy.PrivacyMode.None) Options.LastEnabledMode = settings.PrivacyMode;
            PrivacyMode = settings.HasPrivacy ? settings.PrivacyMode : Features.MonitorPrivacy.PrivacyMode.None;
            PrivacyIdleSeconds = settings.PrivacyIdleSeconds;
            IsManaged = settings.IsManaged && !(settings.PrivacyEnabled == null && settings.Behavior == MonitorBehaviorType.None);
            Behavior = settings.Behavior;
            DimLevel = settings.DimLevel;
            LowerBrightnessOnBlackout = settings.LowerBrightnessOnBlackout && IsDimBehaviorEnabled;
            IdleValue = settings.IdleValue;
            SelectedTimeUnit = settings.IdleUnit;
            IsActiveOnInput = settings.IsActiveOnInput;
            IsActiveOnMousePosition = settings.IsActiveOnMousePosition;
            IsActiveOnActiveWindow = settings.IsActiveOnActiveWindow;
        }

        /// <summary>
        /// Converts the current ViewModel state to a MonitorSettings model.
        /// </summary>
        /// <returns>A MonitorSettings object representing the current configuration.</returns>
        public MonitorSettings ToSettings()
        {
            return new MonitorSettings
            {
                HardwareId = _monitorInfo.HardwareId,
                PrivacyOptions = Options.Copy(),
                PrivacyMode = PrivacyMode,
                PrivacyEnabled = IsPrivacyEnabled,
                PrivacyIdleSeconds = PrivacyIdleSeconds,
                IsManaged = IsManaged,
                Behavior = Behavior,
                DimLevel = DimLevel,
                LowerBrightnessOnBlackout = LowerBrightnessOnBlackout,
                IdleValue = IdleValue,
                IdleUnit = SelectedTimeUnit,
                IsActiveOnInput = IsActiveOnInput,
                IsActiveOnMousePosition = IsActiveOnMousePosition,
                IsActiveOnActiveWindow = IsActiveOnActiveWindow
            };
        }

        #endregion State Management (Dirty Tracking, Saving, Loading)

        #region Validation

        /// <summary>
        /// Gets whether the current configuration is valid according to all validation rules.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (!IsManaged) return ValidatePrivacy() == null;

                return ValidatePrivacy() == null && ValidateIdleValue() == null && ValidateActiveConditions() == null && ValidateBehavior() == null;
            }
        }

        /// <summary>
        /// Gets the error message for the entire object. Not used in this implementation.
        /// </summary>
        public string Error => string.Empty;

        /// <summary>
        /// Gets the error message for the property with the given name, if any.
        /// </summary>
        /// <param name="columnName">The name of the property to validate.</param>
        /// <returns>An error message if invalid, otherwise an empty string.</returns>
        public string this[string columnName]
        {
            get
            {
                if (!IsManaged) return string.Empty; // Only validate if the monitor is managed

                string? result = null;
                switch (columnName)
                {
                    case nameof(IdleValue):
                        result = ValidateIdleValue();
                        break;

                    case nameof(IsActiveOnInput):
                    case nameof(IsActiveOnMousePosition):
                    case nameof(IsActiveOnActiveWindow):
                        result = ValidateActiveConditions();
                        break;

                    case nameof(Behavior):
                        result = ValidateBehavior();
                        break;
                }
                return result ?? string.Empty;
            }
        }

        /// <summary>
        /// Validates the idle value property.
        /// </summary>
        /// <returns>An error message if invalid, otherwise null.</returns>
        private string? ValidateIdleValue()
        {
            if (!IsSleepEnabled && IsPrivacyEnabled) return null;
            var factor = SelectedTimeUnit == TimeUnit.Hours ? 3600000L : SelectedTimeUnit == TimeUnit.Minutes ? 60000L : 1000L;
            if (IdleValue > int.MaxValue / factor) return UiText.Get("空闲时长过大，请缩短时长或更换单位。");
            if (IdleValue == null || IdleValue <= 0)
                return UiText.Get("空闲时长必须是大于零的数字。");
            return null;
        }

        /// <summary>
        /// Validates that at least one active condition is selected.
        /// </summary>
        /// <returns>An error message if invalid, otherwise null.</returns>
        private string? ValidateActiveConditions()
        {
            if (!IsSleepEnabled && IsPrivacyEnabled) return null;
            if (!IsActiveOnInput && !IsActiveOnMousePosition && !IsActiveOnActiveWindow)
                return UiText.Get("请至少选择一项保持唤醒的条件。");
            return null;
        }

        /// <summary>
        /// Validates the Behavior property to ensure a monitor behavior is selected when managed.
        /// </summary>
        /// <returns>
        /// An error message if the behavior is not selected (None), otherwise null.
        /// </returns>
        private string? ValidateBehavior()
        {
            if (Behavior == MonitorBehaviorType.Dim && !IsDimBehaviorEnabled)
                return UiText.Get("此显示器不支持 DDC/CI 亮度调节");
            if (Behavior == MonitorBehaviorType.None && !IsPrivacyEnabled)
                return UiText.Get("请选择一种空闲时的操作。");
            return null;
        }

        #endregion Validation
    }
}
