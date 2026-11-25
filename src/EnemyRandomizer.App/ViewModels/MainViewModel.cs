using System.Collections.ObjectModel;
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
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.App.ViewModels;

/// <summary>Main window: option tabs, seed, enemy config selection and the randomizer commands.</summary>
public sealed partial class MainViewModel : ObservableObject, ISettingsHost
{
    private readonly RandomizerOptionSettings _options;
    private readonly MessageSettings _messages;
    private readonly IReadOnlyList<OptionViewModel> _allOptions;
    private readonly ILastConfigStore _lastConfig;
    private readonly IEnemyConfigRepository _enemyConfigs;
    private readonly IRandomizerService _randomizer;
    private readonly IDialogService _dialogs;
    private readonly IGamePaths _paths;

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
        IInstallationStatus installation)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(installation);
        ArgumentNullException.ThrowIfNull(paths);

        _options = options.Value;
        _messages = messages.Value;
        _lastConfig = lastConfig;
        _enemyConfigs = enemyConfigs;
        _randomizer = randomizer;
        _dialogs = dialogs;
        _paths = paths;

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

    private readonly IInstallationStatus _installation;
    private readonly string _baseTitle;

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
            var report = await _dialogs.RunWithProgressAsync(title, _installation.CheckAsync).ConfigureAwait(true);
            while (report.CopiesFailed && _dialogs.Confirm(_messages.Get(MessageKeys.CopiesFailedTitle), _messages.Get(MessageKeys.CopiesFailedText)))
            {
                report = await _dialogs.RunWithProgressAsync(title, _installation.CheckAsync).ConfigureAwait(true);
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
    [NotifyCanExecuteChangedFor(nameof(RandomizeCommand), nameof(RevertCommand), nameof(CopyPlacementCommand))]
    public partial bool IsBusy { get; set; }

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
        _lastConfig.Save(settings);
        await RunAsync("Randomizing, please wait", (p, ct) => _randomizer.RandomizeAsync(settings, p, ct)).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RevertAsync()
    {
        var restoreEffects = _dialogs.Confirm("Revert to normal", _messages.Get(MessageKeys.RevertAskEffects));
        await RunAsync("Reverting, please wait", (p, ct) => _randomizer.RevertAsync(restoreEffects, p, ct)).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task CopyPlacementAsync()
    {
        var file = _dialogs.OpenFile(_messages.Get(MessageKeys.ReferenceFileFilter), _paths.LayoutReferenceDirectory);
        if (file is null)
        {
            return;
        }

        await RunAsync("Copying enemy placement, please wait", (p, ct) => _randomizer.CopyPlacementAsync(file, p, ct)).ConfigureAwait(true);
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

    private bool CanRun() => !IsBusy && Report is { CanRandomize: true };

    private async Task RunAsync(
        string title,
        Func<IProgress<OperationProgress>, CancellationToken, Task<OperationResult>> work)
    {
        IsBusy = true;
        try
        {
            var result = await _dialogs.RunWithProgressAsync(title, work).ConfigureAwait(true);
            if (result.Success)
            {
                _dialogs.ShowInfo(result.Title, result.Message);
            }
            else
            {
                _dialogs.ShowError(result.Title, result.Message);
            }
        }
#pragma warning disable CA1031 // Any failure of a run must end up in a message, never crash the app.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _dialogs.ShowError(title, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

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
