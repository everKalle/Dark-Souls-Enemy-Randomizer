using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnemyRandomizer.App.Services;
using EnemyRandomizer.App.ViewModels.Options;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Installation;
using EnemyRandomizer.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.App.ViewModels;

/// <summary>Main window: option tabs, seed, enemy config selection and the randomizer commands.</summary>
public sealed partial class MainViewModel : ObservableObject, ISettingsHost
{
    private readonly IUserPreferences _preferences;
    private readonly IAppShell _shell;
    private readonly ILogger<MainViewModel> _logger;
    private readonly RandomizerOptionSettings _options;
    private readonly MessageSettings _messages;
    private readonly IReadOnlyList<OptionViewModel> _allOptions;
    private readonly ILastConfigStore _lastConfig;
    private readonly IEnemyConfigRepository _enemyConfigs;
    private readonly IRandomizerService _randomizer;
    private readonly IDialogService _dialogs;
    private readonly IGamePaths _paths;
    private readonly IInstallationStatus _installation;
    private readonly string _baseTitle;

    public MainViewModel(
        IOptions<AppSettings> app,
        IOptions<RandomizerOptionSettings> options,
        IOptions<MessageSettings> messages,
        IOptionViewModelFactory factory,
        ILastConfigStore lastConfig,
        IEnemyConfigRepository enemyConfigs,
        IRandomizerService randomizer,
        IDialogService dialogs,
        IGamePaths paths,
        IInstallationStatus installation,
        IUserPreferences preferences,
        IAppShell shell,
        ILogger<MainViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(installation);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _messages = messages.Value;
        _lastConfig = lastConfig;
        _enemyConfigs = enemyConfigs;
        _randomizer = randomizer;
        _dialogs = dialogs;
        _paths = paths;
        _preferences = preferences;
        _shell = shell;
        _logger = logger;

        _allOptions = _options.Options.Select(factory.Create).ToList();
        foreach (var option in _allOptions)
        {
            option.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(OptionViewModel.Value))
                {
                    UpdateEnabledStates();
                }
            };
        }

        Tabs = _options.Tabs
            .Select(t => new OptionTabViewModel(t.Title, _allOptions.Where(o => o.Definition.Tab == t.Id).ToList()))
            .ToList();
        SelectedTab = Tabs[0];

        _installation = installation;
        _baseTitle = $"{app.Value.Title} v{GetVersion()} (.NET)";
        Title = _baseTitle;
        EditionText = "Checking game files...";
        StartupErrorsHeader = _messages.Get(MessageKeys.StartupErrorsHeader);

        _enemyConfigs.ConfigsChanged += (_, _) => RefreshEnemyConfigs();
        RefreshEnemyConfigs();
        Apply(_lastConfig.Load() ?? RandomizerSettings.CreateDefault(_options));
    }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string EditionText { get; set; }

    /// <summary>Result of the startup checks; <c>null</c> until <see cref="InitializeAsync"/> finished.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RandomizeCommand), nameof(RevertCommand), nameof(CopyPlacementCommand))]
    public partial InstallationReport? Report { get; set; }

    /// <summary>Problems that block randomizing; shown instead of the option descriptions.</summary>
    public ObservableCollection<string> StartupErrors { get; } = [];

    public string StartupErrorsHeader { get; }

    [ObservableProperty]
    public partial bool HasStartupErrors { get; set; }

    /// <summary>
    /// Runs the startup checks and the first-time setup with a progress window once the main window is visible,
    /// asks to retry failed copies and shows startup warnings (legacy <c>randomizer.py:625-648</c>).
    /// </summary>
    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var title = _messages.Get(MessageKeys.SetupProgressTitle);
            var report = await RunWithProgressAsync(title, _installation.CheckAsync, allowCancel: false).ConfigureAwait(true);
            while (report.CopiesFailed && _dialogs.Confirm(_messages.Get(MessageKeys.CopiesFailedTitle), _messages.Get(MessageKeys.CopiesFailedText)))
            {
                report = await RunWithProgressAsync(title, _installation.CheckAsync, allowCancel: false).ConfigureAwait(true);
            }

            ShowReport(report);
        }
