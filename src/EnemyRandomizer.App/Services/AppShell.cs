using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace EnemyRandomizer.App.Services;

/// <summary>WPF/Windows implementation of <see cref="IAppShell"/>.</summary>
public sealed class AppShell : IAppShell
{
    public string? PickFolder(string? initialDirectory)
    {
        var dialog = new OpenFolderDialog { Title = "Choose the Dark Souls game folder" };
        if (initialDirectory is not null && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        var owner = Application.Current?.MainWindow is { IsLoaded: true } main ? main : null;
        return dialog.ShowDialog(owner) == true ? dialog.FolderName : null;
    }

    public void OpenPath(string path)
    {
        if (path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || File.Exists(path) || Directory.Exists(path))
        {
            using var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }

    public void CopyText(string text) => Clipboard.SetText(text);

    public void ApplyTheme(AppTheme theme) => ThemeService.Apply(theme);

    public void Restart()
    {
        if (Environment.ProcessPath is { } exe)
        {
            using var process = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
        }

        Application.Current.Shutdown();
    }
}
