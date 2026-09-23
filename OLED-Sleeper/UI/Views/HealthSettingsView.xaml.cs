using OLED_Sleeper.Features.HealthReminder;
using OLED_Sleeper.UI.ViewModels;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace OLED_Sleeper.UI.Views;
public partial class HealthSettingsView : UserControl
{
    private HealthSettingsViewModel? _model;
    private string? _sceneKey;
    public HealthSettingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ObserveModel();
        Loaded += (_, _) => { ObserveModel(); RefreshScene(); };
        Unloaded += (_, _) => { if (_model != null) _model.PropertyChanged -= ModelChanged; _model = null; SceneHost.Content = null; _sceneKey = null; };
        IsVisibleChanged += (_, _) => RefreshScene();
        SizeChanged += (_, _) =>
        {
            bool wide = ActualWidth >= 650;
            PreviewColumn.Width = new GridLength(wide ? 250 : 0);
            Grid.SetColumn(PreviewPanel, wide ? 1 : 0); Grid.SetRow(PreviewPanel, wide ? 0 : 1);
            AppearanceOptions.Margin = new Thickness(0, 0, wide ? 18 : 0, 0);
        };
    }
    private void ObserveModel()
    {
        if (_model != null) _model.PropertyChanged -= ModelChanged;
        _model = DataContext as HealthSettingsViewModel;
        if (_model != null) _model.PropertyChanged += ModelChanged;
        _sceneKey = null; RefreshScene();
    }
    private void ModelChanged(object? sender, PropertyChangedEventArgs e) => RefreshScene();
    private void RefreshScene()
    {
        if (_model == null || _model.SelectedPage != 1 || (IsLoaded && !IsVisible)) { SceneHost.Content = null; _sceneKey = null; return; }
        var appearance = _model.DraftAppearance;
        var key = JsonSerializer.Serialize(appearance) + _model.PreviewRestStage;
        if (_sceneKey == key) return; // Runtime countdown notifications do not recreate animations or decode images.
        _sceneKey = key;
        SceneHost.Content = !_model.PreviewRestStage && appearance.ReminderStyle == HealthReminderStyle.Compact ? null :
            HealthSceneWindow.CreateScene(appearance, _model.PreviewRestStage, _model.StageUsesPicture ? _model.LoadPreviewImage() : null);
        SceneHost.Opacity = _model.PreviewRestStage ? 1 : appearance.OverlayPercent / 100d;
    }
}
