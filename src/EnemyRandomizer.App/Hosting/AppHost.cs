using System.IO;
using EnemyRandomizer.App.Services;
using EnemyRandomizer.App.ViewModels;
using EnemyRandomizer.App.ViewModels.Options;
using EnemyRandomizer.App.Views;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Infrastructure;
using EnemyRandomizer.Infrastructure.Configuration;
using EnemyRandomizer.Infrastructure.Game;
using EnemyRandomizer.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnemyRandomizer.App.Hosting;

/// <summary>Builds the Generic Host with configuration, logging and all services (factory for the host).</summary>
public static class AppHost
{
    public static IHost Create(string[] args)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = baseDirectory,
            DisableDefaults = true,
        });

        var preferences = new JsonUserPreferences(JsonUserPreferences.DefaultFile);

        builder.Configuration.AddRandomizerConfigFiles(baseDirectory);
        if (!string.IsNullOrWhiteSpace(preferences.Current.GameDirectory))
        {
            // The folder chosen in the UI overrides appsettings.json; env vars and the command line still win.
            builder.Configuration.AddInMemoryCollection(
                [new($"{AppSettings.SectionName}:{nameof(AppSettings.GameDirectory)}", preferences.Current.GameDirectory)]);
        }

        builder.Configuration
               .AddEnvironmentVariables("ENEMYRANDOMIZER_")
               .AddCommandLine(args);

        var app = builder.Configuration.GetSection(AppSettings.SectionName).Get<AppSettings>() ?? new AppSettings();
        var logDirectory = new GamePaths(app, baseDirectory).LogDirectory;

        builder.Services.AddSingleton<IUserPreferences>(preferences);
        builder.Services
               .AddRandomizerSettings(builder.Configuration)
               .AddRandomizerLogging(builder.Configuration, logDirectory)
               .AddRandomizerInfrastructure()
               .AddUi();

        return builder.Build();
    }

    private static IServiceCollection AddUi(this IServiceCollection services)
    {
        services.AddSingleton<IAppShell, AppShell>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<IDialogService>(sp => sp.GetRequiredService<DialogService>());
        services.AddSingleton<IUiThread>(sp => sp.GetRequiredService<DialogService>());

        services.AddSingleton<IOptionViewModelFactory, OptionViewModelFactory>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ISettingsHost>(sp => sp.GetRequiredService<MainViewModel>());
        services.AddSingleton<MainWindow>();

        services.AddTransient<TextConfigViewModel>();
        services.AddTransient<TextConfigWindow>();
        services.AddTransient<EnemyConfigEditorViewModel>();
        services.AddTransient<EnemyConfigEditorWindow>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<AboutWindow>();
        return services;
    }
}