#pragma warning disable CA1031 // A crash during setup must end up as a visible startup error.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            ShowReport(new InstallationReport(null, ExeState.NotFound, [new InstallationProblem(MessageKeys.SetupFailed, ex.Message)]));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowReport(InstallationReport report)
    {
        Report = report;
        StartupErrors.Clear();
        foreach (var problem in report.Problems)
        {
            StartupErrors.Add(_messages.Get(problem.MessageKey, problem.Arguments));
        }

        HasStartupErrors = StartupErrors.Count > 0;

        var mode = report.Edition switch
        {
            GameEdition.Ptde => "PTDE",
            GameEdition.Remastered => "REMASTERED",
            _ => null,
        };
        Title = _baseTitle + (mode is null ? "" : $"    [Current Mode: {mode}]");
        EditionText = mode is null
            ? "No Dark Souls installation found in " + _paths.GameDirectory
            : $"Current mode: {mode} · exe: {report.ExeState} · {_paths.GameDirectory}";

        if (report.ShowUnknownExeWarning)
        {
            _dialogs.ShowWarning(_messages.Get(MessageKeys.UnknownExeTitle), _messages.Get(MessageKeys.UnknownExeText));
        }

        foreach (var warning in report.Warnings)
        {
            _dialogs.ShowWarning(_messages.Get(MessageKeys.EffectsWriteFailedTitle), _messages.Get(warning.MessageKey, warning.Arguments));
        }
    }

    public IReadOnlyList<OptionTabViewModel> Tabs { get; }

    public ObservableCollection<string> EnemyConfigNames { get; } = [];

    [ObservableProperty]
    public partial OptionTabViewModel SelectedTab { get; set; }

    [ObservableProperty]
    public partial string Seed { get; set; } = "";

    [ObservableProperty]
    public partial string SelectedEnemyConfig { get; set; } = RandomizerSettings.DefaultEnemyConfig;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RandomizeCommand), nameof(RevertCommand), nameof(CopyPlacementCommand), nameof(ChooseGameFolderCommand))]
    public partial bool IsBusy { get; set; }

    /// <summary>Progress overlay state while an operation runs; <c>null</c> otherwise.</summary>
    [ObservableProperty]
    public partial ProgressViewModel? ActiveProgress { get; set; }

    /// <summary>Result banner of the last operation; <c>null</c> when dismissed.</summary>
    [ObservableProperty]
    public partial NoticeViewModel? Notice { get; set; }

    public RandomizerSettings Current =>
        new(_allOptions.ToDictionary(o => o.Key, o => o.Value), Seed.Trim(), SelectedEnemyConfig);

    public void Apply(RandomizerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var normalized = settings.Normalize(_options);
        foreach (var option in _allOptions)
        {
            option.Value = normalized[option.Key];
        }

        Seed = normalized.Seed;
        SelectedEnemyConfig = EnemyConfigNames.Contains(normalized.EnemyConfig)
            ? normalized.EnemyConfig
            : RandomizerSettings.DefaultEnemyConfig;
        UpdateEnabledStates();
    }

    [RelayCommand]
    private void RestoreDefaults() => Apply(RandomizerSettings.CreateDefault(_options) with { EnemyConfig = SelectedEnemyConfig });

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RandomizeAsync()
    {
        var settings = Current;
        Persist();
        await RunAsync(
            _messages.Get(MessageKeys.RandomizingTitle),
            (p, ct) => _randomizer.RandomizeAsync(settings, p, ct),
            allowCancel: true,
            successFolder: _paths.LayoutReferenceDirectory).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RevertAsync()
    {
        var restoreEffects = _dialogs.ConfirmOrCancel(_messages.Get(MessageKeys.RevertTitle), _messages.Get(MessageKeys.RevertAskEffects));
        if (restoreEffects is null)
        {
            return;
        }

        // Not cancellable: stopping half way would leave a mix of original and randomized files.
        await RunAsync(_messages.Get(MessageKeys.RevertingTitle), (p, ct) => _randomizer.RevertAsync(restoreEffects.Value, p, ct), allowCancel: false).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task CopyPlacementAsync()
    {
        var file = _dialogs.OpenFile(_messages.Get(MessageKeys.ReferenceFileFilter), _paths.LayoutReferenceDirectory);
        if (file is null)
        {
            return;
        }

        await RunAsync(_messages.Get(MessageKeys.CopyingTitle), (p, ct) => _randomizer.CopyPlacementAsync(file, p, ct), allowCancel: true).ConfigureAwait(true);
    }

    [RelayCommand]
    private void ShowCopyInfo() =>
        _dialogs.ShowInfo(_messages.Get(MessageKeys.CopyInfoTitle), _messages.Get(MessageKeys.CopyInfoText));

    [RelayCommand]
    private void OpenTextConfig() => _dialogs.ShowTextConfig();

    [RelayCommand]
    private void OpenEnemyConfigEditor() => _dialogs.ShowEnemyConfigEditor();

    [RelayCommand]
    private void ShowAbout() => _dialogs.ShowAbout();

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private void ChooseGameFolder()
    {
        var folder = _shell.PickFolder(_paths.GameDirectory);
        if (folder is null)
        {
            return;
        }

        if (!_dialogs.Confirm(_messages.Get(MessageKeys.ChangeFolderTitle), _messages.Get(MessageKeys.ChangeFolderText, folder)))
        {
            return;
        }

        Persist();
        _preferences.Save(_preferences.Current with { GameDirectory = folder });
        _shell.Restart();
    }

    [RelayCommand]
    private void SetTheme(AppTheme theme)
    {
        _preferences.Save(_preferences.Current with { Theme = theme });
        _shell.ApplyTheme(theme);
    }

    [RelayCommand]
    private void DismissNotice() => Notice = null;

    [RelayCommand]
    private void OpenNoticeFolder()
    {
        if (Notice?.FolderPath is { } folder)
        {
            _shell.OpenPath(folder);
        }
    }

    [RelayCommand]
    private void CopyNoticeDetails()
    {
        if (Notice is { } notice)
        {
            _shell.CopyText(notice.Details);
        }
    }

    /// <summary>Saves the current settings. Called on randomize and when the window closes.</summary>
    public void Persist()
    {
        try
        {
            _lastConfig.Save(Current);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogSaveFailed(_logger, ex);
        }
    }

    private bool CanRun() => !IsBusy && Report is { CanRandomize: true };

    private bool IsNotBusy() => !IsBusy;

    private async Task<T> RunWithProgressAsync<T>(
        string title,
        Func<IProgress<OperationProgress>, CancellationToken, Task<T>> work,
        bool allowCancel)
    {
        using var cancellation = new CancellationTokenSource();
        var progress = new ProgressViewModel(title, allowCancel ? cancellation : null);
        var reporter = new Progress<OperationProgress>(progress.Report); // captures the UI context
        ActiveProgress = progress;
        try
        {
            return await Task.Run(() => work(reporter, cancellation.Token)).ConfigureAwait(true);
        }
        finally
        {
            ActiveProgress = null;
        }
    }

    private async Task RunAsync(
        string title,
        Func<IProgress<OperationProgress>, CancellationToken, Task<OperationResult>> work,
        bool allowCancel,
        string? successFolder = null)
    {
        IsBusy = true;
        Notice = null;
        try
        {
            var result = await RunWithProgressAsync(title, work, allowCancel).ConfigureAwait(true);
            Notice = result.Success
                ? new NoticeViewModel(NoticeKind.Success, result.Title, result.Message, successFolder, "Open folder")
                : new NoticeViewModel(NoticeKind.Error, result.Title, result.Message, _paths.LogDirectory, "Open log folder");
        }
        catch (OperationCanceledException)
        {
            Notice = new NoticeViewModel(NoticeKind.Warning, _messages.Get(MessageKeys.CancelledTitle), _messages.Get(MessageKeys.CancelledText));
        }
#pragma warning disable CA1031 // Any failure of a run must end up in a message, never crash the app.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogRunFailed(_logger, ex, title);
            Notice = new NoticeViewModel(NoticeKind.Error, title, ex.Message, _paths.LogDirectory, "Open log folder");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Operation failed: {Title}")]
    private static partial void LogRunFailed(ILogger logger, Exception exception, string title);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not save the last settings")]
    private static partial void LogSaveFailed(ILogger logger, Exception exception);

    private void RefreshEnemyConfigs()
    {
        var selected = SelectedEnemyConfig;
        EnemyConfigNames.Clear();
        foreach (var name in _enemyConfigs.ListNames())
        {
            EnemyConfigNames.Add(name);
        }

        SelectedEnemyConfig = EnemyConfigNames.Contains(selected) ? selected : RandomizerSettings.DefaultEnemyConfig;
    }

    private void UpdateEnabledStates()
    {
        var values = _allOptions.ToDictionary(o => o.Key, o => o.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var option in _allOptions)
        {
            option.UpdateEnabled(key => values.TryGetValue(key, out var v) ? v : int.MinValue);
        }
    }

    private static string GetVersion()
    {
        var info = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return info?.Split('+')[0] ?? "0.0.0";
    }
}
