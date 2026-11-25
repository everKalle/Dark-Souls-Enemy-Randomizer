using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Installation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Installation;

/// <summary>
/// Raises the PTDE memory limit so the game does not crash when all effects are loaded (fix by Metal Crow).
/// Legacy <c>check_exe.py:54-115</c>: back up once, then replace every occurrence of the search bytes.
/// </summary>
public sealed partial class ExePatcher(
    IGamePaths paths,
    IGameInstallation installation,
    IOptions<GameDataSettings> gameData,
    ILogger<ExePatcher> logger) : IExePatcher
{
    private GameEditionDefinition Ptde => gameData.Value.Editions[GameEdition.Ptde];

    private string ExeFile => paths.InGame(Ptde.ExeName);

    public bool PatchIfNeeded()
    {
        var state = installation.ExeState;
        if (!ExeStates.NeedsPatch(state) || Ptde.ExePatch is not { } patch)
        {
            return true;
        }

        try
        {
            var backup = paths.InGame(ExeStates.IsDebug(state) ? patch.DebugBackupName : patch.BackupName);
            if (!File.Exists(backup))
            {
                File.Copy(ExeFile, backup);
            }

            var bytes = File.ReadAllBytes(ExeFile);
            var count = ReplaceAll(bytes, ExePatchSettings.ParseHex(patch.Search), ExePatchSettings.ParseHex(patch.Replace));
            File.WriteAllBytes(ExeFile, bytes);
            LogPatched(logger, count);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            LogPatchFailed(logger, ex);
            return false;
        }
        finally
        {
            installation.Refresh();
        }
    }

    public bool Restore()
    {
        if (Ptde.ExePatch is not { } patch)
        {
            return false;
        }

        var backupName = installation.ExeState switch
        {
            ExeState.Patched => patch.BackupName,
            ExeState.PatchedDebug => patch.DebugBackupName,
            _ => null,
        };

        if (backupName is null)
        {
            return false;
        }

        var backup = paths.InGame(backupName);
        if (!File.Exists(backup))
        {
            LogBackupMissing(logger, backupName);
            return false;
        }

        File.Copy(backup, ExeFile, overwrite: true);
        installation.Refresh();
        LogRestored(logger);
        return true;
    }

    /// <summary>Replaces every occurrence of <paramref name="search"/> in place; returns the number of replacements.</summary>
    internal static int ReplaceAll(Span<byte> data, ReadOnlySpan<byte> search, ReadOnlySpan<byte> replace)
    {
        if (search.Length != replace.Length || search.IsEmpty)
        {
            throw new ArgumentException("Search and replace must be non-empty and of equal length.", nameof(replace));
        }

        var count = 0;
        var offset = 0;
        int index;
        while ((index = data[offset..].IndexOf(search)) >= 0)
        {
            replace.CopyTo(data[(offset + index)..]);
            offset += index + search.Length;
            count++;
        }

        return count;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Exe patched ({Count} replacements)")]
    private static partial void LogPatched(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exe could not be patched")]
    private static partial void LogPatchFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exe backup {Backup} not found, exe not restored")]
    private static partial void LogBackupMissing(ILogger logger, string backup);

    [LoggerMessage(Level = LogLevel.Information, Message = "Exe restored from backup")]
    private static partial void LogRestored(ILogger logger);
}
