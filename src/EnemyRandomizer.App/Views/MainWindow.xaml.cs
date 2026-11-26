using System.ComponentModel;
using System.Windows;
using EnemyRandomizer.App.Services;
using EnemyRandomizer.App.ViewModels;

namespace EnemyRandomizer.App.Views;

/// <summary>Main window. Hover/focus help tracking lives in <c>OptionHelp</c>; this file restores and saves the window bounds.</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IUserPreferences _preferences;

    public MainWindow(MainViewModel viewModel, IUserPreferences preferences)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _preferences = preferences;
        DataContext = viewModel;
        ApplySavedBounds(preferences.Current);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel)
        {
            return;
        }

        _viewModel.Persist();
        var bounds = RestoreBounds;
        _preferences.Save(_preferences.Current with
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            Maximized = WindowState == WindowState.Maximized,
        });
    }

    private void ApplySavedBounds(UserPreferencesData saved)
    {
        if (saved is not { Left: { } left, Top: { } top, Width: { } width, Height: { } height })
        {
            return;
        }

        // Ignore bounds from a monitor setup that no longer exists, so the window never opens off screen.
        var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        if (width < MinWidth || height < MinHeight || !screen.IntersectsWith(new Rect(left, top, width, height)))
        {
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = left;
        Top = top;
        Width = width;
        Height = height;
        if (saved.Maximized)
        {
            WindowState = WindowState.Maximized;
        }
    }
}
