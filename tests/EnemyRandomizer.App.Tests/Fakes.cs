using System.IO;
using EnemyRandomizer.App.Services;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.EnemyConfigs;
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
    public int ProgressRuns { get; private set; }

    public void ShowInfo(string title, string message) => Infos.Add(message);
    public void ShowError(string title, string message) => Errors.Add(message);
    public bool Confirm(string title, string message) => ConfirmAnswer;
    public string? OpenFile(string filter, string? initialDirectory) => null;
    public void ShowTextConfig() { }
    public void ShowEnemyConfigEditor() { }
    public void ShowAbout() { }

    public Task<OperationResult> RunWithProgressAsync(
        string title,
        Func<IProgress<OperationProgress>, CancellationToken, Task<OperationResult>> work)
    {
        ProgressRuns++;
        return work(new Progress<OperationProgress>(), CancellationToken.None);
    }

    public void Post(Action action) => action();
}

internal sealed class FakeLastConfigStore : ILastConfigStore
{
    public RandomizerSettings? Saved { get; set; }
    public RandomizerSettings? Load() => Saved;
    public void Save(RandomizerSettings settings) => Saved = settings;
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

    public Task<OperationResult> RandomizeAsync(RandomizerSettings settings, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        LastSettings = settings;
        return Task.FromResult(OperationResult.Ok("Randomize", "done"));
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
    public string LayoutReferenceDirectory => "C:/game/data/refs";
    public string LogDirectory => "C:/game/data/logs";
    public string LastConfigFile => "C:/game/data/lastConfig.json";
    public string LegacyLastConfigFile => "C:/game/data/lastConfig.txt";
}

internal sealed class FakeInstallation(GameEdition? edition) : IGameInstallation
{
    public GameEdition? Edition { get; } = edition;
}
