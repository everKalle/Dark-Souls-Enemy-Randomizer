namespace EnemyRandomizer.Core.Abstractions;

/// <summary>Absolute paths of the game install and the randomizer data folder.</summary>
public interface IGamePaths
{
    string GameDirectory { get; }
    string DataDirectory { get; }
    string CustomConfigDirectory { get; }
    string ReplacementReferenceDirectory { get; }
    string LayoutReferenceDirectory { get; }
    string LogDirectory { get; }

    /// <summary>Settings of the last run (<c>lastConfig.json</c>).</summary>
    string LastConfigFile { get; }

    /// <summary>Settings file of v0.4.x (<c>lastConfig.txt</c>), imported once when no JSON file exists.</summary>
    string LegacyLastConfigFile { get; }
}

/// <summary>Which game is installed in <see cref="IGamePaths.GameDirectory"/>.</summary>
public interface IGameInstallation
{
    /// <summary>Detected edition, or <c>null</c> when no game exe was found.</summary>
    GameEdition? Edition { get; }
}
