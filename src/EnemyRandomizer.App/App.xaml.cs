using System.IO;
using System.Windows;
using System.Windows.Threading;
using EnemyRandomizer.App.Hosting;
using EnemyRandomizer.App.ViewModels;
using EnemyRandomizer.App.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.App;

/// <summary>Composition root: builds the Generic Host, validates configuration, shows the main window.</summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _host = AppHost.Create(e.Args);
            await _host.StartAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is OptionsValidationException or InvalidDataException or FileNotFoundException)
        {
            ShowFatal("Invalid configuration", ex.Message);
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        LogStarted(logger, AppContext.BaseDirectory);

        var window = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
        await _host.Services.GetRequiredService<MainViewModel>().InitializeAsync().ConfigureAwait(true);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(true);
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_host is not null)
        {
            LogUnhandled(_host.Services.GetRequiredService<ILogger<App>>(), e.Exception);
        }

        MessageBox.Show(e.Exception.Message, "Unexpected error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Enemy randomizer started from {BaseDirectory}")]
    private static partial void LogStarted(ILogger logger, string baseDirectory);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled UI exception")]
    private static partial void LogUnhandled(ILogger logger, Exception exception);

    private void ShowFatal(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(1);
    }
}
