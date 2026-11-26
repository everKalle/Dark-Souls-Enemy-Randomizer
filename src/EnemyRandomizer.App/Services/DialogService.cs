using System.IO;
using System.Windows;
using System.Windows.Threading;
using EnemyRandomizer.App.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace EnemyRandomizer.App.Services;

/// <summary>WPF implementation of <see cref="IDialogService"/>. Tool windows are created by DI and kept single-instance.</summary>
public sealed class DialogService(IServiceProvider services) : IDialogService, IUiThread
{
    private readonly Dictionary<Type, Window> _openWindows = [];

    private static Window? Owner => Application.Current?.MainWindow is { IsLoaded: true } main ? main : null;

    public void ShowInfo(string title, string message) => Show(title, message, MessageBoxImage.Information);

    public void ShowWarning(string title, string message) => Show(title, message, MessageBoxImage.Warning);

    public void ShowError(string title, string message) => Show(title, message, MessageBoxImage.Error);

    public bool Confirm(string title, string message) =>
        (Owner is { } owner
            ? MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            : MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)) == MessageBoxResult.Yes;

    public bool? ConfirmOrCancel(string title, string message)
    {
        var result = Owner is { } owner
            ? MessageBox.Show(owner, message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question)
            : MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return result switch
        {
            MessageBoxResult.Yes => true,
            MessageBoxResult.No => false,
            _ => null,
        };
    }

    public string? OpenFile(string filter, string? initialDirectory)
    {
        var dialog = new OpenFileDialog { Filter = filter, CheckFileExists = true };
        if (initialDirectory is not null && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }

    public void ShowTextConfig() => ShowSingle<TextConfigWindow>();

    public void ShowEnemyConfigEditor() => ShowSingle<EnemyConfigEditorWindow>();

    public void ShowAbout()
    {
        var window = services.GetRequiredService<AboutWindow>();
        window.Owner = Owner;
        window.ShowDialog();
    }

    public void Post(Action action) =>
        Application.Current.Dispatcher.BeginInvoke(action, DispatcherPriority.Background);

    private void ShowSingle<TWindow>()
        where TWindow : Window
    {
        if (_openWindows.TryGetValue(typeof(TWindow), out var open))
        {
            open.Activate();
            return;
        }

        var window = services.GetRequiredService<TWindow>();
        window.Owner = Owner;
        window.Closed += (_, _) => _openWindows.Remove(typeof(TWindow));
        _openWindows[typeof(TWindow)] = window;
        window.Show();
    }

    private static void Show(string title, string message, MessageBoxImage image)
    {
        if (Owner is { } owner)
        {
            MessageBox.Show(owner, message, title, MessageBoxButton.OK, image);
        }
        else
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, image);
        }
    }
}
