using EnemyRandomizer.Core.Installation;

namespace EnemyRandomizer.Core.Abstractions;

/// <summary>Absolute paths of the game install and the randomizer data folder.</summary>
public interface IGamePaths
{
    string GameDirectory { get; }
    string DataDirectory { get; }
    string CustomConfigDirectory { get; }
    string ReplacementReferenceDirectory { get; }
    string OriginalEnemyReferenceDirectory { get; }
    string LayoutReferenceDirectory { get; }
    string LogDirectory { get; }

    /// <summary>Clean .msb copies with the added character models (randomizing starts from these).</summary>
    string MapStudioCopyDirectory { get; }

    /// <summary>Original .luabnd copies.</summary>
    string MapAiCopyDirectory { get; }

    /// <summary>The randomizer's own event files, one subfolder per edition.</summary>
    string EmevdDirectory { get; }

    /// <summary>Extracted <c>*_battle.lua</c>/<c>*_logic.lua</c> AI scripts.</summary>
    string AiScriptDirectory { get; }

    /// <summary>Original GameParam copy (NpcParam is restored from it).</summary>
    string ParamBackupDirectory { get; }

    /// <summary>Settings of the last run (<c>lastConfig.json</c>).</summary>
    string LastConfigFile { get; }

    /// <summary>Settings file of v0.4.x (<c>lastConfig.txt</c>), imported once when no JSON file exists.</summary>
    string LegacyLastConfigFile { get; }

    /// <summary>Absolute path of a file relative to the game folder (forward or back slashes).</summary>
    string InGame(string relativePath);

    /// <summary>Absolute path of a file relative to the data folder.</summary>
    string InData(string relativePath);
}

/// <summary>Which game is installed and in which state its exe is.</summary>
public interface IGameInstallation
{
    /// <summary>Detected edition, or <c>null</c> when no game exe was found.</summary>
    GameEdition? Edition { get; }

    ExeState ExeState { get; }

    /// <summary>Detects again (after the exe was patched or restored).</summary>
    void Refresh();
}

/// <summary>One startup check (composite part). Returns the problems it found, empty when fine.</summary>
public interface IInstallationCheck
{
    IEnumerable<InstallationProblem> Check(GameEdition? edition, ExeState exeState);
}

/// <summary>Patches the PTDE exe memory limit and restores the original (legacy <c>check_exe.py</c>).</summary>
public interface IExePatcher
{
    /// <summary>Patches an unpacked exe (backup first). Returns false when the exe could not be written.</summary>
    bool PatchIfNeeded();

    /// <summary>Restores the exe from its backup when it is patched. Returns true when something was restored.</summary>
    bool Restore();
}

/// <summary>
/// Startup sequence: checks, exe patch and first-time setup. Can be run again (retry after failed copies).
/// </summary>
public interface IInstallationStatus
{
    /// <summary>Result of the last <see cref="CheckAsync"/>, <c>null</c> before the first run.</summary>
    InstallationReport? Report { get; }

    Task<InstallationReport> CheckAsync(IProgress<OperationProgress> progress, CancellationToken cancellationToken);
}

/// <summary>Prepares the data copies the randomizer works from (legacy <c>firstTimeSetup</c>).</summary>
public interface IFirstTimeSetup
{
    SetupResult Run(GameEdition edition, IProgress<OperationProgress> progress, CancellationToken cancellationToken);

    /// <summary>Counts missing/broken copies without changing anything (legacy <c>checkCopiedFiles</c>).</summary>
    SetupResult Verify(GameEdition edition);
}
