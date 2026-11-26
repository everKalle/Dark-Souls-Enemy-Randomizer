using System.IO;
using EnemyRandomizer.App.Services;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.EnemyConfigs;
using EnemyRandomizer.Core.Installation;
using EnemyRandomizer.Core.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.App.Tests;

/// <summary>Loads the shipped JSON config sections.</summary>
internal static class ShippedConfig
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .SetBasePath(Path.Combine(AppContext.BaseDirectory, "config"))
        .AddJsonFile("appsettings.json")
        .AddJsonFile("randomizer-options.json")
        .AddJsonFile("messages.json")
        .AddJsonFile("rules.json")
        .Build();

    public static IOptions<T> Get<T>()
        where T : class, IValidatableSettings =>
        Options.Create(Configuration.GetSection(T.SectionName).Get<T>()!);
}

internal sealed class FakeDialogs : IDialogService, IUiThread
{
    public List<string> Errors { get; } = [];
    public List<string> Infos { get; } = [];
    public bool ConfirmAnswer { get; set; } = true;
    public bool? ConfirmOrCancelAnswer { get; set; } = true;

    public List<string> Warnings { get; } = [];

    public void ShowInfo(string title, string message) => Infos.Add(message);
    public void ShowWarning(string title, string message) => Warnings.Add(message);
    public void ShowError(string title, string message) => Errors.Add(message);
    public bool Confirm(string title, string message) => ConfirmAnswer;
    public bool? ConfirmOrCancel(string title, string message) => ConfirmOrCancelAnswer;
    public string? OpenFile(string filter, string? initialDirectory) => null;
    public void ShowTextConfig() { }
    public void ShowEnemyConfigEditor() { }
    public void ShowAbout() { }

    public void Post(Action action) => action();
}

internal sealed class FakeShell : IAppShell
{
    public string? PickedFolder { get; set; }
    public List<string> Opened { get; } = [];
    public List<string> Copied { get; } = [];
    public AppTheme? Theme { get; private set; }
    public int Restarts { get; private set; }

    public string? PickFolder(string? initialDirectory) => PickedFolder;
    public void OpenPath(string path) => Opened.Add(path);
    public void CopyText(string text) => Copied.Add(text);
    public void ApplyTheme(AppTheme theme) => Theme = theme;
    public void Restart() => Restarts++;
}

internal sealed class FakePreferences : IUserPreferences
{
    public UserPreferencesData Current { get; private set; } = new();
    public void Save(UserPreferencesData data) => Current = data;
}

internal sealed class FakeLastConfigStore : ILastConfigStore
{
    public RandomizerSettings? Saved { get; set; }
    public bool ThrowOnSave { get; set; }
    public RandomizerSettings? Load() => Saved;

    public void Save(RandomizerSettings settings)
    {
        if (ThrowOnSave)
        {
            throw new IOException("disk full");
        }

        Saved = settings;
    }
}

internal sealed class FakeEnemyConfigs : IEnemyConfigRepository
{
    public event EventHandler? ConfigsChanged;
    public List<string> Names { get; } = ["Default"];
    public List<bool>? LastSaved { get; private set; }

    public IReadOnlyList<string> ListNames() => Names;

    public IReadOnlyList<EnemyConfigEntry> Load(string name) =>
    [
        new("c2500", "Hollow", true, ""),
        new("c5201", "Arm", false, "excluded from enable all"),
    ];

    public bool Exists(string name) => Names.Contains(name);
    public bool IsValidName(string name) => !string.IsNullOrWhiteSpace(name) && name != "Default";

    public string Save(string name, IReadOnlyList<bool> enabled)
    {
        LastSaved = enabled.ToList();
        if (!Names.Contains(name))
        {
            Names.Add(name);
        }

        ConfigsChanged?.Invoke(this, EventArgs.Empty);
        return name + ".txt";
    }
}

internal sealed class FakeRandomizer : IRandomizerService
{
    public RandomizerSettings? LastSettings { get; private set; }

    /// <summary>Replaces the randomize result (to simulate failures, slow runs and cancellation).</summary>
    public Func<CancellationToken, Task<OperationResult>>? RandomizeOverride { get; set; }

    public Task<OperationResult> RandomizeAsync(RandomizerSettings settings, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        LastSettings = settings;
        return RandomizeOverride?.Invoke(cancellationToken) ?? Task.FromResult(OperationResult.Ok("Randomize", "done"));
    }

    public Task<OperationResult> CopyPlacementAsync(string referenceFile, IProgress<OperationProgress> progress, CancellationToken cancellationToken) =>
        Task.FromResult(OperationResult.Ok("Copy", "done"));

    public Task<OperationResult> RevertAsync(bool restoreEffects, IProgress<OperationProgress> progress, CancellationToken cancellationToken) =>
        Task.FromResult(OperationResult.Ok("Revert", "done"));
}

internal sealed class FakePaths : IGamePaths
{
    public string GameDirectory => "C:/game";
    public string DataDirectory => "C:/game/data";
    public string CustomConfigDirectory => "C:/game/data/customConfigs";
    public string ReplacementReferenceDirectory => "C:/game/data/replacement_ref";
    public string OriginalEnemyReferenceDirectory => "C:/game/data/original_enemies_ref";
    public string InGame(string relativePath) => "C:/game/" + relativePath;
    public string InData(string relativePath) => "C:/game/data/" + relativePath;
    public string LayoutReferenceDirectory => "C:/game/data/refs";
    public string LogDirectory => "C:/game/data/logs";
    public string MapStudioCopyDirectory => "C:/game/data/mapStudioCopies";
    public string MapAiCopyDirectory => "C:/game/data/mapAiCopies";
    public string EmevdDirectory => "C:/game/data/emevd";
    public string AiScriptDirectory => "C:/game/data/aiscripts";
    public string ParamBackupDirectory => "C:/game/data/param";
    public string LastConfigFile => "C:/game/data/lastConfig.json";
    public string LegacyLastConfigFile => "C:/game/data/lastConfig.txt";
}

internal sealed class FakeInstallationStatus(params InstallationReport[] reports) : IInstallationStatus
{
    private int _calls;

    public InstallationReport? Report { get; private set; }

    public int Calls => _calls;

    /// <summary>Returns the given reports one after another (the last one repeats).</summary>
    public Task<InstallationReport> CheckAsync(IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        Report = reports[Math.Min(_calls++, reports.Length - 1)];
        return Task.FromResult(Report);
    }

    public static FakeInstallationStatus Ok(ExeState state = ExeState.Patched) =>
        new(new InstallationReport(ExeStates.EditionOf(state), state, []));
}
