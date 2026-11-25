using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Infrastructure.Game;

namespace EnemyRandomizer.Infrastructure.Tests;

/// <summary>A throw-away game folder with the shipped <c>enemyRandomizerData</c> catalog.</summary>
internal sealed class TempGameFolder : IDisposable
{
    public TempGameFolder()
    {
        Root = Path.Combine(Path.GetTempPath(), "er-tests-" + Guid.NewGuid().ToString("N"));
        Settings = new AppSettings
        {
            Title = "t",
            OriginalAuthor = "a",
            ProjectUrl = "https://example.org",
            GameDirectory = Root,
            DataFolder = "enemyRandomizerData",
            FileCopyRetries = 1,
            DataFolders = new DataFolderSettings
            {
                MapStudioCopies = "mapStudioCopies", MapAiCopies = "mapAiCopies", SfxCopies = "sfxCopies",
                AiScripts = "aiscripts", ParamBackup = "param", Logs = "logs", LayoutReferences = "refs",
                CustomConfigs = "customConfigs", Emevd = "emevd", ReplacementReferences = "replacement_ref",
                OriginalEnemyReferences = "original_enemies_ref",
            },
        };
        Paths = new GamePaths(Settings, Root);

        Directory.CreateDirectory(Paths.ReplacementReferenceDirectory);
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "data", "valid_new.txt"),
            Path.Combine(Paths.ReplacementReferenceDirectory, "valid_new.txt"));
    }

    public string Root { get; }

    public AppSettings Settings { get; }

    public IGamePaths Paths { get; }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}

internal sealed class FixedInstallation(GameEdition? edition) : IGameInstallation
{
    public GameEdition? Edition { get; } = edition;
}
