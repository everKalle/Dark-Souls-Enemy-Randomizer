using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Infrastructure.Game;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Setup;

/// <summary>Result of "Revert to normal".</summary>
public sealed record RevertResult(int FilesRestored, bool ExeRestored, bool ParamRestored);

/// <summary>
/// "Revert to normal" (legacy <c>revertToNormal</c> + <c>revertParam</c>, <c>randomizer_rng.py:993-1121</c>):
/// maps, events and AI scripts come back from their <c>.bak</c> files; effect files and the exe only on request;
/// NpcParam is taken from the GameParam copy made by the first-time setup.
/// </summary>
public sealed partial class GameRestorer(
    IGamePaths paths,
    GameFileLayout layout,
    IExePatcher exePatcher,
    IOptions<GameDataSettings> gameData,
    ILogger<GameRestorer> logger)
{
    public RevertResult Revert(GameEdition edition, bool restoreEffects, IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var total = layout.Maps.Count + 2;
        var step = 0;
        var restored = 0;

        foreach (var map in layout.Maps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(new OperationProgress(++step, total, $"Restoring {map.Name}"));
            restored += Restore(layout.MapStudio(map.Id));
            restored += Restore(layout.Event(map.Id, edition));
            if (map.Randomized)
            {
                restored += Restore(layout.AiScript(map.Id, edition));
            }
        }

        var exeRestored = false;
        progress.Report(new OperationProgress(++step, total, "Restoring effect files"));
        if (restoreEffects)
        {
            restored += gameData.Value.FfxFiles.Sum(f => Restore(layout.Sfx(f, edition)));
            exeRestored = exePatcher.Restore();
        }

        progress.Report(new OperationProgress(++step, total, "Restoring NpcParam"));
        var paramRestored = RevertParam(edition);

        LogReverted(logger, restored, exeRestored, paramRestored);
        return new RevertResult(restored, exeRestored, paramRestored);
    }

    /// <summary>Puts the original NpcParam back into the game's GameParam; other params stay untouched.</summary>
    private bool RevertParam(GameEdition edition)
    {
        var gameFile = paths.InGame(layout.GameParam(edition));
        var backupFile = Path.Combine(paths.ParamBackupDirectory, Path.GetFileName(gameFile));
        if (!File.Exists(backupFile) || !File.Exists(gameFile))
        {
            return false;
        }

        var index = gameData.Value.NpcParamIndex;
        var original = GameArchive.Read(backupFile);
        var current = GameArchive.Read(gameFile);
        current.Entries[index] = original.Entries[index];
        AtomicFile.WriteAllBytes(gameFile, current.ToBytes());
        return true;
    }

    private int Restore(string relativePath) => BackupFiles.Restore(paths.InGame(relativePath)) ? 1 : 0;

    [LoggerMessage(Level = LogLevel.Information, Message = "Reverted: {Files} files, exe {Exe}, NpcParam {Param}")]
    private static partial void LogReverted(ILogger logger, int files, bool exe, bool param);
}
