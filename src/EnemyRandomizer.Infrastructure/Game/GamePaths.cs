using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Game;

/// <summary>Resolves all folders from <see cref="AppSettings"/> once (single source of path rules).</summary>
public sealed class GamePaths : IGamePaths
{
    public GamePaths(IOptions<AppSettings> options)
        : this(options?.Value ?? throw new ArgumentNullException(nameof(options)), AppContext.BaseDirectory)
    {
    }

    public GamePaths(AppSettings settings, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var folders = settings.DataFolders;

        GameDirectory = ResolveGameDirectory(settings.GameDirectory, baseDirectory);
        DataDirectory = Path.Combine(GameDirectory, settings.DataFolder);
        CustomConfigDirectory = Path.Combine(DataDirectory, folders.CustomConfigs);
        ReplacementReferenceDirectory = Path.Combine(DataDirectory, folders.ReplacementReferences);
        LayoutReferenceDirectory = Path.Combine(DataDirectory, folders.LayoutReferences);
        LogDirectory = Path.Combine(DataDirectory, folders.Logs);
        LastConfigFile = Path.Combine(DataDirectory, "lastConfig.json");
        LegacyLastConfigFile = Path.Combine(DataDirectory, "lastConfig.txt");
    }

    public string GameDirectory { get; }
    public string DataDirectory { get; }
    public string CustomConfigDirectory { get; }
    public string ReplacementReferenceDirectory { get; }
    public string LayoutReferenceDirectory { get; }
    public string LogDirectory { get; }
    public string LastConfigFile { get; }
    public string LegacyLastConfigFile { get; }

    /// <summary>Empty setting means "the folder of the exe" (the exe is installed into the game folder).</summary>
    public static string ResolveGameDirectory(string configured, string baseDirectory) =>
        string.IsNullOrWhiteSpace(configured)
            ? Path.GetFullPath(baseDirectory)
            : Path.GetFullPath(configured, baseDirectory);
}
